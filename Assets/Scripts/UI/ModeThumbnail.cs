using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Rubik
{
    public class ModeThumbnail : MonoBehaviour
    {
        [Header("UI Refs")]
        public RawImage previewImage;
        public TextMeshProUGUI titleText;
        public Button pickButton;

        ModeSelectUI owner;
        int modeIndex;

        Transform previewRoot;
        Camera previewCam;
        RenderTexture rt;

        public void Init(ModeSelectUI owner, int modeIndex, string title, Material mat, int size)
        {
            this.owner = owner;
            this.modeIndex = modeIndex;
            titleText.text = title;

            rt = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32);
            rt.Create();
            previewImage.texture = rt;

            var holder = new GameObject("PreviewHolder");
            holder.hideFlags = HideFlags.HideAndDontSave;
            previewRoot = holder.transform;
            previewRoot.position = new Vector3(10000 + modeIndex * 10, 10000, 10000);

            BuildPreviewCube(previewRoot, mat);

            var camObj = new GameObject("PreviewCam");
            camObj.hideFlags = HideFlags.HideAndDontSave;
            previewCam = camObj.AddComponent<Camera>();
            previewCam.clearFlags = CameraClearFlags.SolidColor;
            previewCam.backgroundColor = new Color(0, 0, 0, 0);
            previewCam.transform.position = previewRoot.position + new Vector3(0, 0.6f, -1.1f);
            previewCam.transform.LookAt(previewRoot.position, Vector3.up);
            previewCam.nearClipPlane = 0.01f;
            previewCam.farClipPlane = 5f;
            previewCam.targetTexture = rt;

            pickButton.onClick.AddListener(() => owner.Choose(this.modeIndex));
        }

        void OnDestroy()
        {
            if (previewCam) Destroy(previewCam.gameObject);
            if (previewRoot) Destroy(previewRoot.gameObject);
            if (rt) rt.Release();
        }

        void Update()
        {
            if (previewRoot) previewRoot.Rotate(Vector3.up, 20f * Time.unscaledDeltaTime, Space.World);
        }

        void BuildPreviewCube(Transform parent, Material stickerMat)
        {
            // 薄い“ステッカー”6枚で見せる（各面ごとに _FaceIndex/_RubikCenter を設定）
            for (int face = 0; face < 6; face++)
            {
                var q = Quaternion.identity;
                var n = Vector3.forward;
                switch (face)
                {
                    case 0: q = Quaternion.Euler(  0,   0,   0); n = Vector3.up;    break; // Up
                    case 1: q = Quaternion.Euler(180,   0,   0); n = Vector3.down;  break; // Down
                    case 2: q = Quaternion.Euler(  0,  90,   0); n = Vector3.left;  break; // Left
                    case 3: q = Quaternion.Euler(  0, -90,   0); n = Vector3.right; break; // Right
                    case 4: q = Quaternion.Euler(-90,   0,   0); n = Vector3.forward; break; // Front
                    case 5: q = Quaternion.Euler( 90,   0,   0); n = Vector3.back;    break; // Back
                }

                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediate(go.GetComponent<Collider>());
                go.name = $"Sticker_{face}";
                go.transform.SetParent(parent, false);
                go.transform.localRotation = q;
                go.transform.localPosition = n * 0.51f;
                go.transform.localScale = Vector3.one * 1.02f;

                var mr = go.GetComponent<MeshRenderer>();
                // サムネ用にインスタンス化（本体アセットを書き換えない）
                mr.sharedMaterial = Object.Instantiate(stickerMat);

                var mpb = new MaterialPropertyBlock();
                mpb.SetFloat("_FaceIndex", face);
                mpb.SetVector("_RubikCenter", new Vector4(parent.position.x, parent.position.y, parent.position.z, 1));
                mr.SetPropertyBlock(mpb);
            }

            // ボディ（薄い黒）— 任意
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(parent, false);
            body.transform.localScale = Vector3.one * 1.0f;
            var bodyMr = body.GetComponent<MeshRenderer>();
            var dark = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            dark.color = new Color(0.05f, 0.05f, 0.06f, 1);
            bodyMr.sharedMaterial = dark;
        }
    }
}
