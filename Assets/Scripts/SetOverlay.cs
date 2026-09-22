using UnityEngine;
using UnityEngine.UI;

public class SetOverlay : MonoBehaviour
{
    public Image overlay;

    void Start()
    {
        if (overlay == null) return;

        // stretch to fully cover the screen regardless of resolution. A Screen Space - Overlay
        // Canvas always matches the screen size, so anchoring 0,0 -> 1,1 with zero offsets is
        // all that's needed - no camera reference or per-frame syncing required.
        RectTransform rect = overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay.gameObject.SetActive(false); // hidden until Show() is called
    }

    public void Show()
    {
        if (overlay != null) overlay.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (overlay != null) overlay.gameObject.SetActive(false);
    }
}
