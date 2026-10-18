Shader "Leonardo/Mandelbulb"
{
    Properties
    {
        _Power ("Power", Range(2,12)) = 8
        _Radius ("Radius", Float) = 1.05
        _Iterations ("Fractal Iterations", Float) = 12
        _Bailout ("Bailout", Float) = 4
        _RaySteps ("Ray Steps", Float) = 160
        _Epsilon ("Surface Epsilon", Float) = 0.00065
        _AO ("Ambient Occlusion", Range(0,1)) = 0.8
        _SelfShadows ("Self Shadows", Float) = 1
        _Gloss ("Gloss", Range(0,1)) = 0.4
        _RimGlow ("Rim Glow", Range(0,0.5)) = 0.1
        _Brightness ("Brightness", Float) = 1.35
        _Palette ("Palette", Float) = 0
        _WarmColor ("Warm Colour", Color) = (1,0.6,0.22,1)
        _CoolColor ("Cool Colour", Color) = (0.02,0.45,0.65,1)
        _Contrast ("Colour Contrast", Float) = 1.4
        _ColorPhase ("Colour Phase", Float) = 0
        _Spin ("Rotation", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float _Power, _Radius, _Iterations, _Bailout, _RaySteps, _Epsilon;
            float _AO, _SelfShadows, _Gloss, _RimGlow, _Palette, _Brightness;
            float4 _WarmColor, _CoolColor;
            float _Contrast, _ColorPhase, _Spin;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        struct Output { half4 color : SV_Target; float depth : SV_Depth; };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input,output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float3 proxy=input.positionOS.xyz*(_Radius*4);
            output.positionWS=TransformObjectToWorld(proxy);
            output.positionCS=TransformWorldToHClip(output.positionWS);
            return output;
        }
        float3 FractalPoint(float3 p)
        {
            p/=_Radius;
            float s,c; sincos(_Spin,s,c);
            p.xz=float2(c*p.x-s*p.z,s*p.x+c*p.z);
            // Standard spherical-power convention: mathematical Z becomes Unity's up axis.
            return p.xzy;
        }
        float DistanceEstimate(float3 samplePoint,out float4 trap)
        {
            float3 p=FractalPoint(samplePoint);
            float3 z=p;
            float derivative=1;
            float r=length(z);
            trap=float4(abs(z),r);
            bool escaped=false;
            [loop] for (int i=0;i<24;i++)
            {
                if (i>=(int)_Iterations) break;
                r=length(z);
                trap=min(trap,float4(abs(z),r));
                if (r>_Bailout) { escaped=true; break; }
                float safeR=max(r,1e-8);
                float theta=acos(clamp(z.z/safeR,-1,1));
                float phi=dot(z.xy,z.xy)>1e-16 ? atan2(z.y,z.x) : 0;
                float radialDerivative=pow(safeR,_Power-1);
                derivative=min(radialDerivative*_Power*derivative+1,1e30);
                float zr=radialDerivative*safeR;
                float st,ct,sp,cp;
                sincos(theta*_Power,st,ct); sincos(phi*_Power,sp,cp);
                z=zr*float3(st*cp,st*sp,ct)+p;
            }
            r=length(z);
            if (!escaped && r<=_Bailout) return 0;
            return max(.5*log(max(r,1.000001))*r/max(derivative,1e-8),0)*_Radius;
        }
        float DE(float3 p) { float4 trap; return DistanceEstimate(p,trap); }
        bool March(Varyings input,out float3 hit,out float3 direction)
        {
            float3 directionWS=normalize(input.positionWS-GetCameraPositionWS());
            float3 origin=TransformWorldToObject(GetCameraPositionWS());
            direction=normalize(mul((float3x3)unity_WorldToObject,directionWS));
            if (unity_OrthoParams.w>.5)
            {
                directionWS=-UNITY_MATRIX_V[2].xyz;
                direction=normalize(mul((float3x3)unity_WorldToObject,directionWS));
                // Start on the near side of the rasterized proxy for orthographic cameras.
                origin=TransformWorldToObject(input.positionWS)-direction*(_Radius*8);
            }
            // Analytic bounds skip all fractal evaluation for rays that miss the specimen.
            float bound=_Radius*2;
            float b=dot(origin,direction);
            float discriminant=b*b-dot(origin,origin)+bound*bound;
            hit=0;
            if (discriminant<0) return false;
            float root=sqrt(discriminant);
            float travel=max(0,-b-root);
            float end=-b+root;
            if (end<=travel) return false;
            [loop] for (int step=0;step<256;step++)
            {
                if (step>=(int)_RaySteps || travel>end) break;
                float3 p=origin+direction*travel;
                float distance=DE(p);
                float tolerance=_Epsilon*_Radius*max(1,travel/5);
                if (distance<tolerance) {hit=p; return true;}
                travel+=max(distance*.72,tolerance*.25);
            }
            return false;
        }
        float3 Normal(float3 p)
        {
            float h=max(_Epsilon*_Radius*1.5,.00015*_Radius);
            float2 e=float2(1,-1)*.57735027;
            float3 n=e.xyy*DE(p+e.xyy*h)+e.yyx*DE(p+e.yyx*h)
                +e.yxy*DE(p+e.yxy*h)+e.xxx*DE(p+e.xxx*h);
            float sq=dot(n,n);
            return sq>1e-20 ? n*rsqrt(sq) : normalize(p+float3(0,1e-5,0));
        }
        float AmbientOcclusion(float3 p,float3 normal)
        {
            float occlusion=0;
            [unroll] for(int i=1;i<=4;i++)
            {
                float h=(.012+i*i*.012)*_Radius;
                occlusion+=max(0,h-DE(p+normal*h))/h*.18;
            }
            return saturate(1-occlusion*_AO);
        }
        float Shadow(float3 p,float3 light)
        {
            if (_SelfShadows<.5) return 1;
            float shade=1;
            float travel=.008*_Radius;
            [loop] for(int i=0;i<20;i++)
            {
                float d=DE(p+light*travel);
                shade=min(shade,12*d/travel);
                travel+=clamp(d,.012*_Radius,.16*_Radius);
                if (shade<.04 || travel>2*_Radius) break;
            }
            return clamp(shade,.16,1);
        }
        float HitDepth(float3 hit)
        {
            float4 clip=TransformObjectToHClip(hit);
            float depth=clip.z/clip.w;
            #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
                depth=depth*.5+.5;
            #endif
            return depth;
        }
        ENDHLSL
        Pass
        {
            Name "MandelbulbForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Front
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            Output Frag(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 hit,direction;
                if (!March(input,hit,direction)) discard;
                float3 normal=Normal(hit);
                float3 normalWS=normalize(TransformObjectToWorldNormal(normal));
                float3 view=normalize(GetCameraPositionWS()-TransformObjectToWorld(hit));
                Light main=GetMainLight();
                float3 key=normalize(float3(-.5,.75,-.75));
                float3 keyOS=normalize(mul((float3x3)unity_WorldToObject,key));
                float ao=AmbientOcclusion(hit,normal);
                float shadow=Shadow(hit+normal*(_Epsilon*_Radius*4),keyOS);
                float4 trap; DistanceEstimate(hit,trap);
                float pattern=pow(saturate(.5+.5*sin(11*trap.w+9*trap.x-7*trap.z+_ColorPhase)),_Contrast);
                float3 baseColor=lerp(_CoolColor.rgb,_WarmColor.rgb,smoothstep(.12,.85,pattern));
                if (_Palette>.5 && _Palette<1.5)
                    baseColor=lerp(float3(.015,.09,.22),float3(.6,.9,1),pattern);
                if (_Palette>1.5)
                    baseColor=.5+.5*cos(6.28318*(pattern+float3(0,.33,.67))+_ColorPhase);
                float ndl=saturate(dot(normalWS,key));
                float fill=saturate(dot(normalWS,normalize(float3(.7,.3,-.6))));
                float3 studioLight=float3(1.1,.9,.7)+main.color*.3;
                float3 lighting=float3(.13,.17,.21)+studioLight*ndl*shadow+float3(.14,.3,.4)*fill;
                float3 color=baseColor*lighting*ao;
                float3 halfVector=normalize(key+view);
                color+=studioLight*pow(saturate(dot(normalWS,halfVector)),40)*_Gloss*shadow*ao*.5;
                color+=pow(1-saturate(dot(normalWS,view)),3)*_RimGlow*float3(.1,.6,1)*ao;
                float4 hitClip=TransformObjectToHClip(hit);
                color=MixFog(color*_Brightness,ComputeFogFactor(hitClip.z));
                Output output; output.color=half4(color,1); output.depth=HitDepth(hit);
                return output;
            }
            ENDHLSL
        }
        Pass
        {
            Name "MandelbulbDepth"
            Tags { "LightMode"="DepthOnly" }
            Cull Front
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragDepth
            #pragma multi_compile_instancing
            Output FragDepth(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 hit,direction;
                if (!March(input,hit,direction)) discard;
                Output output; output.color=0; output.depth=HitDepth(hit); return output;
            }
            ENDHLSL
        }
        Pass
        {
            Name "MandelbulbDepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragNormals
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            Output FragNormals(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 hit,direction;
                if (!March(input,hit,direction)) discard;
                float3 normalWS=normalize(TransformObjectToWorldNormal(Normal(hit)));
                Output output;
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct=PackNormalOctQuadEncode(normalWS);
                    output.color=half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                    output.color=half4(normalWS,0);
                #endif
                output.depth=HitDepth(hit);
                return output;
            }
            ENDHLSL
        }
    }
}
