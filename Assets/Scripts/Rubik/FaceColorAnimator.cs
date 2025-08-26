// Assets/Scripts/Rubik/FaceColorAnimator.cs
using System.Collections.Generic;
using UnityEngine;

namespace Rubik
{
    public class FaceColorAnimator : MonoBehaviour
    {
        [Header("Base (per-face)")]
        [Range(0f,1f)] public float saturation = 0.9f;
        [Range(0f,1f)] public float value      = 0.9f;
        public float hueSpeed = 0.3f;

        [Header("Make it wilder")]
        [Range(0f,0.5f)] public float hueAmplitude = 0.18f;
        [Range(0f,5f)]   public float huePulseHz   = 1.2f;
        [Range(0f,1f)]   public float satAmplitude = 0.35f;
        [Range(0f,1f)]   public float valAmplitude = 0.25f;

        [Header("Spatial variation")]
        public float coordPhase     = 0.12f;
        [Range(0f,1f)] public float noiseStrength = 0.25f;
        public float noiseScale     = 0.8f;
        public float noiseTime      = 0.7f;

        [Header("Emission")]
        public float emissionIntensity = 1.6f;   // マテリアル側で Emission をONに

        [Header("Hue offsets per face (0..1)")]
        public float hueUp=0.00f, hueDown=0.50f, hueLeft=0.16f, hueRight=0.33f, hueFront=0.66f, hueBack=0.83f;

        [Header("Auto refresh (safety)")]
        public bool autoRefresh = true;
        public float refreshInterval = 0.5f;

        struct StickerInfo { public MeshRenderer mr; public Vector3Int coord; public StickerTag tag; }

        readonly Dictionary<Face, List<StickerInfo>> byFace = new();
        MaterialPropertyBlock mpb;
        float t0;
        bool ready = false;
        float lastRefreshTime = -999f;
        int cachedCount = 0;

        void OnEnable()
        {
            mpb = new MaterialPropertyBlock();
            t0 = Time.time;
            ForceRefresh();
        }

        void LateUpdate()
        {
            // 足りていなければ再スキャンを続ける（生成順に依存しない）
            if (!ready || (autoRefresh && Time.time - lastRefreshTime > refreshInterval))
            {
                if (RefreshIfNeeded()) lastRefreshTime = Time.time;
            }
            if (!ready) return;

            float t = Time.time - t0;
            AnimateFace(Face.Up,    hueUp,   t);
            AnimateFace(Face.Down,  hueDown, t);
            AnimateFace(Face.Left,  hueLeft, t);
            AnimateFace(Face.Right, hueRight,t);
            AnimateFace(Face.Front, hueFront,t);
            AnimateFace(Face.Back,  hueBack, t);
        }

        // ===== 集め直し =====
        public void ForceRefresh()
        {
            byFace.Clear();
            foreach (Face f in System.Enum.GetValues(typeof(Face))) byFace[f] = new List<StickerInfo>();
            cachedCount = 0;
            ready = false;
            RefreshIfNeeded();
        }

        bool RefreshIfNeeded()
        {
            // 子孫の StickerTag をすべて拾い直す
            var stickers = GetComponentsInChildren<StickerTag>(includeInactive: true);
            int total = stickers.Length;

            // 変化がなければ何もしない
            if (total == cachedCount && ready) return false;

            byFace.Clear();
            foreach (Face f in System.Enum.GetValues(typeof(Face))) byFace[f] = new List<StickerInfo>();

            foreach (var st in stickers)
            {
                var mr = st.GetComponent<MeshRenderer>();
                if (!mr) continue;
                var c = st.cubelet ? st.cubelet.coord : Vector3Int.zero;
                byFace[st.face].Add(new StickerInfo { mr = mr, coord = c, tag = st });
            }

            cachedCount = 0;
            int minPerFace = int.MaxValue, maxPerFace = 0;
            foreach (var kv in byFace)
            {
                int n = kv.Value.Count;
                cachedCount += n;
                minPerFace = Mathf.Min(minPerFace, n);
                maxPerFace = Mathf.Max(maxPerFace, n);
            }

            // 3x3x3 の想定（54枚）に達していて、かつ各面の枚数が揃っていれば ready
            ready = (cachedCount == 54 && minPerFace == maxPerFace && minPerFace == 9);

            return true; // いずれにせよ再構築は行った
        }

        // ===== 色適用 =====
        void AnimateFace(Face face, float baseHue, float t)
        {
            var list = byFace[face];
            foreach (var s in list)
            {
                // 連続な面座標(u,v)で滑らかに
                Vector3 p = s.mr.transform.position - transform.position;
                Vector3 r = s.mr.transform.right;
                Vector3 u = s.mr.transform.up;
                float uCoord = Vector3.Dot(p, r);
                float vCoord = Vector3.Dot(p, u);

                float coordOffset = coordPhase * (0.5f * uCoord + 0.5f * vCoord);

                float n = 0f;
                if (noiseStrength > 0f)
                {
                    float nx = uCoord * noiseScale + Time.time * noiseTime;
                    float ny = vCoord * noiseScale + Time.time * noiseTime * 0.87f + 100f;
                    n = (Mathf.PerlinNoise(nx, ny) - 0.5f) * 2f;
                }

                float hue = Mathf.Repeat(
                    baseHue
                  + hueSpeed * t
                  + hueAmplitude * Mathf.Sin(2f * Mathf.PI * (huePulseHz * t + coordOffset))
                  + noiseStrength * 0.15f * n,
                  1f
                );

                float S = Mathf.Clamp01(saturation + satAmplitude * Mathf.Sin(2f * Mathf.PI * (huePulseHz * 0.5f * t + coordOffset + 0.17f)));
                float V = Mathf.Clamp01(value      + valAmplitude * Mathf.Sin(2f * Mathf.PI * (huePulseHz * 0.37f * t + coordOffset + 0.41f)));

                Color c = Color.HSVToRGB(hue, S, V, true);

                mpb.Clear();
                mpb.SetColor("_Color", c);
                mpb.SetColor("_BaseColor", c);

                if (emissionIntensity > 0f)
                {
                    Color e = c * emissionIntensity;
                    mpb.SetColor("_EmissionColor", e);  // Standard/URP
                    mpb.SetColor("_EmissiveColor", e);  // HDRP
                }

                s.mr.SetPropertyBlock(mpb);
            }
        }
    }
}
