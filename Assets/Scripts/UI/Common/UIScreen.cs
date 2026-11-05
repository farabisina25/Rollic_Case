using UnityEngine;

namespace BlockJam.UI.Common
{
    public abstract class UIScreen : MonoBehaviour
    {
        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
    }
}
