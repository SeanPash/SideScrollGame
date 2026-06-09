using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

// Small on-screen banner for tutorial hints and ability-unlock messages.
// Other scripts call ProgressionHud.Show(text, duration). It also subscribes to
// PlayerProgress.OnUnlock to announce unlocks automatically. If TextMeshPro is
// not used in this project, swap TMP_Text for UnityEngine.UI.Text.
public class ProgressionHud : MonoBehaviour
{
    public static ProgressionHud Instance;

    [Tooltip("Assign a TextMeshPro UI text element.")]
    public TMP_Text label;
    [Tooltip("CanvasGroup on the banner, used to show/hide.")]
    public CanvasGroup group;

    private Coroutine hideRoutine;

    void Awake()
    {
        Instance = this;
        if (group != null) group.alpha = 0f;
    }

    // Subscribe in Start so PlayerProgress.Awake has already run.
    void Start()
    {
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnUnlock += HandleUnlock;
    }

    void OnDestroy()
    {
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnUnlock -= HandleUnlock;
    }

    // Auto-announce any unlock.
    private void HandleUnlock(AbilityId id) => Show($"Unlocked: {Prettify(id)}", 3f);

    // Static entry point so triggers can show hints without a direct reference.
    public static void Show(string text, float duration)
    {
        if (Instance == null) { Debug.Log($"[ProgressionHud] {text}"); return; }
        Instance.ShowInternal(text, duration);
    }

    private void ShowInternal(string text, float duration)
    {
        if (label != null) label.text = text;
        if (group != null) group.alpha = 1f;
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfter(duration));
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (group != null) group.alpha = 0f;
    }

    // Turn an enum name into spaced words: DoubleJump -> Double Jump.
    private static string Prettify(AbilityId id)
    {
        string s = id.ToString();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (i > 0 && char.IsUpper(s[i])) sb.Append(' ');
            sb.Append(s[i]);
        }
        return sb.ToString();
    }
}
