Shader "Leibowitz/Procedural Surfaces"
{
    Properties
    {
        [KeywordEnum(Concrete, Wood, Stone, Brick, RustedMetal, Plaster)] _Surface("Surface", Float) = 0
        _BaseColor("Base Color", Color) = (0.45,0.46,0.44,1)
        _SecondaryColor("Secondary Color", Color) = (0.67,0.66,0.62,1)
        _DetailColor("Detail / Mortar Color", Color) = (0.2,0.21,0.2,1)
        _PatternScale("Pattern Scale (per metre)", Range(0.1,8)) = 1
        _Contrast("Pattern Contrast", Range(0.2,3)) = 1.5
        _DetailStrength("Detail Strength", Range(0,1)) = 0.7
        _Smoothness("Smoothness", Range(0,1)) = 0.15
        _HeightStrength("Surface Relief", Range(0,0.08)) = 0.015
        _Seed("Pattern Seed", Float) = 1
        _WoodDirection("Wood Grain Direction (local)", Vector) = (0,1,0,0)
        _BrickSize("Brick Width / Height (metres)", Vector) = (0.4,0.2,0,0)
        _MortarWidth("Mortar Width (metres)", Range(0.005,0.08)) = 0.025
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Lit" }
        LOD 200

        HLSLINCLUDE
        #include "ProceduralSurfaces.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            // ForwardOnly also renders in the project's Deferred renderer.
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SurfaceVertex
            #pragma fragment SurfaceFragment
            #pragma shader_feature_local_fragment _SURFACE_CONCRETE _SURFACE_WOOD _SURFACE_STONE _SURFACE_BRICK _SURFACE_RUSTEDMETAL _SURFACE_PLASTER
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 pattern : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                half4 fogAndLighting : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings SurfaceVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalOS = input.normalOS;
                output.pattern = SurfaceCoordinates(input.positionOS.xyz);
                output.fogAndLighting = half4(ComputeFogFactor(position.positionCS.z),
                    VertexLighting(position.positionWS, output.normalWS));
                return output;
            }
            half4 SurfaceFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ProceduralSurface s = EvaluateSurface(input.pattern, input.normalOS);
                InputData data = (InputData)0;
                data.positionWS = input.positionWS;
                data.positionCS = input.positionCS;
                data.normalWS = SurfaceNormal(input.positionWS, normalize(input.normalWS), s.height);
                data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    data.shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #endif
                data.fogCoord = input.fogAndLighting.x;
                data.vertexLighting = input.fogAndLighting.yzw;
                data.bakedGI = SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask = half4(1,1,1,1);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = s.albedo;
                surface.metallic = s.metallic;
                surface.smoothness = s.smoothness;
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.alpha = 1;
                half4 color = UniversalFragmentPBR(data, surface);
                color.rgb = MixFog(color.rgb, data.fogCoord);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex NormalVertex
            #pragma fragment NormalFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma shader_feature_local_fragment _SURFACE_CONCRETE _SURFACE_WOOD _SURFACE_STONE _SURFACE_BRICK _SURFACE_RUSTEDMETAL _SURFACE_PLASTER
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            struct NormalAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct NormalVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 pattern : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            NormalVaryings NormalVertex(NormalAttributes input)
            {
                NormalVaryings output = (NormalVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalOS = input.normalOS;
                output.pattern = SurfaceCoordinates(input.positionOS.xyz);
                return output;
            }
            half4 NormalFragment(NormalVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ProceduralSurface s = EvaluateSurface(input.pattern, input.normalOS);
                float3 n = SurfaceNormal(input.positionWS, normalize(input.normalWS), s.height);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(n);
                    return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0);
                #else
                    return half4(n, 0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack Off
}
