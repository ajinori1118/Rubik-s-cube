Shader "Rubik/Sticker_DiagonalGrad_URP"
{
    Properties
    {
        _Saturation("Saturation", Range(0,1)) = 0.9
        _Value("Value", Range(0,1)) = 0.9
        _PulseHzGlobal("Global Pulse Hz", Float) = 0.8
        _PhaseUD("Phase Up-Down", Range(0,1)) = 0.00
        _PhaseLR("Phase Left-Right", Range(0,1)) = 0.33
        _PhaseFB("Phase Front-Back", Range(0,1)) = 0.66
        _FaceWorldSize("Face World Size", Float) = 1.0
        _UVRotDeg("Diagonal Rotation (deg)", Float) = 45.0
        _HueSpanDeg("Hue Span (Degrees)", Float) = 10.0
        _HueUD_A0("UD Hue A0", Range(0,1)) = 0.000000
        _HueLR_A0("LR Hue A0", Range(0,1)) = 0.666667
        _HueFB_A0("FB Hue A0", Range(0,1)) = 0.833333
        [PerRendererData]_FaceIndex("FaceIndex", Float) = 0
        [PerRendererData]_RubikCenter("RubikCenter", Vector) = (0,0,0,1)
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
                float _FaceWorldSize, _UVRotDeg;
                float _HueSpanDeg;
                float _HueUD_A0, _HueLR_A0, _HueFB_A0;
            CBUFFER_END

            CBUFFER_START(UnityPerDraw)
                float _FaceIndex;
                float4 _RubikCenter;
            CBUFFER_END

            V vert(A IN)
            {
                V o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionHCS = p.positionCS;
                o.wpos = p.positionWS;
                o.wnorm = TransformObjectToWorldNormal(IN.normalOS);
                return o;
            }

            float3 HSV2RGB(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            void BuildBasis(float3 n, out float3 u, out float3 v)
            {
                float3 any = (abs(n.y) < 0.95) ? float3(0,1,0) : float3(1,0,0);
                u = normalize(cross(any, n));
                v = normalize(cross(n, u));
            }

            float2 Rotate2(float2 p, float deg)
            {
                float r = radians(deg); float c = cos(r), s = sin(r);
                return float2(c*p.x - s*p.y, s*p.x + c*p.y);
            }

            int  PairId(float fi) { return (fi < 1.5) ? 0 : (fi < 3.5) ? 1 : 2; }
            bool IsOpp(float fi)  { return fmod(fi, 2.0) > 0.5; }

            float PairPhase(int pid) { return (pid==0)?_PhaseUD : (pid==1)?_PhaseLR : _PhaseFB; }
            float PairHueA0(int pid) { return (pid==0)?_HueUD_A0 : (pid==1)?_HueLR_A0 : _HueFB_A0; }

            half4 frag(V i):SV_Target
            {
                float3 n = normalize(i.wnorm);
                float3 u, v; BuildBasis(n, u, v);
                float3 pw = i.wpos - _RubikCenter.xyz;
                float2 uv = float2(dot(pw,u), dot(pw,v));
                float2 uvR = Rotate2(uv, _UVRotDeg);

                float s = max(_FaceWorldSize, 1e-4);
                float diag = (uvR.x + uvR.y) / s;
                float t = saturate(0.5 * diag + 0.5);
                t = smoothstep(0.0, 1.0, t);

                int pid = PairId(_FaceIndex);
                bool opp = IsOpp(_FaceIndex);

                float base = frac(_PulseHzGlobal * _Time.y + PairPhase(pid));
                float hueA0 = PairHueA0(pid);

                float span = _HueSpanDeg / 360.0;
                float baseHue = frac(base + hueA0 + (opp ? 0.5 : 0.0));
                float tt = opp ? (1.0 - t) : t;

                float hue = frac(baseHue + tt * span);

                float3 rgb = HSV2RGB(float3(hue, saturate(_Saturation), 1.0)) * saturate(_Value);
                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
