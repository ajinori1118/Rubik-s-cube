// Assets/Scripts/Rubik/StickerFaceBinder.cs
using UnityEngine;

namespace Rubik
{
    [ExecuteAlways]
    public class StickerFaceBinder : MonoBehaviour
    {
        // Shader property IDs
        static readonly int FaceIndexID   = Shader.PropertyToID("_FaceIndex");
        static readonly int RubikCenterID = Shader.PropertyToID("_RubikCenter");
        static readonly int AnimSpeedID   = Shader.PropertyToID("_AnimSpeed");
        static readonly int PulseHzID     = Shader.PropertyToID("_PulseHz");
        static readonly int GradScaleID   = Shader.PropertyToID("_GradScale");
        static readonly int UVRotDegID    = Shader.PropertyToID("_UVRotDeg");
        static readonly int SeedID        = Shader.PropertyToID("_Seed");
        static readonly int PatternID     = Shader.PropertyToID("_Pattern");

        [System.Serializable]
        public class FaceParams
        {
            [Range(0,4)] public int pattern = 0;   // 0:U 1:V 2:Radial 3:Stripe 4:Checker
            public float uvRotDeg = 0f;
            public float gradScale = 0.35f;
            public float animSpeed = 0.30f;        // rev/sec
            public float pulseHz   = 1.2f;
            [Range(0,1)] public float seed = 0.0f;
        }

        [Header("Auto resolve")]
        [Tooltip("空でもOK。見つからない場合は自動で中心を推定します")]
        public Transform cubeRoot;

        [Header("Per-Face Parameters")]
        public FaceParams up    = new(){ pattern=2, uvRotDeg=0,   gradScale=0.35f, animSpeed=0.3f, pulseHz=1.2f, seed=0.00f };
        public FaceParams down  = new(){ pattern=2, uvRotDeg=45,  gradScale=0.30f, animSpeed=0.3f, pulseHz=1.0f, seed=0.25f };
        public FaceParams left  = new(){ pattern=3, uvRotDeg=0,   gradScale=0.40f, animSpeed=0.4f, pulseHz=1.5f, seed=0.10f };
        public FaceParams right = new(){ pattern=3, uvRotDeg=90,  gradScale=0.40f, animSpeed=0.4f, pulseHz=1.5f, seed=0.35f };
        public FaceParams front = new(){ pattern=0, uvRotDeg=0,   gradScale=0.30f, animSpeed=0.5f, pulseHz=0.8f, seed=0.50f };
        public FaceParams back  = new(){ pattern=1, uvRotDeg=0,   gradScale=0.30f, animSpeed=0.5f, pulseHz=0.8f, seed=0.75f };

        [Header("Auto Rebind / Debug")]
        public bool autoRebindEveryFrame = true;
        public bool drawCenterGizmo = false;

        MaterialPropertyBlock _mpb;
        int _lastStickerCount = -1;

        void OnEnable()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            ResolveCubeRoot();
            ApplyToAllStickers();
        }

        void OnValidate()
        {
            if (!Application.isPlaying) ResolveCubeRoot();
            ApplyToAllStickers();
        }

        void LateUpdate()
        {
            ResolveCubeRoot();

            var center4 = ComputeCenterV4();
            var stickers = GetComponentsInChildren<StickerTag>(true);
            foreach (var st in stickers)
            {
                var mr = st.GetComponent<MeshRenderer>();
                if (!mr) continue;
                _mpb.Clear();
                mr.GetPropertyBlock(_mpb);
                _mpb.SetVector(RubikCenterID, center4);
                mr.SetPropertyBlock(_mpb);
            }

            if (autoRebindEveryFrame) ApplyToAllStickers();
        }
        void ResolveCubeRoot()
        {
            if (cubeRoot) return;

            var b = GetComponent<CubeBuilder>() ?? GetComponentInParent<CubeBuilder>();
            if (b != null)
            {
                var prop = b.GetType().GetProperty("CubeRoot");
                cubeRoot = (Transform)(prop?.GetValue(b) ?? b.GetType().GetField("cubeRoot")?.GetValue(b));
            }
            if (!cubeRoot)
            {
                var tr = transform.Find("CubeRoot"); // 名前で保険
                if (tr) cubeRoot = tr;
            }
        }

        Vector3 GetRubikCenter()
        {
            if (cubeRoot) return cubeRoot.position;

            // フォールバック：ステッカー群の平均位置
            var stickers = GetComponentsInChildren<StickerTag>(true);
            if (stickers != null && stickers.Length > 0)
            {
                Vector3 sum = Vector3.zero;
                int n = 0;
                foreach (var st in stickers) { sum += st.transform.position; n++; }
                if (n > 0) return sum / n;
            }
            return transform.position;
        }

        void ShaderCenterToAll(Vector3 centerWS)
        {
            var stickers = GetComponentsInChildren<StickerTag>(true);
            foreach (var st in stickers)
            {
                var mr = st.GetComponent<MeshRenderer>();
                if (!mr) continue;
                mr.GetPropertyBlock(_mpb);
                _mpb.SetVector(RubikCenterID, centerWS);
                mr.SetPropertyBlock(_mpb);
            }
        }

        [ContextMenu("Rebind All Face Params")]
        public void ApplyToAllStickers()
        {
            var stickers = GetComponentsInChildren<StickerTag>(true);
            Vector4 center4 = ComputeCenterV4();

            foreach (var st in stickers)
            {
                var mr = st.GetComponent<MeshRenderer>();
                if (!mr) continue;

                var p = GetParams(st.face);

                _mpb.Clear();
                mr.GetPropertyBlock(_mpb);
                _mpb.SetFloat(FaceIndexID,  (float)ToIndex(st.face));
                _mpb.SetVector(RubikCenterID, center4);
                _mpb.SetFloat(AnimSpeedID,   Mathf.Max(0.0001f, p.animSpeed));
                _mpb.SetFloat(PulseHzID,     p.pulseHz);
                _mpb.SetFloat(GradScaleID,   p.gradScale);
                _mpb.SetFloat(UVRotDegID,    p.uvRotDeg);
                _mpb.SetFloat(SeedID,        p.seed);
                _mpb.SetFloat(PatternID,     p.pattern);
                mr.SetPropertyBlock(_mpb);
            }
        }

        FaceParams GetParams(Face f)
        {
            switch (f)
            {
                case Face.Up: return up; case Face.Down: return down;
                case Face.Left: return left; case Face.Right: return right;
                case Face.Front: return front; case Face.Back: return back;
            }
            return up;
        }

        int ToIndex(Face f)
        {
            switch (f)
            {
                case Face.Up: return 0; case Face.Down: return 1;
                case Face.Left: return 2; case Face.Right: return 3;
                case Face.Front: return 4; case Face.Back: return 5;
            }
            return 0;
        }

        void OnDrawGizmosSelected()
        {
            if (!drawCenterGizmo) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetRubikCenter(), 0.15f);
        }

        Vector4 ComputeCenterV4()
        {
            if (cubeRoot)
            {
                var p = cubeRoot.position;
                return new Vector4(p.x, p.y, p.z, 1f);
            }
            // フォールバック: ステッカーの平均位置
            var stickers = GetComponentsInChildren<StickerTag>(true);
            if (stickers != null && stickers.Length > 0)
            {
                Vector3 sum = Vector3.zero;
                foreach (var st in stickers) sum += st.transform.position;
                var c = sum / stickers.Length;
                return new Vector4(c.x, c.y, c.z, 1f);
            }
            var t = transform.position;
            return new Vector4(t.x, t.y, t.z, 1f);
        }
    }
}
