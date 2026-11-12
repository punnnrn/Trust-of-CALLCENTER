using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// คัทซีนก่อนขึ้นหน้า Win / Lose ของเกมหลัก (ไม่เกี่ยวกับมินิเกม)
/// สร้างฉากทับจอเองตอนรันไทม์ ไม่ต้องลาก Object ในซีน: จอเฟดมืด -> ข้อความทีละบรรทัด -> เรียก onFinished (โชว์ Panel) -> เฟดฉากออก
/// ใช้เวลาจริง (unscaled) เพราะตอนจบเกมจะตั้ง Time.timeScale = 0 ไว้แล้ว คลิกที่จอเพื่อข้ามได้
/// </summary>
public class EndingCutscene : MonoBehaviour
{
    private static readonly string[] DefaultWin =
    {
        "หนี้ก้อนสุดท้าย... ถูกจ่ายหมดแล้ว",
        "ไม่มีสายทวงหนี้อีกต่อไป",
        "ในที่สุด คุณก็เป็นอิสระ"
    };

    private static readonly string[] DefaultLose =
    {
        "ทุกอย่างพังลงทีละนิด...",
        "สายสุดท้ายดังขึ้น แต่ไม่มีใครรับอีกแล้ว",
        "จบเกมแล้ว"
    };

    private bool win;
    private string[] lines;
    private Action onFinished;
    private TMP_FontAsset font;

    private CanvasGroup overlay;
    private TMP_Text label;
    private TMP_Text hint;
    private bool skip;

    /// เริ่มคัทซีน lines เป็น null/ว่าง = ใช้ข้อความมาตรฐาน font เป็น null = ฟอนต์ TMP เริ่มต้น
    public static EndingCutscene Play(bool win, string[] lines, TMP_FontAsset font, Action onFinished)
    {
        GameObject go = new GameObject(win ? "EndingCutscene_Win" : "EndingCutscene_Lose");
        EndingCutscene c = go.AddComponent<EndingCutscene>();
        c.win = win;
        c.lines = (lines != null && lines.Length > 0) ? lines : (win ? DefaultWin : DefaultLose);
        c.font = font;
        c.onFinished = onFinished;
        c.Build();
        c.StartCoroutine(c.Run());
        return c;
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000; // ทับ UI ทั้งหมด

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // พื้นหลังเต็มจอ (กดเพื่อข้าม)
        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Button));
        bgGo.transform.SetParent(transform, false);
        RectTransform bgRt = (RectTransform)bgGo.transform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        Image bg = bgGo.GetComponent<Image>();
        bg.color = win ? new Color(0.03f, 0.04f, 0.10f, 1f) : new Color(0.07f, 0.01f, 0.01f, 1f);
        overlay = bgGo.GetComponent<CanvasGroup>();
        overlay.alpha = 0f;
        bgGo.GetComponent<Button>().onClick.AddListener(() => skip = true);

        label = MakeText(bgGo.transform, "Line", 56, TextAlignmentOptions.Center,
            win ? new Color(1f, 0.90f, 0.50f, 1f) : new Color(1f, 0.55f, 0.50f, 1f));
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0.1f, 0.3f); lrt.anchorMax = new Vector2(0.9f, 0.7f);
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        label.alpha = 0f;

        hint = MakeText(bgGo.transform, "SkipHint", 28, TextAlignmentOptions.BottomRight, new Color(1f, 1f, 1f, 0.35f));
        hint.text = "คลิกเพื่อข้าม";
        RectTransform hrt = hint.rectTransform;
        hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
        hrt.offsetMin = new Vector2(40f, 30f); hrt.offsetMax = new Vector2(-40f, -30f);
    }

    private TMP_Text MakeText(Transform parent, string objName, float size, TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.enableWordWrapping = true;
        return t;
    }

    private IEnumerator Run()
    {
        yield return Fade(a => overlay.alpha = a, 0f, 1f, 1.2f);

        for (int i = 0; i < lines.Length && !skip; i++)
        {
            label.text = lines[i];
            yield return Fade(a => label.alpha = a, 0f, 1f, 0.7f);
            yield return Wait(1.6f);
            yield return Fade(a => label.alpha = a, label.alpha, 0f, 0.6f);
            yield return Wait(0.2f);
        }

        label.alpha = 0f;
        hint.alpha = 0f;

        onFinished?.Invoke(); // โชว์หน้า Win/Lose ไว้ข้างล่างก่อน แล้วค่อยเฟดฉากคัทซีนออก

        yield return Fade(a => overlay.alpha = a, 1f, 0f, 0.6f);
        Destroy(gameObject);
    }

    private IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds && !skip) { t += Time.unscaledDeltaTime; yield return null; }
    }

    private IEnumerator Fade(Action<float> set, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            if (skip && to < from) { set(to); yield break; } // ข้ามระหว่างเฟดออก
            t += Time.unscaledDeltaTime;
            set(Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        set(to);
    }
}
