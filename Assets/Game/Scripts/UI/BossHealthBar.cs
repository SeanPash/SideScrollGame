using UnityEngine;
using UnityEngine.UI;

// On-screen health bar for one boss. The phase manager shows, hides, and updates it.
public class BossHealthBar : MonoBehaviour
{
    [Header("References")]
    // Filled-type Image whose fillAmount displays the health fraction.
    public Image fillImage;
    // Root object toggled by Show/Hide. Defaults to this object.
    public GameObject root;

    void Awake()
    {
        if (root == null) root = gameObject;
    }

    // Sets the bar fill to a 0..1 fraction.
    public void SetPercent(float percent)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(percent);
    }

    // Makes the bar visible.
    public void Show()
    {
        if (root != null) root.SetActive(true);
    }

    // Hides the bar.
    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }
}
