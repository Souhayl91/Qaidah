using UnityEngine;
using UnityEngine.UI;

namespace SimpleQaidah.UI
{
    /// <summary>
    /// Simple helper that wires the header back button to the LessonFlowController at runtime.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BackButtonHelper : MonoBehaviour
    {
        private void Start()
        {
            var flow = FindAnyObjectByType<LessonFlowController>();
            if (flow != null)
            {
                GetComponent<Button>().onClick.AddListener(flow.BackToMenu);
            }
        }
    }
}
