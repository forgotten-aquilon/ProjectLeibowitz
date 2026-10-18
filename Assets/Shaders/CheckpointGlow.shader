Shader "Leibowitz/Checkpoint Glow"
{
    Properties
    {
        [HDR] _BaseColor("Glow Colour", Color) = (1,0.6,0.15,1)
        _Intensity("Glow Intensity", Range(0,8)) = 2.5
        [Enum(Solid,0,Ribbon,1)] _Shape("Geometry Shading", Float) = 0
        _Fade("Visibility", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "CheckpointGlow"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GlowVertex
            #pragma fragment GlowFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Intensity;
                float _Shape;
                float _Fade;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings GlowVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 GlowFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float ribbon = smoothstep(0.0,0.3,input.uv.y) * (1.0-smoothstep(0.7,1.0,input.uv.y));
                float shape = lerp(1.0,ribbon,saturate(_Shape));
                return half4(_BaseColor.rgb * _Intensity, _BaseColor.a * _Fade * shape);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
