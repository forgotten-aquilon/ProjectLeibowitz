#ifndef LEIBOWITZ_PROCEDURAL_SURFACES_INCLUDED
#define LEIBOWITZ_PROCEDURAL_SURFACES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// The same layout is used by every pass for SRP Batcher compatibility.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _SecondaryColor;
    float4 _DetailColor;
    float4 _WoodDirection;
    float4 _BrickSize;
    float _Surface;
    float _PatternScale;
    float _Contrast;
    float _DetailStrength;
    float _Smoothness;
    float _HeightStrength;
    float _Seed;
    float _MortarWidth;
    float _WearStrength;
    float _StripeWidth;
    float _StripeAngle;
    float _RibSpacing;
    float _RibWidth;
CBUFFER_END

float SurfaceHash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float SurfaceNoise(float3 p)
{
    float3 cell = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(lerp(SurfaceHash(cell), SurfaceHash(cell + float3(1,0,0)), f.x),
             lerp(SurfaceHash(cell + float3(0,1,0)), SurfaceHash(cell + float3(1,1,0)), f.x), f.y),
        lerp(lerp(SurfaceHash(cell + float3(0,0,1)), SurfaceHash(cell + float3(1,0,1)), f.x),
             lerp(SurfaceHash(cell + float3(0,1,1)), SurfaceHash(cell + float3(1,1,1)), f.x), f.y), f.z);
}

float SurfaceFractal(float3 p)
{
    return SurfaceNoise(p) * 0.57 + SurfaceNoise(p * 2.07 + 7.1) * 0.29
        + SurfaceNoise(p * 4.13 + 13.7) * 0.14;
}

float SurfaceContrast(float value)
{
    return saturate((value - 0.5) * _Contrast + 0.5);
}

// Object anchored, measured in world units: moving/rotating an object does not
// slide the pattern, and stretching a wall does not stretch its surface detail.
float3 SurfaceCoordinates(float3 positionOS)
{
    float3 scale = float3(length(unity_ObjectToWorld._m00_m10_m20),
                          length(unity_ObjectToWorld._m01_m11_m21),
                          length(unity_ObjectToWorld._m02_m12_m22));
    return positionOS * scale * _PatternScale;
}

float2 SurfacePlane(float3 p, float3 normalOS)
{
    float3 n = abs(normalOS);
    if (n.y >= n.x && n.y >= n.z) return p.xz;
    if (n.x >= n.z) return p.zy;
    return p.xy;
}

// Distances to the two nearest cells; their difference forms stone cracks.
float2 SurfaceCells(float2 uv)
{
    float2 cell = floor(uv);
    float2 f = frac(uv);
    float2 closest = float2(8, 8);
    [unroll] for (int y = -1; y <= 1; ++y)
    {
        [unroll] for (int x = -1; x <= 1; ++x)
        {
            float2 offset = float2(x, y);
            float2 id = cell + offset;
            float2 jitter = float2(SurfaceHash(float3(id, _Seed)),
                                   SurfaceHash(float3(id, _Seed + 17.0)));
            float2 delta = offset + 0.2 + jitter * 0.6 - f;
            float d = dot(delta, delta);
            if (d < closest.x) closest = float2(d, closest.x);
            else closest.y = min(closest.y, d);
        }
    }
    return sqrt(closest);
}

struct ProceduralSurface
{
    half3 albedo;
    half metallic;
    half smoothness;
    float height;
};

