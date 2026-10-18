#ifndef LEIBOWITZ_LABORATORY_SURFACES_INCLUDED
#define LEIBOWITZ_LABORATORY_SURFACES_INCLUDED

#include "ProceduralSurfaces.hlsl"

// Cell edges in local, metre-scaled coordinates. Derivatives soften subpixel seams.
float LaboratoryJoint(float2 uv, out float2 id)
{
    float2 size = max(_BrickSize.xy, float2(0.05, 0.05));
    uv += size * 0.5;
    id = floor(uv / size);
    float2 local = frac(uv / size) * size;
    float2 toEdge = min(local, size - local);
    float edge = min(toEdge.x, toEdge.y);
    float aa = max(fwidth(edge), 0.001);
    return 1.0 - smoothstep(_MortarWidth * 0.5, _MortarWidth * 0.5 + aa, edge);
}

ProceduralSurface EvaluateLaboratorySurface(float3 p, float3 normalOS)
{
    ProceduralSurface s = (ProceduralSurface)0;
    s.smoothness = _Smoothness;
    float2 uv = SurfacePlane(p, normalOS);
    float3 q = p + _Seed * float3(1.37, 2.91, 0.73);
    float broad = SurfaceFractal(q * 2.4);
    float visibility = 1.0 - saturate(max(length(ddx(p)), length(ddy(p))) * 22.0);
    float grain = (SurfaceNoise(q * 25.0) - 0.5) * visibility * _DetailStrength;
    float wear = smoothstep(0.52, 0.73, SurfaceContrast(broad)) * _WearStrength;

#if defined(_SURFACE_LABPANELS)
    float2 id;
    float joint = LaboratoryJoint(uv, id);
    float tint = SurfaceHash(float3(id, _Seed));
    s.albedo = lerp(_BaseColor.rgb, _SecondaryColor.rgb, tint * 0.25 + broad * 0.12);
    s.albedo = lerp(s.albedo, _SecondaryColor.rgb, wear * 0.45);
    s.albedo = lerp(s.albedo, _DetailColor.rgb, joint);
    s.height = -joint * 0.35 + grain * 0.03;
    s.smoothness *= 1.0 - joint * 0.85;

#elif defined(_SURFACE_TILES)
    float2 id;
    float joint = LaboratoryJoint(uv, id);
    float tint = SurfaceHash(float3(id, _Seed));
    float stained = smoothstep(0.72, 0.96, tint) * _WearStrength;
    half3 tile = lerp(_BaseColor.rgb, _SecondaryColor.rgb, tint * 0.45 + stained * 0.35);
    tile *= 1.0 + grain * 0.08;
    s.albedo = lerp(tile, _DetailColor.rgb, joint);
    s.height = -joint * 0.45 + broad * 0.025;
    s.smoothness *= (1.0 - joint * 0.9) * (1.0 - stained * 0.4);

#elif defined(_SURFACE_PAINTEDMETAL)
    float chipped = smoothstep(0.57, 0.73, SurfaceContrast(broad + SurfaceNoise(q * 8.0) * 0.12));
    chipped *= _WearStrength;
    float bare = smoothstep(0.4, 0.7, SurfaceNoise(q * 5.0));
    half3 undercoat = lerp(_SecondaryColor.rgb, _DetailColor.rgb, bare);
    s.albedo = lerp(_BaseColor.rgb * (0.92 + broad * 0.16), undercoat, chipped);
    s.metallic = lerp(0.15, 0.85, chipped * bare);
    s.smoothness = lerp(_Smoothness, 0.25, chipped);
    s.height = -chipped * 0.2 + grain * 0.04;

#elif defined(_SURFACE_RUBBER)
    float spacing = max(_RibSpacing, 0.02);
    float distanceToRib = abs(frac(uv.x / spacing) - 0.5) * spacing;
    float aa = max(fwidth(distanceToRib), 0.001);
    float rib = 1.0 - smoothstep(_RibWidth * 0.5, _RibWidth * 0.5 + aa, distanceToRib);
    s.albedo = lerp(_BaseColor.rgb, _SecondaryColor.rgb, rib);
    s.albedo = lerp(s.albedo, _DetailColor.rgb, wear * rib * 0.4);
    s.height = rib * 0.3 + grain * 0.025;
    s.smoothness *= 0.7 + rib * 0.3;

#elif defined(_SURFACE_HAZARD)
    float angle = radians(_StripeAngle);
    float along = dot(uv, float2(cos(angle), sin(angle)));
    float wave = sin(along * 3.14159265 / max(_StripeWidth, 0.02));
    float aa = max(fwidth(wave), 0.01);
    float stripe = smoothstep(-aa, aa, wave);
    s.albedo = lerp(_SecondaryColor.rgb, _BaseColor.rgb, stripe);
    s.albedo = lerp(s.albedo, _DetailColor.rgb, wear * 0.6);
    s.height = grain * 0.05 - wear * 0.05;
    s.smoothness *= 1.0 - wear * 0.65;

#else // Epoxy floor: broad wear, restrained mineral flecks and a satin finish.
    float fleck = smoothstep(0.69, 0.83, SurfaceNoise(q * 18.0)) * visibility * _DetailStrength;
    s.albedo = lerp(_BaseColor.rgb, _SecondaryColor.rgb, SurfaceContrast(broad) * 0.7);
    s.albedo = lerp(s.albedo, _DetailColor.rgb, wear * 0.45 + fleck * 0.3);
    s.height = broad * 0.06 + grain * 0.035;
    s.smoothness *= 1.0 - wear * 0.55;
#endif
    s.albedo = max(s.albedo, 0.0);
    return s;
}

#endif
