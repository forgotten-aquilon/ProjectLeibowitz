#ifndef LEIBOWITZ_COMPACT_DISC_INCLUDED
#define LEIBOWITZ_COMPACT_DISC_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half4 _RimColor;
    float _Part;
    float _InnerRadius;
    float _OuterRadius;
    float _TrackDensity;
    half _Iridescence;
    half _ReflectionStrength;
    half _Smoothness;
    half _Metallic;
    half _Opacity;
    float _SrcBlend;
    float _DstBlend;
    float _ZWrite;
CBUFFER_END

// Each part is a Unity cylinder, with the disc lying in its local XZ plane.
// Clipping is shared by colour, depth, normals and shadows: the hole stays open.
float DiscRadiusAndClip(float3 positionOS)
{
    float radius = length(positionOS.xz);
    clip(_Part > 1.5 ? -1.0 : min(radius - _InnerRadius, _OuterRadius - radius));
    return radius;
}

struct DiscAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DiscVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 positionOS : TEXCOORD2;
    half4 fogAndLighting : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

DiscVaryings DiscVertex(DiscAttributes input)
{
    DiscVaryings output = (DiscVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = position.positionCS;
    output.positionWS = position.positionWS;
    output.positionOS = input.positionOS.xyz;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.fogAndLighting.x = ComputeFogFactor(position.positionCS.z);
    return output;
}

#endif
