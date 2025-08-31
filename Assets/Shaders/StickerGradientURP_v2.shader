Shader "Rubik/StickerGradientURP_v2"
{
    Properties
    {
        // 共通の基本設定
        _Saturation      ("Saturation", Range(0,1)) = 0.9
        _Value           ("Value",      Range(0,1)) = 0.9
        _HueAmplitude    ("Hue Pulse Amplitude", Range(0,0.5)) = 0.18

        // 面ごとの基調色（0..1）
        _HueUp    ("Hue Up",    Range(0,1)) = 0.00
        _HueDown  ("Hue Down",  Range(0,1)) = 0.50
        _HueLeft  ("Hue Left",  Range(0,1)) = 0.16
        _HueRight ("Hue Right", Range(0,1)) = 0.33
        _HueFront ("Hue Front", Range(0,1)) = 0.66
        _HueBack  ("Hue Back",  Range(0,1)) = 0.83

        [PerRendererData]_FaceIndex   ("FaceIndex", Float)  = 0
        [PerRendererData]_RubikCenter ("RubikCenter", Vector) = (0,0,0,1)
        [PerRendererData]_AnimSpeed   ("AnimSpeed", Float)  = 0.4
        [PerRendererData]_PulseHz     ("PulseHz", Float)    = 1.2
        [PerRendererData]_GradScale   ("GradScale", Float)  = 0.35
        [PerRendererData]_UVRotDeg    ("UVRotDeg", Float)   = 0
        [PerRendererData]_Seed        ("Seed", Float)       = 0
        [PerRendererData]_Pattern     ("Pattern", Float)    = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
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

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 worldPos:TEXCOORD0; float3 worldNormal:TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float _Saturation, _Value, _HueAmplitude;
                float _HueUp,_HueDown,_HueLeft,_HueRight,_HueFront,_HueBack;
            CBUFFER_END

            CBUFFER_START(UnityPerDraw)
                float _FaceIndex;     // 0..5
                float4 _RubikCenter;  // このレンダラが属するキューブ中心
                float _AnimSpeed;     // 秒あたり色相周回（rev/s）
                float _PulseHz;       // パルス周波数
                float _GradScale;     // 面内勾配の強さ
                float _UVRotDeg;      // 面内UV回転（度）
                float _Seed;          // 位相用シード
                float _Pattern;       // 0:U直線 1:V直線 2:放射状 3:ストライプ 4:チェッカー
            CBUFFER_END

            Varyings vert (Attributes IN) {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.worldPos    = p.positionWS;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float3 HSV2RGB(float3 c) {
                float4 K=float4(1.0,2.0/3.0,1.0/3.0,3.0);
                float3 p=abs(frac(c.xxx+K.xyz)*6.0-K.www);
                return c.z*lerp(K.xxx, saturate(p-K.xxx), c.y);
            }
            float GetHueBase(float i){
                return (i<0.5)?_HueUp:(i<1.5)?_HueDown:(i<2.5)?_HueLeft:(i<3.5)?_HueRight:(i<4.5)?_HueFront:_HueBack;
            }

            half4 frag (Varyings IN):SV_Target
            {
                float3 n = normalize(IN.worldNormal);
                float3 any = (abs(n.y) < 0.95) ? float3(0,1,0) : float3(1,0,0);
                float3 u = normalize(cross(any, n));
                float3 v = normalize(cross(n, u));

                float3 p = IN.worldPos - _RubikCenter;
                float2 uv = float2(dot(p,u), dot(p,v));

                // UV回転
                float rad = radians(_UVRotDeg);
                float cr = cos(rad), sr = sin(rad);
                float2 uvR = float2( cr*uv.x - sr*uv.y, sr*uv.x + cr*uv.y );

                // パターン
                float g=0;
                if (_Pattern < 0.5)      g = uvR.x;                    // 0: U直線
                else if (_Pattern < 1.5) g = uvR.y;                    // 1: V直線
                else if (_Pattern < 2.5) g = length(uvR);              // 2: 放射
                else if (_Pattern < 3.5) g = sin(6.28318*(uvR.x));     // 3: ストライプ
                else                     g = sin(6.28318*uvR.x)*sin(6.28318*uvR.y); // 4: チェッカー

                // 係数を混ぜて色相へ
                float t = _Time.y;
                float baseHue = GetHueBase(_FaceIndex);
                float phase   = _Seed; // 面ごとにバラす

                float hue = frac( baseHue
                                  + _AnimSpeed * t
                                  + _HueAmplitude * sin(6.28318*(_PulseHz*t + phase))
                                  + _GradScale * g );

                float3 rgb = HSV2RGB(float3(hue, saturate(_Saturation), saturate(_Value)));

                return half4(rgb,1);
            }
            ENDHLSL
        }
    }
}