ProceduralSurface EvaluateSurface(float3 p, float3 normalOS)
{
    ProceduralSurface s = (ProceduralSurface)0;
    s.smoothness = _Smoothness;
    float3 q = p + _Seed * float3(1.37, 2.91, 0.73);
    float broad = SurfaceFractal(q * 2.4);
    // Fade subpixel grain instead of letting it shimmer at a distance.
    float grainVisibility = 1.0 - saturate(max(length(ddx(p)), length(ddy(p))) * 22.0);
    float grain = (SurfaceNoise(q * 35.0) - 0.5) * grainVisibility * _DetailStrength;

#if defined(_SURFACE_WOOD)
    float3 axis = _WoodDirection.xyz;
    axis = dot(axis, axis) > 0.0001 ? normalize(axis) : float3(0,1,0);
    float3 reference = abs(axis.y) < 0.95 ? float3(0,1,0) : float3(1,0,0);
    float3 u = normalize(cross(axis, reference));
    float3 v = cross(axis, u);
    float along = dot(p, axis);
    float2 radial = float2(dot(p, u), dot(p, v));
    float warp = SurfaceFractal(float3(radial * 2.3, along * 0.35) + _Seed);
    float rings = length(radial + float2(0.34, -0.18)) * 14.0 + warp * 1.8;
    float ringWave = sin(rings * 6.2831853) * 0.5 + 0.5;
    float lines = SurfaceNoise(float3(radial * 26.0, along * 1.3) + _Seed);
    float endFace = pow(saturate(abs(dot(normalize(normalOS), axis))), 6.0);
    float value = lerp(ringWave * 0.45 + lines * 0.55, ringWave, endFace);
    value = SurfaceContrast(value);
    s.albedo = lerp(_BaseColor.rgb, _SecondaryColor.rgb, value);
    s.albedo = lerp(s.albedo, _DetailColor.rgb, smoothstep(0.76, 0.96, lines) * _DetailStrength * 0.35);
    s.height = value * 0.25 + grain * 0.12;

#elif defined(_SURFACE_STONE)
    float2 cells = SurfaceCells(SurfacePlane(p, normalOS) * 3.0);
    float edge = cells.y - cells.x;
    float aa = max(fwidth(edge), 0.008);
    float crack = 1.0 - smoothstep(0.015, 0.015 + aa, edge);
    float minerals = smoothstep(0.60, 0.78, SurfaceNoise(q * 13.0)) * _DetailStrength;
    s.albedo = lerp(_BaseColor.rgb, _SecondaryColor.rgb, SurfaceContrast(broad));
    s.albedo = lerp(s.albedo, _DetailColor.rgb, saturate(minerals * 0.6));
    s.albedo *= 1.0 - crack * 0.55 * _DetailStrength;
    s.height = broad * 0.45 - crack * 0.2 * _DetailStrength + grain * 0.15;

#elif defined(_SURFACE_BRICK)
    float2 uv = SurfacePlane(p, normalOS);
    float2 size = max(_BrickSize.xy, float2(0.05, 0.05));
    float row = floor(uv.y / size.y);
    uv.x += (frac(row * 0.5) * 2.0) * size.x * 0.5;
    float2 id = floor(uv / size);
    float2 local = frac(uv / size) * size;
    float2 distanceToEdge = min(local, size - local);
    float edge = min(distanceToEdge.x, distanceToEdge.y);
    float aa = max(fwidth(edge), 0.001);
    float brick = smoothstep(_MortarWidth * 0.5, _MortarWidth * 0.5 + aa, edge);
    float variation = SurfaceContrast(SurfaceHash(float3(id, _Seed)) * 0.65 + broad * 0.35);
    half3 clay = lerp(_BaseColor.rgb, _SecondaryColor.rgb, variation) * (1.0 + grain * 0.2);
    s.albedo = lerp(_DetailColor.rgb, clay, brick);
    s.height = brick * 0.55 + broad * 0.1 + grain * 0.12;
    s.smoothness *= brick;

#elif defined(_SURFACE_RUSTEDMETAL)
    float rust = smoothstep(0.43, 0.63, broad + SurfaceNoise(q * 11.0) * 0.16);
    float variation = SurfaceContrast(SurfaceNoise(q * 7.0));
    half3 oxide = lerp(_SecondaryColor.rgb, _DetailColor.rgb, variation);
    s.albedo = lerp(_BaseColor.rgb * (0.85 + broad * 0.3), oxide, rust);
    s.metallic = (1.0 - rust) * 0.85;
    s.smoothness = lerp(_Smoothness, 0.08, rust);
    s.height = rust * 0.22 + grain * 0.15;

#elif defined(_SURFACE_PLASTER)
    float wear = smoothstep(0.53, 0.76, broad);
    s.albedo = lerp(_BaseColor.rgb, _SecondaryColor.rgb, SurfaceContrast(broad) * 0.65);
    s.albedo = lerp(s.albedo, _DetailColor.rgb, wear * _DetailStrength * 0.6);
    s.albedo *= 1.0 + grain * 0.12;
    s.height = broad * 0.2 + grain * 0.15 - wear * 0.12;

#else // Concrete, also the default when no surface keyword is selected.
    float pore = smoothstep(0.66, 0.84, SurfaceNoise(q * 24.0)) * grainVisibility * _DetailStrength;
    s.albedo = lerp(_BaseColor.rgb, _SecondaryColor.rgb, SurfaceContrast(broad));
    s.albedo = lerp(s.albedo, _DetailColor.rgb, saturate(pore * 0.75));
    s.albedo *= 1.0 + grain * 0.16;
    s.height = broad * 0.2 + grain * 0.12 - pore * 0.3;
#endif
    s.albedo = max(s.albedo, 0.0);
    return s;
}

float3 SurfaceNormal(float3 positionWS, float3 normalWS, float height)
{
    float3 dx = ddx(positionWS);
    float3 dy = ddy(positionWS);
    float3 r1 = cross(dy, normalWS);
    float3 r2 = cross(normalWS, dx);
    float det = dot(dx, r1);
    float3 gradient = (ddx(height) * r1 + ddy(height) * r2)
        * sign(det) / max(abs(det), 0.0000001);
    return normalize(normalWS - gradient * _HeightStrength);
}

#endif
