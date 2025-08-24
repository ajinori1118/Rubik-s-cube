using System.Collections.Generic;
using UnityEngine;

namespace Rubik
{
    public class CubeBuilder : MonoBehaviour
    {
        [Header("Build")]
        public bool BuildOnStart = true;
        public float cubeletSize = 0.98f; 
        public float stickerInset = 0.51f;
        public float stickerScale = 0.95f;

        [Header("Materials")]
        public Material baseMaterial;
        public Material stickerMaterial;

        [HideInInspector] public Transform cubeRoot;
        [HideInInspector] public List<Cubelet> cubelets = new();

        void Start()
        {
            if (BuildOnStart) Build();
        }

        [ContextMenu("Build Cube")]
        public void Build()
        {
            // clear 
            if (cubeRoot != null) DestroyImmediate(cubeRoot.gameObject);
            cubelets.Clear();

            cubeRoot = new GameObject("CubeRoot").transform;
            cubeRoot.SetParent(transform, false);

            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Cubelet_{x}_{y}_{z}";
                go.transform.SetParent(cubeRoot, false);
                go.transform.localPosition = new Vector3(x, y, z);
                go.transform.localScale = Vector3.one * cubeletSize;

                if (baseMaterial) go.GetComponent<MeshRenderer>().sharedMaterial = baseMaterial;

                var cubelet = go.AddComponent<Cubelet>();
                cubelet.coord = new Vector3Int(x, y, z);

                // sticker 
                CreateStickerIfEdge(go.transform, cubelet, Face.Right,  Vector3.right);
                CreateStickerIfEdge(go.transform, cubelet, Face.Left,   Vector3.left);
                CreateStickerIfEdge(go.transform, cubelet, Face.Up,     Vector3.up);
                CreateStickerIfEdge(go.transform, cubelet, Face.Down,   Vector3.down);
                CreateStickerIfEdge(go.transform, cubelet, Face.Front,  Vector3.forward);
                CreateStickerIfEdge(go.transform, cubelet, Face.Back,   Vector3.back);

                cubelets.Add(cubelet);
            }
        }

        void CreateStickerIfEdge(Transform parent, Cubelet cubelet, Face face, Vector3 n)
        {

            Vector3Int c = cubelet.coord;
            if ((n == Vector3.right   && c.x !=  1) ||
                (n == Vector3.left    && c.x != -1) ||
                (n == Vector3.up      && c.y !=  1) ||
                (n == Vector3.down    && c.y != -1) ||
                (n == Vector3.forward && c.z !=  1) ||
                (n == Vector3.back    && c.z != -1))
                return;

            var sticker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sticker.name = $"Sticker_{face}";
            sticker.transform.SetParent(parent, false);
            sticker.transform.localPosition = n * stickerInset;
            sticker.transform.localRotation = Quaternion.LookRotation(n);
            sticker.transform.localScale = Vector3.one * stickerScale;

            var mr = sticker.GetComponent<MeshRenderer>();
            if (stickerMaterial) mr.sharedMaterial = stickerMaterial;

            DestroyImmediate(sticker.GetComponent<MeshCollider>());
            var bc = sticker.AddComponent<BoxCollider>();
            bc.isTrigger = false;

            var tag = sticker.AddComponent<StickerTag>();
            tag.face = face;
            tag.cubelet = cubelet;
            tag.normalWS = n;
        }
    }
}