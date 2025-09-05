using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rubik
{
    public class ModeSelectUI : MonoBehaviour
    {
        [Header("Target")]
        public StickerShaderModeSwitcher switcher;

        [Header("UI")]
        public RectTransform gridRoot;
        public ModeThumbnail thumbnailPrefab;

        [Header("Entries (順に表示)")]
        public List<Material> modeMaterials;
        public List<string>   modeTitles;

        [Header("Preview")]
        public int previewSize = 256;

        void Start()
        {
            if (!switcher) switcher = FindObjectOfType<StickerShaderModeSwitcher>(true);
            BuildGallery();
            gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        void BuildGallery()
        {
            for (int i = 0; i < modeMaterials.Count; i++)
            {
                var mat = modeMaterials[i];
                var title = (i < modeTitles.Count) ? modeTitles[i] : $"Mode {i}";
                var th = Instantiate(thumbnailPrefab, gridRoot);
                th.Init(this, i, title, mat, previewSize);
            }
        }

        public void Choose(int modeIndex)
        {
            Time.timeScale = 1f;
            if (switcher) switcher.SetMode(modeIndex);

            gameObject.SetActive(false);
        }
    }
}
