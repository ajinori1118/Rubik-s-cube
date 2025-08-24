using UnityEngine;
using TMPro;

namespace UI
{
    public class TimerUI : MonoBehaviour
    {
        public TextMeshProUGUI label;
        bool running = false;
        float t0 = 0f;

        public void StartTimer()
        {
            if (!running)
            {
                running = true;
                t0 = Time.time;
            }
        }

        public void ResetTimer()
        {
            running = false;
            t0 = Time.time;
            if (label) label.text = "00:00.00";
        }

        void Update()
        {
            if (!label) return;

            float t = running ? (Time.time - t0) : 0f;
            int min = Mathf.FloorToInt(t / 60f);
            float sec = t - min * 60f;
            label.text = $"{min:00}:{sec:00.00}";
        }
    }
}