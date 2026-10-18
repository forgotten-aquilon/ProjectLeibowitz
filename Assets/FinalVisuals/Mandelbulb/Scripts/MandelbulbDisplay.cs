using UnityEngine;
using Leonardo.VisualObjects;

namespace Leonardo.Mandelbulb
{
    public enum MandelbulbPalette { CopperAndTeal, Arctic, Spectrum }

    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class MandelbulbDisplay : MonoBehaviour, IVisualObjectAnimation
    {
        [Header("Fractal")]
        [Range(2,12)] public float power=8;
        [Range(.3f,2)] public float radius=1.05f;
        [Range(4,24)] public int fractalIterations=12;
        [Range(2,8)] public float bailout=4;
        [Header("Rendering")]
        [Range(48,256)] public int raySteps=160;
        [Tooltip("Smaller values reveal finer detail and cost more GPU time.")]
        [Range(.00015f,.004f)] public float surfaceEpsilon=.00065f;
        [Range(0,1)] public float ambientOcclusion=.8f;
        public bool selfShadows=true;
        [Range(0,1)] public float gloss=.4f;
        [Range(0,.5f)] public float rimGlow=.1f;
        [Range(.5f,3)] public float brightness=1.35f;
        [Header("Colour")]
        public MandelbulbPalette palette;
        public Color warmColor=new Color(1,.6f,.22f,1);
        public Color coolColor=new Color(.02f,.45f,.65f,1);
        [Range(.2f,4)] public float colorContrast=1.4f;
        [Range(0,6.28f)] public float colorPhase;
        [Header("Animation")]
        public bool animate=true;
        public bool autoRotate=true;
        [Range(0,20)] public float rotationSpeed=5;
        public bool animatePower;
        [Range(2,12)] public float minimumPower=6;
        [Range(2,12)] public float maximumPower=10;
        [Range(3,60)] public float powerCycleDuration=18;
        public bool animateColors;
        public bool previewAnimationInEditor;

        public bool IsAnimating { get => animate; set => animate=value; }
        public float EffectivePower => animatePower
            ? Mathf.Lerp(minimumPower, maximumPower, .5f-.5f*Mathf.Cos((float)(elapsed/powerCycleDuration)*Mathf.PI*2))
            : power;
        public double AnimationTime => elapsed;
        MeshRenderer targetRenderer;
        MaterialPropertyBlock properties;
        double elapsed, editorTime;
        float spin;

        void OnEnable()
        {
            targetRenderer=GetComponent<MeshRenderer>();
            properties=new MaterialPropertyBlock();
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
            power=Mathf.Clamp(power,2,12); radius=Mathf.Clamp(radius,.3f,2);
            fractalIterations=Mathf.Clamp(fractalIterations,4,24); bailout=Mathf.Clamp(bailout,2,8);
            raySteps=Mathf.Clamp(raySteps,48,256); surfaceEpsilon=Mathf.Clamp(surfaceEpsilon,.00015f,.004f);
            minimumPower=Mathf.Clamp(minimumPower,2,12); maximumPower=Mathf.Clamp(maximumPower,minimumPower,12);
            powerCycleDuration=Mathf.Clamp(powerCycleDuration,3,60);
        }
        void Update()
        {
            if (Application.IsPlaying(gameObject)) Advance(Time.deltaTime);
            RefreshDisplay();
        }
#if UNITY_EDITOR
        void EditorUpdate()
        {
            if (this==null || !isActiveAndEnabled || UnityEditor.EditorUtility.IsPersistent(this) ||
                UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) return;
            double now=UnityEditor.EditorApplication.timeSinceStartup;
            float delta=(float)(now-editorTime);
            editorTime=now;
            if (Application.IsPlaying(gameObject) && !UnityEditor.EditorApplication.isPaused) return;
            if (!Application.IsPlaying(gameObject) && previewAnimationInEditor)
                Advance(Mathf.Min(delta,.1f));
            RefreshDisplay();
            if (previewAnimationInEditor) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
        void Advance(float delta)
        {
            if (!animate) return;
            elapsed+=delta;
            if (autoRotate) spin=Mathf.Repeat(spin+rotationSpeed*delta,360);
        }
        public void RefreshDisplay()
        {
            if (targetRenderer==null) targetRenderer=GetComponent<MeshRenderer>();
            if (targetRenderer==null) return;
            if (properties==null) properties=new MaterialPropertyBlock();
            properties.SetFloat("_Power",EffectivePower);
            properties.SetFloat("_Radius",radius);
            properties.SetFloat("_Iterations",fractalIterations);
            properties.SetFloat("_Bailout",bailout);
            properties.SetFloat("_RaySteps",raySteps);
            properties.SetFloat("_Epsilon",surfaceEpsilon);
            properties.SetFloat("_AO",ambientOcclusion);
            properties.SetFloat("_SelfShadows",selfShadows?1:0);
            properties.SetFloat("_Gloss",gloss);
            properties.SetFloat("_RimGlow",rimGlow);
            properties.SetFloat("_Brightness",brightness);
            properties.SetFloat("_Palette",(float)palette);
            properties.SetColor("_WarmColor",warmColor);
            properties.SetColor("_CoolColor",coolColor);
            properties.SetFloat("_Contrast",colorContrast);
            properties.SetFloat("_ColorPhase",colorPhase+(animateColors?(float)elapsed*.12f:0));
            properties.SetFloat("_Spin",spin*Mathf.Deg2Rad);
            targetRenderer.SetPropertyBlock(properties);
            // The shader expands a unit cube. Match CPU culling to that expanded volume.
            targetRenderer.localBounds=new Bounds(Vector3.zero,Vector3.one*radius*4.05f);
        }
    }
}
