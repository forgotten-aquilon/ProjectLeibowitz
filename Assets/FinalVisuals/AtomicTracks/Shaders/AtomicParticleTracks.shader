Shader "Leonardo/Atomic Particle Tracks"
{
    Properties
    {
        _Background("Film background", Color) = (0.003,0.005,0.007,1)
        [HDR] _Ink("Track ink", Color) = (2.3,2.15,1.85,1)
        _Progress("Exposure / draw progress", Range(0,1)) = 1
        _Opacity("Exposure opacity", Range(0,1)) = 1
        _Density("Track density", Range(0.1,1)) = 1
        _PatternScale("Pattern scale", Float) = 1
        _Width("Pen width", Range(0.0004,0.004)) = 0.0012
        _Roughness("Pen irregularity", Range(0,1)) = 0.7
        _Breakup("Broken ink", Range(0,1)) = 0.42
        _Grain("Film grain", Range(0,1)) = 0.24
        _Seed("Pattern seed", Float) = 17
        _Aspect("Surface aspect", Float) = 1.38
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ParticleFilm"
            Tags { "LightMode"="UniversalForward" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Background, _Ink;
                float _Progress, _Opacity, _Density, _PatternScale, _Width, _Roughness, _Breakup, _Grain, _Seed, _Aspect;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; return o;
            }
            #define TAU 6.28318530718
            float Hash(float n) { return frac(sin(n*127.1+_Seed*9.13)*43758.5453); }
            float Hash2(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float x)
            {
                float i=floor(x),f=frac(x); f=f*f*(3-2*f); return lerp(Hash(i),Hash(i+1),f);
            }
            // Stable, spatial pen texture: it never crawls when the drawing is paused.
            float InkStroke(float distance, float along, float id, float brightness, float aa)
            {
                float pressure=lerp(1,0.55+0.9*Noise(along*74+id*19),_Roughness);
                float edge=(Noise(along*185+id*31)-0.5)*_Width*_Roughness;
                float width=_Width*pressure*lerp(0.65,1.3,Hash(id+101));
                float stroke=1-smoothstep(width-aa,width+aa,abs(distance+edge));
                float dots=Hash(floor(along*480)+id*179);
                float broken=lerp(1,smoothstep(_Breakup*0.35,_Breakup*0.35+0.1,dots),saturate(_Breakup*3));
                float dry=lerp(1,0.52+0.48*Hash(floor(along*1150)+id*313),_Roughness);
                return stroke*broken*dry*brightness;
            }
            float Drawn(float progress, float id, float order)
            {
                float onset=order+Hash(id+47)*0.36;
                return saturate((progress-onset)/lerp(0.12,0.34,Hash(id+38)));
            }
            float2 Polar(float angle) { return float2(cos(angle),sin(angle)); }
            // Each cell owns complete trajectories. Neighbour cells are sampled as well,
            // so paths cross cell boundaries without clipping or repeating a texture.
            float TrackCell(float2 p, float cellSeed, float aa)
            {
                float ink=0,heads=0;
                // Nearly parallel beam tracks mixed with sharp collision rays.
                [loop] for(int i=0;i<6;i++)
                {
                    float id=i+1+cellSeed;
                    if(Hash(id+702)>_Density) continue;
                    float angle=i<3 ? (Hash(id+8)-0.5)*0.38 : (Hash(id+8)-0.5)*TAU;
                    float2 a=i<3 ? float2(-1.7,lerp(-0.25,0.3,Hash(id+4)))
                        : float2(lerp(-0.8,0.55,Hash(floor(id/5)+19)),lerp(-0.45,0.4,Hash(floor(id/5)+88)));
                    float2 dir=Polar(angle), normal=float2(-dir.y,dir.x);
                    float len=i<3?3.6:lerp(0.65,3.4,Hash(id+70));
                    float t=dot(p-a,dir), u=t/len;
                    float bend=i<3?0:lerp(-0.07,0.07,Hash(id+96));
                    float wobble=(Noise(t*23+id*9)-0.5)*0.004*_Roughness;
                    float kink=(i>=3 && Hash(id+210)>0.65)?max(0,t-len*0.37)*lerp(-0.1,0.1,Hash(id+201)):0;
                    float d=dot(p-a,normal)-bend*t*t-kink-wobble;
                    float reveal=Drawn(_Progress,id,0.02);
                    if(u>=0 && u<=reveal && reveal>0)
                        ink=max(ink,InkStroke(d,t,id,lerp(0.5,1,Hash(id+18)),aa));
                    float tipKink=(i>=3 && Hash(id+210)>0.65)?max(0,len*reveal-len*0.37)*lerp(-0.1,0.1,Hash(id+201)):0;
                    float2 tip=a+dir*len*reveal+normal*(bend*len*len*reveal*reveal+tipKink);
                    heads=max(heads,(1-smoothstep(0.001,0.006,length(p-tip)))*step(0.001,reveal)*(1-step(0.995,reveal)));
                }
                // Broad partial arcs, slightly faceted as if traced with a fine nib.
                [loop] for(int j=0;j<5;j++)
                {
                    float id=j+61+cellSeed;
                    if(Hash(id+702)>_Density) continue;
                    float2 center=float2(lerp(-1.4,1.4,Hash(id+11)),lerp(-1.05,1.05,Hash(id+26)));
                    float radius=lerp(0.18,1.28,Hash(id+77));
                    float start=Hash(id+44)*TAU;
                    float sweep=lerp(1.6,5.8,Hash(id+23));
                    float direction=Hash(id+87)>0.5?1:-1;
                    float2 q=p-center;
                    float angle=atan2(q.y,q.x);
                    float relative=frac((angle-start)*direction/TAU+1)*TAU;
                    float t=relative/sweep;
                    float reveal=Drawn(_Progress,id,0.14);
                    float facet=TAU/lerp(100,190,Hash(id+7));
                    float angularCell=(frac((angle+3.14159265)/facet)-0.5)*facet;
                    float wobble=(Noise(relative*radius*42+id*17)-0.5)*0.006*_Roughness;
                    float d=length(q)-radius/cos(angularCell)-wobble;
                    if(t<=reveal && reveal>0)
                        ink=max(ink,InkStroke(d,relative*radius,id,lerp(0.45,1,Hash(id+98)),aa));
                }
                // Logarithmic curls: select the nearest winding, then reveal along its trajectory.
                [loop] for(int k=0;k<4;k++)
                {
                    float id=k+111+cellSeed;
                    if(Hash(id+702)>_Density) continue;
                    float2 center=float2(lerp(-1.18,1.18,Hash(id+11)),lerp(-0.79,0.79,Hash(id+26)));
                    float outer=lerp(0.13,0.7,Hash(id+17));
                    float turns=lerp(2.2,4.8,Hash(id+93))*TAU;
                    float decay=lerp(2.7,4.4,Hash(id+53));
                    float start=Hash(id+28)*TAU;
                    float direction=Hash(id+89)>0.5?1:-1;
                    float2 q=p-center;
                    float r=max(length(q),0.0001);
                    float theta=frac((atan2(q.y,q.x)-start)*direction/TAU+1)*TAU;
                    float radialT=-log(r/outer)/decay;
                    float winding=floor((radialT*turns-theta)/TAU+0.5);
                    float reveal=Drawn(_Progress,id,0.23);
                    // Adjacent turns matter near a drawing head and the outer end.
                    [unroll] for(int n=-1;n<=1;n++)
                    {
                        float t=(theta+(winding+n)*TAU)/turns;
                        if(t<0 || t>reveal || reveal<=0) continue;
                        float target=outer*exp(-decay*t);
                        float wobble=(Noise(t*turns*target*68+id*7)-0.5)*0.0035*_Roughness;
                        float d=(r-target-wobble)/sqrt(1+pow(decay/turns,2));
                        ink=max(ink,InkStroke(d,t*turns*outer,id,lerp(0.65,1,Hash(id+24)),aa));
                    }
                }
                return ink+heads*0.25;
            }
            half4 Frag(Varyings v) : SV_Target
            {
                float2 surface=(v.uv-0.5)*float2(_Aspect,1)*2;
                float2 p=surface/max(_PatternScale,0.01);
                float aa=max(length(fwidth(p))*0.45,0.00018);
                // An unbounded seeded field replaces the old finite cluster. The cost
                // per pixel stays fixed even when scale exposes hundreds of cells.
                const float spacing=1.4;
                const float trajectoryScale=0.42;
                float2 cellIndex=floor(p/spacing+0.5);
                float ink=0;
                // Long beam families span the film at every scale. Only nearby beams
                // need evaluation; their drawing heads travel across the visible surface.
                [unroll] for(int family=0;family<3;family++)
                {
                    float angle=family==0?0:(family==1?0.67:-0.48);
                    angle+=(Hash(family+815)-0.5)*0.12;
                    float2 dir=Polar(angle),normal=float2(-dir.y,dir.x);
                    float gap=family==0?0.26:0.63;
                    float row=floor(dot(p,normal)/gap+0.5);
                    [unroll] for(int nearby=-1;nearby<=1;nearby++)
                    {
                        float index=row+nearby;
                        float id=Hash(index+family*79+912)*63+family*37+401;
                        float offset=(index+(Hash(id+35)-0.5)*0.5)*gap;
                        float along=dot(p,dir);
                        float d=dot(p,normal)-offset-(Noise(along*23+id*9)-0.5)*0.004*_Roughness;
                        float extent=abs(dir.x)*_Aspect+abs(dir.y);
                        float u=(dot(surface,dir)+extent)/(2*extent);
                        float reveal=Drawn(_Progress,id,0.02);
                        if(Hash(id+702)<=_Density && u<=reveal && reveal>0)
                            ink=max(ink,InkStroke(d,along,id,lerp(0.45,0.95,Hash(id+18)),aa));
                    }
                }
                [unroll] for(int y=-1;y<=1;y++)
                {
                    [unroll] for(int x=-1;x<=1;x++)
                    {
                        float2 neighbour=cellIndex+float2(x,y);
                        float2 jitter=float2(Hash2(neighbour+7.13+_Seed),Hash2(neighbour+41.71+_Seed))-0.5;
                        float2 local=(p-neighbour*spacing-jitter*0.4)/trajectoryScale;
                        float seed=Hash2(neighbour+float2(_Seed*0.17,_Seed*0.31))*63;
                        ink=max(ink,TrackCell(local,seed,aa/trajectoryScale));
                    }
                }
                // Small chamber droplets appear with the exposure, never before the first track.
                float2 cell=floor((p+2)*215);
                float random=Hash2(cell+_Seed);
                float mote=step(0.9978,random)*(1-smoothstep(0.12,0.36,length(frac((p+2)*215)-0.5)));
                mote*=step(Hash2(cell+13)*0.8+0.12,_Progress)*_Grain;
                float grain=Hash2(floor(v.uv*float2(1900,1400))+_Seed);
                float3 background=_Background.rgb*(0.94+grain*_Grain*0.35);
                float coverage=saturate((ink+mote)*_Opacity);
                return half4(lerp(background,_Ink.rgb,coverage),1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
