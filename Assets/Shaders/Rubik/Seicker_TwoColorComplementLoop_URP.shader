Shader "Rubik/Sticker_TwoColorComplementLoop_URP"
{
    Properties
    {
        _Saturation("Saturation", Range(0,1)) = 0.9
        _Value    ("Value",      Range(0,1)) = 0.9

        _PulseHzGlobal("Global Pulse Hz", Float) = 1.2

        _PhaseUD("Phase Up-Down",    Range(0,1)) = 0.00
        _PhaseLR("Phase Left-Right", Range(0,1)) = 0.33
        _PhaseFB("Phase Front-Back", Range(0,1)) = 0.66

        [PerRendererData]_FaceIndex   ("FaceIndex",   Float)  = 0
        [PerRendererData]_RubikCenter ("RubikCenter", Vector) = (0,0,0,1)
        [PerRendererData]_UVRotDeg    ("UVRotDeg",    Float)  = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back
        ZWrite On

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionHCS:SV_POSITION; float3 wpos:TEXCOORD0; float3 wnorm:TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float _Saturation, _Value;
                float _PulseHzGlobal;
                float _PhaseUD, _PhaseLR, _PhaseFB;
            CBUFFER_END

            CBUFFER_START(UnityPerDraw)
                float _FaceIndex;
                float4 _RubikCenter;
                float _UVRotDeg;
            CBUFFER_END

            V vert(A IN)
            {
                V o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionHCS = p.positionCS;
                o.wpos        = p.positionWS;
                o.wnorm       = TransformObjectToWorldNormal(IN.normalOS);
                return o;
            }

            float3 HSV2RGB(float3 c){
                float4 K=float4(1.0,2.0/3.0,1.0/3.0,3.0);
                float3 p=abs(frac(c.xxx+K.xyz)*6.0-K.www);
                return c.z*lerp(K.xxx, saturate(p-K.xxx), c.y);
            }

            int  PairId(float fi)       { return (fi < 1.5) ? 0 : (fi < 3.5) ? 1 : 2; }
            bool IsOpposite(float fi)   { return fmod(fi, 2.0) > 0.5; }

            float PairPhase(int pid){
                return (pid==0) ? _PhaseUD : (pid==1) ? _PhaseLR : _PhaseFB;
            }

            half4 frag(V i):SV_Target
            {
                int   pid    = PairId(_FaceIndex);
                float phase0 = PairPhase(pid);

                float baseHue = frac(_PulseHzGlobal * _Time.y + phase0);

                float hue = frac(baseHue + (IsOpposite(_FaceIndex) ? 0.5 : 0.0));

                float3 rgb = HSV2RGB(float3(hue, saturate(_Saturation), saturate(_Value)));
                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
