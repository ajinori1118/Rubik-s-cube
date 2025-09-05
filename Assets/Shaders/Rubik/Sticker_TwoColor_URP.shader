Shader "Rubik/Sticker_TwoColorPairSwap_URP"
{
    Properties
    {
        _Saturation("Saturation", Range(0,1)) = 0.9
        _Value("Value", Range(0,1)) = 0.9

        _HueUD_A("Hue Up-Down A", Range(0,1)) = 0.000000
        _HueUD_B("Hue Up-Down B", Range(0,1)) = 0.333333
        _HueLR_A("Hue Left-Right A", Range(0,1)) = 0.166667
        _HueLR_B("Hue Left-Right B", Range(0,1)) = 0.833333
        _HueFB_A("Hue Front-Back A", Range(0,1)) = 0.666667
        _HueFB_B("Hue Front-Back B", Range(0,1)) = 0.083333

        [PerRendererData]_FaceIndex("FaceIndex", Float) = 0
        [PerRendererData]_RubikCenter("RubikCenter", Vector) = (0,0,0,1)
        [PerRendererData]_PulseHz("PulseHz", Float) = 1.2
        [PerRendererData]_Seed("Seed", Float) = 0.0
        [PerRendererData]_UVRotDeg("UVRotDeg", Float) = 0

        _PulseHzGlobal("Global Pulse Hz", Float) = 1.2

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
                float _HueUD_A, _HueUD_B, _HueLR_A, _HueLR_B, _HueFB_A, _HueFB_B;
                float _PulseHzGlobal;
            CBUFFER_END

            CBUFFER_START(UnityPerDraw)
                float _FaceIndex;
                float4 _RubikCenter;
                float _PulseHz, _Seed, _UVRotDeg;
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
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            int PairId(float fi)      { return (fi < 1.5) ? 0 : (fi < 3.5) ? 1 : 2; }
            bool IsSlaveFace(float fi){ return fmod(fi, 2.0) > 0.5; }

            void GetPairHues(int pid, out float hA, out float hB){
                if (pid==0){ hA=_HueUD_A; hB=_HueUD_B; }
                else if(pid==1){ hA=_HueLR_A; hB=_HueLR_B; }
                else { hA=_HueFB_A; hB=_HueFB_B; }
            }

            half4 frag(V i):SV_Target
            {
                int pid = PairId(_FaceIndex);
                float hA, hB; GetPairHues(pid, hA, hB);

                float w = 0.5 + 0.5 * sin(6.28318 * (_PulseHzGlobal * _Time.y + _Seed));

                if (IsSlaveFace(_FaceIndex)) w = 1.0 - w;

                float3 rgbA = HSV2RGB(float3(frac(hA), saturate(_Saturation), saturate(_Value)));
                float3 rgbB = HSV2RGB(float3(frac(hB), saturate(_Saturation), saturate(_Value)));
                float3 rgb  = lerp(rgbA, rgbB, w);

                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}