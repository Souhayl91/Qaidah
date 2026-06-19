using UnityEngine;
using UnityEngine.UI;

namespace SimpleQaidah.UI
{
    public class ProgressBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        public void SetProgress(float value)
        {
            fillImage.fillAmount = Mathf.Clamp01(value);
        }
    }
}
