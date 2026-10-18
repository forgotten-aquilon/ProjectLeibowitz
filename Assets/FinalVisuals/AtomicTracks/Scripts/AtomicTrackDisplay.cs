using UnityEngine;
using Leonardo.VisualObjects;

namespace Leonardo.AtomicTracks
{
    [ExecuteAlways, RequireComponent(typeof(MeshRenderer))]
    public sealed class AtomicTrackDisplay : MonoBehaviour, IVisualObjectAnimation
    {
        [Header("Particle film")]
        public Color background = new Color(.003f,.005f,.007f,1);
        [ColorUsage(false,true)] public Color ink = new Color(2.3f,2.15f,1.85f,1);
        [Range(.1f,1)] public float density = 1;
        [Tooltip("Track size across the entire surface. Smaller values reveal more tracks; larger values enlarge them.")]
        [Min(.01f)] public float patternScale = 1;
        [Range(.0004f,.004f)] public float penWidth = .0012f;
        [Range(0,1)] public float roughness = .7f;
        [Range(0,1)] public float brokenInk = .42f;
        [Range(0,1)] public float filmGrain = .24f;
        public int patternSeed = 17;
        [Header("Drawing animation")]
        public bool animate = true;
        public bool loop = true;
        [Min(1)] public float drawDuration = 16;
        [Min(0)] public float fullHoldDuration = 5;
        [Min(.1f)] public float clearDuration = 1.2f;
        [Min(0)] public float emptyHoldDuration = 1.5f;
        [Tooltip("Inspect a fixed stage of the drawing instead of the animation clock.")]
        public bool manualPreview;
        [Range(0,1)] public float manualProgress = 1;
        public bool previewAnimationInEditor;

        public bool IsAnimating { get => animate; set => animate = value; }
        public double AnimationTime => elapsed;
        public float CurrentProgress => manualPreview ? manualProgress : ProgressAt((float)elapsed);
        public float CurrentOpacity => manualPreview ? 1 : OpacityAt((float)elapsed);
        double elapsed, editorTime;
        MeshRenderer target;
        MaterialPropertyBlock properties;

        public float CycleDuration => emptyHoldDuration + drawDuration + fullHoldDuration + clearDuration;
        float CycleTime(float time) => loop ? Mathf.Repeat(time, CycleDuration) : Mathf.Min(time,emptyHoldDuration+drawDuration);
        public float ProgressAt(float time) => Mathf.Clamp01((CycleTime(time)-emptyHoldDuration)/drawDuration);
        public float OpacityAt(float time) => loop ? 1-Mathf.Clamp01((CycleTime(time)-emptyHoldDuration-drawDuration-fullHoldDuration)/clearDuration) : 1;
        void OnEnable()
        {
            target=GetComponent<MeshRenderer>();
#if UNITY_EDITOR
            editorTime=UnityEditor.EditorApplication.timeSinceStartup;
            UnityEditor.EditorApplication.update+=EditorUpdate;
#endif
            RefreshDisplay();
        }
        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update-=EditorUpdate;
#endif
        }
        void OnValidate()
        {
            patternScale=Mathf.Max(.01f,patternScale);
            drawDuration=Mathf.Max(1,drawDuration); fullHoldDuration=Mathf.Max(0,fullHoldDuration);
            clearDuration=Mathf.Max(.1f,clearDuration); emptyHoldDuration=Mathf.Max(0,emptyHoldDuration);
        }
        void Update()
        {
            if(Application.IsPlaying(gameObject) && animate && !manualPreview) elapsed+=Time.deltaTime;
            RefreshDisplay();
        }
#if UNITY_EDITOR
        void EditorUpdate()
        {
            if(this==null || !isActiveAndEnabled || UnityEditor.EditorUtility.IsPersistent(this) ||
                UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) return;
            double now=UnityEditor.EditorApplication.timeSinceStartup;
            float delta=(float)(now-editorTime); editorTime=now;
            if(Application.IsPlaying(gameObject) && !UnityEditor.EditorApplication.isPaused) return;
            if(!Application.IsPlaying(gameObject) && previewAnimationInEditor && animate && !manualPreview) elapsed+=Mathf.Min(delta,.1f);
            RefreshDisplay();
            if(previewAnimationInEditor) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
        [ContextMenu("Restart drawing")]
        public void RestartDrawing() { elapsed=0; RefreshDisplay(); }
        public void RefreshDisplay()
        {
            if(target==null) target=GetComponent<MeshRenderer>();
            if(target==null) return;
            if(properties==null) properties=new MaterialPropertyBlock();
            properties.SetFloat("_Progress",CurrentProgress);
            properties.SetFloat("_Opacity",CurrentOpacity);
            properties.SetColor("_Background",background); properties.SetColor("_Ink",ink);
            properties.SetFloat("_Density",density); properties.SetFloat("_Width",penWidth);
            properties.SetFloat("_PatternScale",Mathf.Max(.01f,patternScale));
            properties.SetFloat("_Roughness",roughness); properties.SetFloat("_Breakup",brokenInk);
            properties.SetFloat("_Grain",filmGrain); properties.SetFloat("_Seed",patternSeed);
            properties.SetFloat("_Aspect",Mathf.Abs(transform.lossyScale.x)/Mathf.Max(.001f,Mathf.Abs(transform.lossyScale.y)));
            target.SetPropertyBlock(properties);
        }
    }
}
