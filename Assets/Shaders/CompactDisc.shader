Shader "Leibowitz/Compact Disc"
{
    Properties
    {
        [Enum(Recording Surface,0,Clear Hub,1,Hole Mask,2)] _Part("Disc Part", Float) = 0
        _BaseColor("Silver / Plastic Tint", Color) = (0.72,0.76,0.79,1)
        _RimColor("Ring Edge Tint", Color) = (0.92,0.94,0.96,1)
        _InnerRadius("Inner Radius (mesh local units)", Range(0,0.5)) = 0.2
        _OuterRadius("Outer Radius (mesh local units)", Range(0,0.51)) = 0.5
        _Iridescence("Rainbow Strength", Range(0,1)) = 0.65
        _ReflectionStrength("Silver Reflection Fill", Range(0,0.5)) = 0.2
        _TrackDensity("Concentric Track Density", Range(40,1000)) = 260
        _Smoothness("Smoothness", Range(0,1)) = 0.86
        _Metallic("Metallic", Range(0,1)) = 0.9
        _Opacity("Opacity", Range(0,1)) = 1
        [HideInInspector] _SrcBlend("Source Blend", Float) = 1
        [HideInInspector] _DstBlend("Destination Blend", Float) = 0
        [HideInInspector] _ZWrite("Depth Write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" "UniversalMaterialType"="Lit" }
        LOD 200
        HLSLINCLUDE
        #include "CompactDisc.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DiscLitVertex
            #pragma fragment DiscFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            DiscVaryings DiscLitVertex(DiscAttributes input)
            {
                DiscVaryings output = DiscVertex(input);
                output.fogAndLighting.yzw = VertexLighting(output.positionWS, output.normalWS);
                return output;
            }

            half4 DiscFragment(DiscVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float radius = DiscRadiusAndClip(input.positionOS);
                float3 normal = normalize(input.normalWS);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float aa = max(fwidth(radius), 0.0001);
                float edgeDistance = min(radius - _InnerRadius, _OuterRadius - radius);
                float edge = 1.0 - smoothstep(0.002, 0.005 + aa, edgeDistance);
                SurfaceData surface = (SurfaceData)0;
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.alpha = _Opacity;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;

                if (_Part < 0.5)
                {
                    float3 radial = TransformObjectToWorldDir(float3(input.positionOS.x, 0, input.positionOS.z));
                    radial = normalize(radial);
                    Light mainLight = GetMainLight();
                    // Radial grooves split the reflected light into two broad spectral fans.
                    // Their colour changes with both viewing direction and disc rotation.
                    float radialView = dot(radial, view);
                    float radialLight = dot(radial, mainLight.direction);
                    float phase = (radialView * 0.85 + radialLight * 0.4 + radius * 0.35) * 2.2;
                    half3 spectrum = 0.5h + 0.5h * cos(6.2831853 * (phase + float3(0, 0.333333, 0.666667)));
                    float fan = pow(saturate(abs(radialView + radialLight) * 0.7), 3.0);
                    float amount = _Iridescence * (0.12 + fan * 0.88);
                    float trackVisibility = 1.0 - saturate(aa * _TrackDensity * 2.0);
                    float tracks = sin(radius * _TrackDensity * 6.2831853) * trackVisibility;
                    surface.albedo = lerp(_BaseColor.rgb, spectrum * 0.8 + 0.12, amount * 0.7);
                    surface.albedo *= 1.0 + tracks * 0.025;
                    surface.albedo = lerp(surface.albedo, _RimColor.rgb, edge * 0.65);
                    surface.smoothness = saturate(_Smoothness - tracks * 0.018);
                    // A procedural environment fill keeps silver readable in the inspection
                    // window as well as rooms that do not have a reflection probe.
                    float3 reflectedView = reflect(-view, normal);
                    float skyFill = 0.45 + 0.55 * saturate(reflectedView.y * 0.5 + 0.5);
                    surface.emission = spectrum * amount * 0.22
                        + _BaseColor.rgb * (_ReflectionStrength * skyFill) * (1.0 - amount * 0.45);
                }
                else
                {
                    float fresnel = pow(1.0 - saturate(abs(dot(normal, view))), 4.0);
                    surface.albedo = lerp(_BaseColor.rgb, _RimColor.rgb, edge);
                    surface.alpha = saturate(_Opacity + edge * 0.35 + fresnel * 0.3);
                    surface.emission = _RimColor.rgb * (edge * 0.06 + fresnel * 0.035);
                }

                InputData data = (InputData)0;
                data.positionWS = input.positionWS;
                data.positionCS = input.positionCS;
                data.normalWS = normal;
                data.viewDirectionWS = view;
                data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    data.shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #endif
                data.fogCoord = input.fogAndLighting.x;
                data.vertexLighting = input.fogAndLighting.yzw;
                data.bakedGI = SampleSH(normal);
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask = half4(1,1,1,1);
                half4 colour = UniversalFragmentPBR(data, surface);
                colour.rgb = MixFog(colour.rgb, data.fogCoord);
                colour.a = surface.alpha;
                return colour;
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
            #pragma vertex DiscShadowVertex
            #pragma fragment DiscShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            DiscVaryings DiscShadowVertex(DiscAttributes input)
            {
                DiscVaryings output = DiscVertex(input);
                float3 direction = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    direction = normalize(_LightPosition - output.positionWS);
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, output.normalWS, direction));
                output.positionCS = ApplyShadowClamping(output.positionCS);
                return output;
            }
            half4 DiscShadowFragment(DiscVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                DiscRadiusAndClip(input.positionOS);
                return 0;
            }
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
            #pragma vertex DiscVertex
            #pragma fragment DiscDepthFragment
            #pragma multi_compile_instancing
            half4 DiscDepthFragment(DiscVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                DiscRadiusAndClip(input.positionOS);
                return input.positionCS.z;
            }
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
            #pragma vertex DiscVertex
            #pragma fragment DiscNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            half4 DiscNormalsFragment(DiscVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                DiscRadiusAndClip(input.positionOS);
                float3 normal = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(normal);
                    return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0);
                #else
                    return half4(normal, 0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack Off
}
