using System.Collections.Generic;
using UnityEngine;

namespace Rubik
{
    public class FaceColorAnimator : MonoBehaviour
    {
        [Range(0f, 1f)] public float saturation = 0.9f;
        [Range(0f, 1f)] public float value = 0.9f;
        public float hueSpeed = 0.03f; // change speed of hue rotation

        // initial hue offsets for each face
        public float hueUp = 0.00f;
        public float hueDown = 0.50f;
        public float hueLeft = 0.16f;
        public float hueRight = 0.33f;
        public float hueFront = 0.66f;
        public float hueBack = 0.83f;

        Dictionary<Face, List<MeshRenderer>> byFace = new();
        MaterialPropertyBlock mpb;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            foreach (Face f in System.Enum.GetValues(typeof(Face)))
                byFace[f] = new List<MeshRenderer>();

            var stickers = GetComponentsInChildren<StickerTag>(includeInactive: true);
            foreach (var st in stickers)
            {
                var mr = st.GetComponent<MeshRenderer>();
                if (mr) byFace[st.face].Add(mr);
            }
        }

        void LateUpdate()
        {
            ApplyFace(Face.Up,    hueUp);
            ApplyFace(Face.Down,  hueDown);
            ApplyFace(Face.Left,  hueLeft);
            ApplyFace(Face.Right, hueRight);
            ApplyFace(Face.Front, hueFront);
            ApplyFace(Face.Back,  hueBack);
        }

        void ApplyFace(Face face, float baseHue)
        {
            float h = Mathf.Repeat(baseHue + Time.time * hueSpeed, 1f);
            Color c = Color.HSVToRGB(h, saturation, value);

            foreach (var mr in byFace[face])
            {
                mpb.Clear();
                // Standard: _Color / URP Lit: _BaseColor
                mpb.SetColor("_Color", c);
                mpb.SetColor("_BaseColor", c);
                mr.SetPropertyBlock(mpb);
            }
        }
    }
}