using System.Collections;
using TMPro;
using UnityEngine;

/// แถบแจ้งเตือนสั้นๆ กลางจอด้านบน เรียกใช้จากที่ไหนก็ได้: NotificationBanner.Show("ข้อความ");
/// (GameObject ต้อง Active ไว้ตลอด ใช้ CanvasGroup.alpha ซ่อน/โชว์)
public class NotificationBanner : MonoBehaviour
{
    public static NotificationBanner Instance { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float defaultDuration = 3.5f;
    [SerializeField] private float fadeInTime = 0.2f;
    [SerializeField] private float fadeOutTime = 0.5f;

    private Coroutine routine;

    private void Awake()
    {
        Instance = this;
        if (group != null) group.alpha = 0f;
    }

    // ถ้า GamePlayUI (หรือแม่ของมัน) ถูกปิดตอนแถบกำลังโชว์ Coroutine จะถูกฆ่าทิ้ง ทำให้ alpha ค้างที่ 1
    // เลยรีเซ็ตให้ซ่อนทุกครั้งที่ถูกปิด/เปิดใหม่
    private void OnEnable() => HideImmediate();

    private void OnDisable() => HideImmediate();

    private void HideImmediate()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (group != null) group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Show(string message, float duration = -1f)
    {
        if (Instance != null) Instance.Display(message, duration);
    }

    private void Display(string message, float duration)
    {
        if (messageText == null || group == null) return;

        messageText.text = message;
        transform.SetAsLastSibling();

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Play(duration > 0f ? duration : defaultDuration));
    }

    private IEnumerator Play(float hold)
    {
        yield return Fade(group.alpha, 1f, fadeInTime);
        yield return new WaitForSecondsRealtime(hold);
        yield return Fade(1f, 0f, fadeOutTime);
        routine = null;
    }

    private IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        group.alpha = to;
    }
}
