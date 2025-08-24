// Assets/Scripts/Rubik/FaceColorAnimator.cs
using System.Collections.Generic;
using UnityEngine;

namespace Rubik
{
    public class FaceColorAnimator : MonoBehaviour
    {
        [Header("Color")]
        [Range(0f, 1f)] public float saturation = 0.9f;
        [Range(0f, 1f)] public float value = 0.9f;
        [Tooltip("1.0で1秒あたり色相1周")]
        public float hueSpeed = 0.03f;

        [Header("Per-Face Hue Offsets (0..1)")]
        public float hueUp = 0.00f;
        public float hueDown = 0.50f;
        public float hueLeft = 0.16f;
        public float hueRight = 0.33f;
        public float hueFront = 0.66f;
        public float hueBack = 0.83f;

        readonly Dictionary<Face, List<MeshRenderer>> byFace = new();
        MaterialPropertyBlock mpb;
        bool warmedUp = false;
        float t0;

        void OnEnable()
        {
            mpb = new MaterialPropertyBlock();
            t0 = Time.time;
            WarmUpIfNeeded(); // まず試す
        }

        void LateUpdate()
        {
            // まだステッカーが見つからない（生成直後など）→ 再試行
            if (!warmedUp) WarmUpIfNeeded();
            if (!warmedUp) return;

            ApplyFace(Face.Up,    hueUp);
            ApplyFace(Face.Down,  hueDown);
            ApplyFace(Face.Left,  hueLeft);
            ApplyFace(Face.Right, hueRight);
            ApplyFace(Face.Front, hueFront);
            ApplyFace(Face.Back,  hueBack);
        }

        void WarmUpIfNeeded()
        {
            // 初期化
            byFace.Clear();
            foreach (Face f in System.Enum.GetValues(typeof(Face))) byFace[f] = new List<MeshRenderer>();

            // 子孫から StickerTag を集める（生成順に依存しない）
            var stickers = GetComponentsInChildren<StickerTag>(includeInactive: true);
            foreach (var st in stickers)
            {
                var mr = st.GetComponent<MeshRenderer>();
                if (mr != null) byFace[st.face].Add(mr);
            }

            // 1面でも見つかればOKとする
            warmedUp = false;
            foreach (var kv in byFace)
            {
                if (kv.Value.Count > 0) { warmedUp = true; break; }
            }
        }

        void ApplyFace(Face face, float baseHue)
        {
            float h = Mathf.Repeat(baseHue + (Time.time - t0) * hueSpeed, 1f);
            Color c = Color.HSVToRGB(h, saturation, value);

            // Standard: _Color / URP/HDRP Lit: _BaseColor 両方をセット
            foreach (var mr in byFace[face])
            {
                if (mr == null) continue;
                mpb.Clear();
                mpb.SetColor("_Color", c);
                mpb.SetColor("_BaseColor", c);
                mr.SetPropertyBlock(mpb);
            }
        }
    }
}
