// Assets/Scripts/UI/PBProbe.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PBProbe : MonoBehaviour
{
    static readonly int FaceIndexID   = Shader.PropertyToID("_FaceIndex");
    static readonly int AnimSpeedID   = Shader.PropertyToID("_AnimSpeed");
    static readonly int GradScaleID   = Shader.PropertyToID("_GradScale");
    static readonly int PatternID     = Shader.PropertyToID("_Pattern");
    static readonly int RubikCenterID = Shader.PropertyToID("_RubikCenter");

    MaterialPropertyBlock pb;

    void Awake() { pb = new MaterialPropertyBlock(); }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f9Key.wasPressedThisFrame)
        {
            var cam = Camera.main;
            if (cam == null || Mouse.current == null) return;

            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 100f)) return;

            var mr = hit.collider.GetComponent<MeshRenderer>() ??
                     hit.collider.GetComponentInParent<MeshRenderer>();
            if (mr == null)
            {
                Debug.Log("[PBProbe] No MeshRenderer under cursor.");
                return;
            }

            pb.Clear();
            mr.GetPropertyBlock(pb); // 現在のMPBを読み戻す

            // 読み出し（未設定なら 0 が返る）
            float idx   = pb.GetFloat(FaceIndexID);
            float speed = pb.GetFloat(AnimSpeedID);
            float grad  = pb.GetFloat(GradScaleID);
            float patt  = pb.GetFloat(PatternID);
            Vector4 ctr = pb.GetVector(RubikCenterID);

            // Binderは animSpeed>0 / grad!=0 / ctr.w=1 などを入れている想定
            bool inferredHasPB =
                (speed != 0f) || (grad != 0f) || (patt != 0f) || (idx != 0f) || (ctr != Vector4.zero);

            Debug.Log($"[PBProbe] {mr.name} MPB?={inferredHasPB} " +
                      $"_FaceIndex={idx}, _AnimSpeed={speed}, _GradScale={grad}, _Pattern={patt}, _RubikCenter={ctr}");
        }
    }
}
