// Assets/Shaders/Rubik/Sticker_TwoColorGrad_URP.shader
Shader "Rubik/Sticker_TwoColorGrad_URP"{
    Properties{
        _Saturation("Saturation",Range(0,1))=0.9
        _Value("Value",Range(0,1))=0.9
        _HueUp("Hue Up",Range(0,1))=0.00
        _HueDown("Hue Down",Range(0,1))=0.50
        _HueLeft("Hue Left",Range(0,1))=0.16
        _HueRight("Hue Right",Range(0,1))=0.33
        _HueFront("Hue Front",Range(0,1))=0.66
        _HueBack("Hue Back",Range(0,1))=0.83
        // per-renderer（MPB）
        [PerRendererData]_FaceIndex("FaceIndex",Float)=0
        [PerRendererData]_RubikCenter("RubikCenter",Vector)=(0,0,0,1)
        [PerRendererData]_AnimSpeed("AnimSpeed",Float)=0.4
        [PerRendererData]_PulseHz("PulseHz",Float)=1.2
        [PerRendererData]_HuePairDelta("Two-Color Hue Delta",Float)=0.12
        [PerRendererData]_GradScale("GradScale",Float)=0.35
        [PerRendererData]_UVRotDeg("UVRotDeg",Float)=0
        [PerRendererData]_Seed("Seed",Float)=0
        [PerRendererData]_Pattern("Pattern",Float)=0 // U/V/放射/ストライプ/チェッカー
    }
    SubShader{
        Tags{"RenderPipeline"="UniversalRenderPipeline" "Queue"="Geometry" "RenderType"="Opaque"}
        Cull Back ZWrite On
        Pass{
        Name "Forward" Tags{"LightMode"="UniversalForward"}
        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/Shaders/Rubik/StickerCore.hlsl"

        struct A{ float4 positionOS:POSITION; float3 normalOS:NORMAL; };
        struct V{ float4 positionHCS:SV_POSITION; float3 wpos:TEXCOORD0; float3 wnorm:TEXCOORD1; };

        CBUFFER_START(UnityPerMaterial)
            float _Saturation,_Value;
            float _HueUp,_HueDown,_HueLeft,_HueRight,_HueFront,_HueBack;
        CBUFFER_END

        CBUFFER_START(UnityPerDraw)
            float _FaceIndex;
            float4 _RubikCenter;
            float _AnimSpeed,_PulseHz,_HuePairDelta,_GradScale,_UVRotDeg,_Seed,_Pattern;
        CBUFFER_END

        V vert(A IN){ V O; VertexPositionInputs p=GetVertexPositionInputs(IN.positionOS.xyz);
            O.positionHCS=p.positionCS; O.wpos=p.positionWS; O.wnorm=TransformObjectToWorldNormal(IN.normalOS); return O; }

        half4 frag(V i):SV_Target{
            float3 n=normalize(i.wnorm), u,v; BuildBasis(n,u,v);
            float3 p=i.wpos - _RubikCenter.xyz; float2 uv=float2(dot(p,u),dot(p,v));
            uv = Rotate2(uv,_UVRotDeg);

            // 面内パターン値 g
            float g=0;
            if (_Pattern<0.5)      g=uv.x;
            else if (_Pattern<1.5) g=uv.y;
            else if (_Pattern<2.5) g=length(uv);
            else if (_Pattern<3.5) g=sin(6.28318*uv.x);
            else                   g=sin(6.28318*uv.x)*sin(6.28318*uv.y);

            float t=_Time.y;
            float baseHue = GetHueBase(_FaceIndex,_HueUp,_HueDown,_HueLeft,_HueRight,_HueFront,_HueBack);
            float hueA=baseHue, hueB=frac(baseHue+_HuePairDelta);
            float pulse=0.5 + 0.5*sin(6.28318*(_PulseHz*t + _Seed));
            float hue=lerp(hueA,hueB,pulse);
            hue = frac(hue + _GradScale * g); // 2色に面グラデを加算

            float3 rgb=HSV2RGB(float3(hue,saturate(_Saturation),saturate(_Value)));
            return half4(rgb,1);
        }
        ENDHLSL
        }
    }
}
