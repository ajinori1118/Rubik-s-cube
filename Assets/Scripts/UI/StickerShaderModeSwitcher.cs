using UnityEngine;
using UnityEngine.InputSystem;

namespace Rubik {
    public class StickerShaderModeSwitcher : MonoBehaviour
    {
        [Header("Materials (assign all sticker modes)")]
        public Material Mode1;
        public Material Mode2;
        public Material Mode3;
        public Material Mode4;

        [Header("Global Speed (Hz)")]
        public float lowHz  = 0.35f;
        public float highHz = 2.0f;

        public StickerFaceBinder binder;

        static readonly int PulseHzGlobalID = Shader.PropertyToID("_PulseHzGlobal");

        void Awake(){ if (!binder) binder = GetComponentInParent<StickerFaceBinder>(); }

        public void SetMode(int mode)
        {
            var mat = (mode==0)? Mode1
                    : (mode==1)? Mode2
                    : (mode==2)? Mode3
                    :            Mode4;

            var stickers = GetComponentsInChildren<StickerTag>(true);
            foreach (var st in stickers)
            {
                var mr = st.GetComponent<MeshRenderer>();
                if (!mr) continue;
                mr.sharedMaterial = mat;
            }
            if (binder) binder.ApplyToAllStickers();
        }

        public enum SpeedMode { Low, High }
        public void SetSpeed(SpeedMode m)
        {
            float hz = (m == SpeedMode.Low) ? lowHz : highHz;

            SetHzOn(Mode1, hz);
            SetHzOn(Mode2, hz);
            SetHzOn(Mode3, hz);
            SetHzOn(Mode4, hz);
        }

        void SetHzOn(Material mat, float hz)
        {
            if (!mat) return;
            mat.SetFloat(PulseHzGlobalID, hz);
        }

        void Update()
        {
            var kb = Keyboard.current; if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SetMode(0);
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SetMode(1);
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SetMode(2);

            if (kb.zKey.wasPressedThisFrame) SetSpeed(SpeedMode.Low);
            if (kb.xKey.wasPressedThisFrame) SetSpeed(SpeedMode.High);
        }
    }
}