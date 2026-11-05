using UnityEngine;
using UnityEngine.UI;

namespace BlockJam.UI.Home
{
    /// <summary>
    /// Placeholder tab bar: selecting a tab only moves the active highlight, no section is opened.
    /// </summary>
    public sealed class TabBarView : MonoBehaviour
    {
        [SerializeField] private Button[] tabs;
        [SerializeField] private RectTransform activeHighlight;
        [SerializeField] private int defaultTabIndex = 2;

        private void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].onClick.AddListener(() => Select(index));
            }
        }

        private void Start() => Select(defaultTabIndex);

        private void Select(int index)
        {
            activeHighlight.SetParent(tabs[index].transform, false);
            activeHighlight.SetAsFirstSibling();
        }
    }
}
