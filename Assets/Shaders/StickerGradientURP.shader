Shader "Rubik/StickerGradientURP"
{
    Properties
    {
        _Saturation      ("Saturation", Range(0,1)) = 0.9
        _Value           ("Value",      Range(0,1)) = 0.9
        _HueSpeed        ("Hue Speed (rev/s)", Float) = 0.3
        _HueAmplitude    ("Hue Pulse Amplitude", Range(0,0.5)) = 0.18
        _HuePulseHz      ("Hue Pulse Hz", Float) = 1.2
        _CoordPhase      ("Phase by Coord", Float) = 0.12
        _GradientScale   ("Face Gradient Strength", Float) = 0.35
        _EmissionIntensity ("Emission Intensity", Float) = 1.6

        _HueUp    ("Hue Up",    Range(0,1)) = 0.00
        _HueDown  ("Hue Down",  Range(0,1)) = 0.50
        _HueLeft  ("Hue Left",  Range(0,1)) = 0.16
        _HueRight ("Hue Right", Range(0,1)) = 0.33
        _HueFront ("Hue Front", Range(0,1)) = 0.66
        _HueBack  ("Hue Back",  Range(0,1)) = 0.83
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Saturation, _Value;
                float _HueSpeed, _HueAmplitude, _HuePulseHz, _CoordPhase, _GradientScale, _EmissionIntensity;
                float _HueUp, _HueDown, _HueLeft, _HueRight, _HueFront, _HueBack;
            CBUFFER_END

            // per-renderer (MPB)
            float _FaceIndex;            // 0:Up 1:Down 2:Left 3:Right 4:Front 5:Back

            // global (共通)
            float3 _RubikCenter;         // cubeRoot のワールド位置

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.worldPos    = pos.positionWS;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float3 HSV2RGB(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float GetHueBase(float idx)
            {
                return (idx < 0.5) ? _HueUp :
                       (idx < 1.5) ? _HueDown :
                       (idx < 2.5) ? _HueLeft :
                       (idx < 3.5) ? _HueRight :
                       (idx < 4.5) ? _HueFront : _HueBack;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // ≪面ごとに連続な座標系(u,v)を構築≫
                float3 n = normalize(IN.worldNormal);

                // n に直交する安定な接線基底(u,v)
                float3 any = (abs(n.y) < 0.95) ? float3(0,1,0) : float3(1,0,0);
                float3 u = normalize(cross(any, n));
                float3 v = normalize(cross(n, u));

                float3 p = IN.worldPos - _RubikCenter;
                float uCoord = dot(p, u);
                float vCoord = dot(p, v);

                // 色相（時間 + 面内勾配 + 位相）
                float baseHue = GetHueBase(_FaceIndex);
                float t = _Time.y;

                float coordOffset = _CoordPhase * (0.5 * uCoord + 0.5 * vCoord);

                float hue = frac(
                    baseHue
                    + _HueSpeed * t
                    + _HueAmplitude * sin(2.0 * PI * (_HuePulseHz * t + coordOffset))
                    + _GradientScale * (0.5 * uCoord + 0.5 * vCoord)   // ★面内で滑らかに変化
                );

                float s = saturate(_Saturation);
                float vval = saturate(_Value);

                float3 rgb = HSV2RGB(float3(hue, s, vval));

                // Unlit + Emission風
                if (_EmissionIntensity > 0.0)
                    rgb *= _EmissionIntensity;

                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
