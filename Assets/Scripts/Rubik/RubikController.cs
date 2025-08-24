// Assets/Scripts/Rubik/RubikController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rubik
{
    public class RubikController : MonoBehaviour
    {
        [Header("Refs")]
        public CubeBuilder builder;
        public UI.TimerUI timerUI;

        [Header("Face Drag (Left Mouse)")]
        [Tooltip("スワイプ開始のデッドゾーン(px)")]
        public float activatePixels = 8f;
        [Tooltip("回転角の感度(度/px) 例: 0.7なら 100px で 70°")]
        public float degreesPerPixel = 0.7f;
        [Tooltip("スナップアニメの基準時間(90°ぶん)")]
        public float turnDuration = 0.12f;
        public AnimationCurve turnCurve = AnimationCurve.EaseInOut(0,0,1,1);

        [Header("Orbit (Right Mouse)")]
        public float orbitSensitivity = 0.25f; // 度/px

        Camera cam;
        bool timerStarted = false;

        // 入力モード
        enum Mode { None, FaceDrag, Orbit }
        Mode mode = Mode.None;

        // --- FaceDrag 状態 ---
        Vector2 leftDownPx;           // 左押下位置(px)
        bool axisLocked = false;
        bool isFinalizing = false;

        // 選択された目標
        Cubelet pickedCubelet;
        Vector3 faceNormalWS;

        // 軸ロック後に確定
        Vector3 uWS, vWS;            // 面接線(画面右/上基準)
        Vector3 uLS, vLS;            // cubeRootローカルに変換
        Vector3 axisLS;              // 回転軸（ローカル）
        bool horizontal = true;      // 水平(左右)でロックしたか

        List<Cubelet> layer;         // 回す層
        Transform pivot;             // 一時親
        float currentAngle = 0f;     // 進行中の角度(-90..90)

        // --- Orbit 状態 ---
        Vector2 lastMousePx;

        void Awake()
        {
            cam = Camera.main;
            if (!builder) builder = GetComponent<CubeBuilder>();
        }

        void Update()
        {
            if (Mouse.current == null || isFinalizing) return;

            // 右押下→常にOrbit
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                mode = Mode.Orbit;
                lastMousePx = Mouse.current.position.ReadValue();
            }
            // 左押下→FaceDrag（ヒット時のみ）
            else if (Mouse.current.leftButton.wasPressedThisFrame && mode == Mode.None)
            {
                if (TryPickTarget(out pickedCubelet, out faceNormalWS))
                {
                    leftDownPx = Mouse.current.position.ReadValue();
                    axisLocked = false;
                    currentAngle = 0f;
                    mode = Mode.FaceDrag;
                }
                else
                {
                    mode = Mode.None;
                }
            }

            // --- Orbit 処理 ---
            if (mode == Mode.Orbit && Mouse.current.rightButton.isPressed)
            {
                Vector2 cur = Mouse.current.position.ReadValue();
                Vector2 d = cur - lastMousePx;
                lastMousePx = cur;

                if (d.sqrMagnitude > 0f)
                {
                    float yaw   =  d.x * orbitSensitivity;
                    float pitch = -d.y * orbitSensitivity;
                    builder.cubeRoot.Rotate(cam.transform.up,    yaw,   Space.World);
                    builder.cubeRoot.Rotate(cam.transform.right, pitch, Space.World);
                }
                return;
            }

            // --- FaceDrag 処理 ---
            if (mode == Mode.FaceDrag && Mouse.current.leftButton.isPressed)
            {
                Vector2 cur = Mouse.current.position.ReadValue();
                Vector2 deltaPx = cur - leftDownPx;

                // 軸がまだならロック判定
                if (!axisLocked)
                {
                    if (deltaPx.sqrMagnitude >= activatePixels * activatePixels)
                    {
                        // 面上の接線（画面基準）を作成
                        uWS = Vector3.ProjectOnPlane(cam.transform.right, faceNormalWS).normalized;
                        if (uWS.sqrMagnitude < 1e-6f) uWS = Vector3.ProjectOnPlane(Vector3.right, faceNormalWS).normalized;
                        vWS = Vector3.Cross(faceNormalWS, uWS).normalized; // 右手系 u→v→n

                        // ローカル変換 & 主軸へ量子化
                        uLS = QuantizeAxis(builder.cubeRoot.InverseTransformDirection(uWS));
                        vLS = QuantizeAxis(builder.cubeRoot.InverseTransformDirection(vWS));

                        // 水平/垂直どちらでロックするか
                        horizontal = Mathf.Abs(deltaPx.x) >= Mathf.Abs(deltaPx.y);

                        // 軸決定（水平→v軸、垂直→u軸）
                        axisLS = horizontal ? vLS : uLS;

                        // 層収集
                        layer = CollectLayerByAxisLocal(axisLS, pickedCubelet.coord);

                        // pivot 準備
                        pivot = new GameObject("TurnPivot").transform;
                        pivot.SetParent(builder.cubeRoot, false);
                        pivot.localPosition = Vector3.zero;
                        pivot.localRotation = Quaternion.identity;
                        foreach (var c in layer) c.transform.SetParent(pivot, true);

                        axisLocked = true;

                        // タイマー開始（初回のみ）
                        if (!timerStarted && timerUI != null)
                        {
                            timerUI.StartTimer();
                            timerStarted = true;
                        }
                    }
                }

                // 軸ロック後：連続回転（-90..+90 クランプ）
                if (axisLocked && pivot != null)
                {
                    float primary = horizontal ? deltaPx.x : deltaPx.y;
                    float target = Mathf.Clamp(primary * degreesPerPixel, -90f, 90f);
                    if (!Mathf.Approximately(target, currentAngle))
                    {
                        currentAngle = target;
                        pivot.localRotation = Quaternion.AngleAxis(currentAngle, axisLS);
                    }
                }
            }

            // --- マウスアップ処理 ---
            if (mode == Mode.FaceDrag && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (axisLocked && pivot != null)
                {
                    float snapTarget =
                        (currentAngle >= 45f)  ?  90f :
                        (currentAngle <= -45f) ? -90f :
                        0f;

                    StartCoroutine(SnapAndFinalize(snapTarget));
                }
                else
                {
                    // 軸ロック前に離した → 何もしない
                    mode = Mode.None;
                }
            }

            if (mode == Mode.Orbit && Mouse.current.rightButton.wasReleasedThisFrame)
            {
                mode = Mode.None;
            }
        }

        // ========= ここから補助ロジック =========

        // カーソル直下で最前面の「小キューブ」を取得（ステッカーでも本体でもOK）
        bool TryPickTarget(out Cubelet cubelet, out Vector3 faceNWS)
        {
            cubelet = null;
            faceNWS = default;

            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                // ステッカー優先
                var st = hit.collider.GetComponent<StickerTag>();
                if (st != null)
                {
                    cubelet = st.cubelet;
                    faceNWS = (st.transform.rotation * Vector3.forward).normalized; // Quadの前向き
                    return true;
                }

                // 次にキューブ本体
                var cu = hit.collider.GetComponentInParent<Cubelet>();
                if (cu != null)
                {
                    cubelet = cu;
                    faceNWS = hit.normal.normalized; // 当たった面の法線
                    return true;
                }
            }
            return false;
        }

        // ±X/±Y/±Z へ量子化
        Vector3 QuantizeAxis(Vector3 v)
        {
            v.Normalize();
            Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3 best = Vector3.right; float bd = -1f;
            foreach (var a in axes)
            {
                float d = Vector3.Dot(v, a);
                if (d > bd) { bd = d; best = a; }
            }
            return best;
        }

        // 指定軸（ローカル）で、クリックした小キューブと同じ層を収集
        List<Cubelet> CollectLayerByAxisLocal(Vector3 axisLocal, Vector3Int picked)
        {
            int which = Mathf.Abs(axisLocal.x) > 0.5f ? 0 : Mathf.Abs(axisLocal.y) > 0.5f ? 1 : 2;
            int target = (which == 0) ? picked.x : (which == 1) ? picked.y : picked.z;

            var list = new List<Cubelet>();
            foreach (var c in builder.cubelets)
            {
                int v = (which == 0) ? c.coord.x : (which == 1) ? c.coord.y : c.coord.z;
                if (v == target) list.Add(c);
            }
            return list;
        }

        IEnumerator SnapAndFinalize(float snapTarget)
        {
            isFinalizing = true;

            // スナップ補間（残角分だけ時間をスケール）
            float remain = Mathf.Abs(snapTarget - currentAngle);
            float dur = Mathf.Max(0.05f, turnDuration * (remain / 90f));

            float t = 0f;
            float start = currentAngle;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float a = Mathf.Lerp(start, snapTarget, turnCurve.Evaluate(t));
                pivot.localRotation = Quaternion.AngleAxis(a, axisLS);
                yield return null;
            }
            currentAngle = snapTarget;
            pivot.localRotation = Quaternion.AngleAxis(currentAngle, axisLS);

            // 0°に戻すなら論理座標は不変。±90°なら論理座標を更新
            bool commit = Mathf.Abs(currentAngle) > 1e-2f; // ≈ ±90°
            foreach (var c in layer)
            {
                if (commit)
                {
                    Vector3 v = (Vector3)c.coord;
                    Vector3 v2 = Quaternion.AngleAxis(currentAngle, axisLS) * v;
                    c.coord = new Vector3Int(RoundInt(v2.x), RoundInt(v2.y), RoundInt(v2.z));
                }

                // 位置・回転は cubeRoot ローカルでスナップ
                Vector3 local = builder.cubeRoot.InverseTransformPoint(c.transform.position);
                local = new Vector3(RoundGrid(local.x), RoundGrid(local.y), RoundGrid(local.z));

                c.transform.SetParent(builder.cubeRoot, false);
                c.transform.localPosition = local;

                Vector3 le = c.transform.localEulerAngles;
                c.transform.localEulerAngles = new Vector3(Round90(le.x), Round90(le.y), Round90(le.z));
            }

            Destroy(pivot.gameObject);
            pivot = null;
            layer = null;
            pickedCubelet = null;
            axisLocked = false;
            currentAngle = 0f;
            mode = Mode.None;
            isFinalizing = false;
        }

        // 丸め
        static int   RoundInt(float x)  => Mathf.RoundToInt(x + Mathf.Sign(x) * 1e-3f);
        static float RoundGrid(float x) => Mathf.Round(x + Mathf.Sign(x) * 1e-3f);
        static float Round90(float deg)
        {
            float r = Mathf.Round(deg / 90f) * 90f;
            r %= 360f; if (r < 0f) r += 360f;
            return r;
        }
    }
}
