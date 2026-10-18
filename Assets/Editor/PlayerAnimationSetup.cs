using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PlayerAnimationSetup
{
    private const string ModelPath = "Assets/Models/SimpleHuman/SimpleHuman_Rigged.fbx";
    private const string ControllerPath = "Assets/Animations/Player/Player.controller";
    private const string VisualPath = "Assets/Prefabs/Gameplay/PlayerVisual.prefab";
    private const string RequestPath = "Temp/PlayerAnimation/install.request";

    [InitializeOnLoadMethod]
    private static void RunRequestedSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RequestPath)) return;
            File.Delete(RequestPath);
            try { File.WriteAllText("Temp/PlayerAnimation/install-result.txt", Install()); }
            catch (Exception e)
            {
                File.WriteAllText("Temp/PlayerAnimation/install-result.txt", e.ToString());
                Debug.LogException(e);
            }
        };
    }

    [MenuItem("Tools/Player Animation/Install Animated Player")]
    public static void InstallFromMenu() => Debug.Log(Install());

    public static string Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory("Temp/PlayerAnimation/Backups");
        Directory.CreateDirectory("Assets/Animations/Player");
        AssetDatabase.Refresh();
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.motionNodeName = "Root";
        importer.importAnimation = true;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.optimizeGameObjects = false;
        importer.SaveAndReimport();
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.name = clip.takeName.Contains("FallToKnees") ? "FallToKnees" :
                clip.takeName.Contains("Walk") ? "Walk" : "Idle";
            clip.loopTime = clip.name != "FallToKnees";
            clip.loopPose = clip.name != "FallToKnees";
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        var animations = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        AnimationClip Find(string name) => animations.Single(c => c.name == name);
        var idleClip = Find("Idle");
        var walkClip = Find("Walk");
        var fallClip = Find("FallToKnees");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AddParameter(controller, "Moving", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "WalkSpeed", AnimatorControllerParameterType.Float);
        AddParameter(controller, "Fall", AnimatorControllerParameterType.Trigger);
        var sm = controller.layers[0].stateMachine;
        var idle = State(sm, "Idle", idleClip, new Vector3(250, 80));
        var walk = State(sm, "Walk", walkClip, new Vector3(500, 80));
        var fall = State(sm, "FallToKnees", fallClip, new Vector3(500, 240));
        sm.defaultState = idle;
        walk.speedParameter = "WalkSpeed";
        walk.speedParameterActive = true;
        Transition(idle, walk, AnimatorConditionMode.If, "Moving");
        Transition(walk, idle, AnimatorConditionMode.IfNot, "Moving");
        if (!sm.anyStateTransitions.Any(t => t.destinationState == fall))
        {
            var t = sm.AddAnyStateTransition(fall);
            t.hasExitTime = false; t.duration = 0.12f; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, "Fall");
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        BuildVisual(controller);

        var report = new StringBuilder("Animated player installed.\n");
        report.AppendLine("Clips: " + string.Join(", ", animations.Select(c => c.name + "=" + c.length.ToString("F3") + "s")));
        const string sharedPath = "Assets/Prefabs/Gameplay/GameplayRig.prefab";
        Backup(sharedPath);
        var shared = PrefabUtility.LoadPrefabContents(sharedPath);
        try
        {
            foreach (var player in shared.GetComponentsInChildren<PlayerMovement>(true)) Attach(player);
            PrefabUtility.SaveAsPrefabAsset(shared, sharedPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(shared); }

        var originalScene = SceneManager.GetActiveScene();
        foreach (string name in new[] { "Prologue", "Labyrinth", "TheFinal" })
        {
            string path = "Assets/Scenes/" + name + ".unity";
            Backup(path);
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var players = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerMovement>(true)).ToArray();
                if (players.Length != 1) throw new Exception(name + ": expected one player, found " + players.Length);
                Attach(players[0]);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.AppendLine(name + ": model, Animator, movement binding installed.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        SceneManager.SetActiveScene(originalScene);
        AssetDatabase.SaveAssets();
        var selected = Object.FindAnyObjectByType<PlayerModelAnimation>();
        if (selected)
        {
            Selection.activeGameObject = selected.gameObject;
            SceneView.lastActiveSceneView?.LookAt(selected.transform.position, Quaternion.Euler(20, 145, 0), 3.5f);
        }
        return report.ToString();
    }

    private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (!controller.parameters.Any(p => p.name == name)) controller.AddParameter(name, type);
    }

    private static AnimatorState State(AnimatorStateMachine sm, string name, Motion motion, Vector3 position)
    {
        var state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? sm.AddState(name, position);
        state.motion = motion;
        return state;
    }

    private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string parameter)
    {
        if (from.transitions.Any(t => t.destinationState == to)) return;
        var t = from.AddTransition(to);
        t.hasExitTime = false; t.duration = 0.12f;
        t.AddCondition(mode, 0f, parameter);
    }

    private static void BuildVisual(RuntimeAnimatorController controller)
    {
        var root = new GameObject("PlayerVisual");
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            model.transform.SetParent(root.transform, false);
            model.name = "Model";
            var idle = controller.animationClips.Single(c => c.name == "Idle");
            idle.SampleAnimation(model, 0f);
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var bounds = new Bounds();
            bool firstVertex = true;
            foreach (var renderer in renderers)
            {
                var mesh = new Mesh();
                renderer.BakeMesh(mesh);
                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 point = renderer.transform.TransformPoint(vertex);
                    if (firstVertex) { bounds = new Bounds(point, Vector3.zero); firstVertex = false; }
                    else bounds.Encapsulate(point);
                }
                Object.DestroyImmediate(mesh);
            }
            model.transform.localPosition -= Vector3.up * bounds.min.y;
            root.transform.localScale = Vector3.one * (2f / bounds.size.y);
            var animator = model.GetComponent<Animator>();
            if (!animator) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ApplyAppearance(root);
            PrefabUtility.SaveAsPrefabAsset(root, VisualPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void Attach(PlayerMovement player)
    {
        var capsule = player.GetComponent<CapsuleCollider>();
        var oldRenderer = player.GetComponent<MeshRenderer>();
        if (oldRenderer) oldRenderer.enabled = false;
        var visual = player.transform.Find("PlayerVisual");
        if (!visual)
        {
            visual = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath), player.transform)).transform;
            visual.name = "PlayerVisual";
        }
        visual.localPosition = capsule.center - Vector3.up * capsule.height * 0.5f;
        var animation = player.GetComponent<PlayerModelAnimation>();
        if (!animation) animation = player.gameObject.AddComponent<PlayerModelAnimation>();
        var serialized = new SerializedObject(animation);
        serialized.FindProperty("animator").objectReferenceValue = visual.GetComponentInChildren<Animator>();
        serialized.FindProperty("visualRoot").objectReferenceValue = visual;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(player.gameObject);
        if (PrefabUtility.IsPartOfPrefabInstance(player))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(oldRenderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animation);
        }
    }

    private static void Backup(string path)
    {
        string target = "Temp/PlayerAnimation/Backups/" + Path.GetFileName(path);
        if (!File.Exists(target)) File.Copy(path, target);
    }

    public static void ApplyAppearance(GameObject visual)
    {
        const string materialFolder = "Assets/Materials/PlayerClothing";
        const string meshFolder = "Assets/Models/SimpleHuman/Clothing";
        EnsureFolder(materialFolder);
        EnsureFolder(meshFolder);
        var skin = ClothingMaterial(materialFolder + "/Skin.mat", "#D6A17B", 0.25f);
        var shirt = ClothingMaterial(materialFolder + "/Shirt.mat", "#C9954F", 0.08f);
        var trousers = ClothingMaterial(materialFolder + "/Trousers.mat", "#34445C", 0.08f);
        var boots = ClothingMaterial(materialFolder + "/Boots.mat", "#49352A", 0.18f);
        foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            string name = renderer.name;
            if (name.EndsWith("Arm"))
            {
                SplitClothing(renderer, "UpperArm.", meshFolder + "/" + name + ".asset");
                renderer.sharedMaterials = new[] { skin, shirt };
            }
            else if (name.EndsWith("Leg"))
            {
                SplitClothing(renderer, "Foot.", meshFolder + "/" + name + ".asset");
                renderer.sharedMaterials = new[] { trousers, boots };
            }
            else renderer.sharedMaterials = new[] { name == "Body" ? shirt : name == "Pelvis" ? trousers : name.EndsWith("Foot") ? boots : skin };
            EditorUtility.SetDirty(renderer);
            if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }

    private static void SplitClothing(SkinnedMeshRenderer renderer, string bonePrefix, string path)
    {
        if (AssetDatabase.GetAssetPath(renderer.sharedMesh) == path) return;
        var source = renderer.sharedMesh;
        var weights = source.boneWeights;
        var triangles = source.triangles;
        var plain = new List<int>(); var clothed = new List<int>();
        float Weight(int vertex)
        {
            var w = weights[vertex];
            float Match(int bone, float value) => value > 0f && renderer.bones[bone].name.StartsWith(bonePrefix) ? value : 0f;
            return Match(w.boneIndex0, w.weight0) + Match(w.boneIndex1, w.weight1) + Match(w.boneIndex2, w.weight2) + Match(w.boneIndex3, w.weight3);
        }
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var target = (Weight(triangles[i]) + Weight(triangles[i + 1]) + Weight(triangles[i + 2])) / 3f > 0.5f ? clothed : plain;
            target.Add(triangles[i]); target.Add(triangles[i + 1]); target.Add(triangles[i + 2]);
        }
        if (plain.Count == 0 || clothed.Count == 0) throw new Exception("Clothing regions missing on " + renderer.name);
        var mesh = Object.Instantiate(source);
        mesh.name = renderer.name + "_Clothing";
        mesh.subMeshCount = 2;
        mesh.SetTriangles(plain, 0); mesh.SetTriangles(clothed, 1);
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved) { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
        else { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
        renderer.sharedMesh = saved;
    }

    private static Material ClothingMaterial(string path, string hex, float smoothness)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        ColorUtility.TryParseHtmlString(hex, out var color);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    [MenuItem("Tools/Player Animation/Apply Clothing Colors")]
    public static void PaintFromMenu() => Debug.Log(PaintPlayer());

    public static string PaintPlayer()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        foreach (string path in new[] { VisualPath, "Assets/Prefabs/Gameplay/GameplayRig.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (path == VisualPath) ApplyAppearance(root);
                else foreach (var player in root.GetComponentsInChildren<PlayerMovement>(true)) ApplyAppearance(player.transform.Find("PlayerVisual").gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var report = new StringBuilder("Clothing materials and skinned submeshes saved.\n");
        foreach (string name in new[] { "Prologue", "Labyrinth", "TheFinal" })
        {
            string path = "Assets/Scenes/" + name + ".unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerMovement>(true)).Single();
                var visual = player.transform.Find("PlayerVisual").gameObject;
                ApplyAppearance(visual);
                foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (renderer.sharedMaterials.Length != renderer.sharedMesh.subMeshCount || renderer.sharedMaterials.Any(m => !m || m.shader.name != "Universal Render Pipeline/Lit"))
                        throw new Exception("Material assignment failed: " + name + "/" + renderer.name);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                report.AppendLine(name + ": shirt, trousers, boots and skin assigned; Animator retained.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        AssetDatabase.SaveAssets();
        return report.ToString();
    }
}
