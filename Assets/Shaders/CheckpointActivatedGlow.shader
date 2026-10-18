Shader "Leibowitz/Checkpoint Activated Glow"
{
    Properties
    {
        [HDR] _GlowColor("Activated Blue", Color) = (0.04,0.32,1.4,1)
        _Intensity("Glow Intensity", Range(0,3)) = 0.85
        _EdgeWidth("Edge Width", Range(0.005,0.12)) = 0.025
        _RimPower("Rim Power", Range(1,8)) = 3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "ActivatedGlow"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GlowVertex
            #pragma fragment GlowFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                float _Intensity;
                float _EdgeWidth;
                float _RimPower;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings GlowVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                // A tiny shell prevents fighting with the original cube surface.
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz * 1.006);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalOS = input.normalOS;
                return output;
            }
            half4 GlowFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 p = abs(input.positionOS);
                float3 n = abs(input.normalOS);
                float2 face = n.y > 0.5 ? p.xz : (n.x > 0.5 ? p.yz : p.xy);
                float edgeDistance = 0.5 - max(face.x,face.y);
                float aa = max(fwidth(edgeDistance),0.0005);
                float edge = 1.0 - smoothstep(_EdgeWidth,_EdgeWidth + aa,edgeDistance);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float rim = pow(1.0-saturate(dot(normalize(input.normalWS),view)),_RimPower);
                float opacity = saturate(edge * 0.7 + rim * 0.2);
                return half4(_GlowColor.rgb * _Intensity, _GlowColor.a * opacity);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
