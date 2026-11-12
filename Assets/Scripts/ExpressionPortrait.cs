using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แปะบน Image ของตัวละคร: เปลี่ยนรูปแบบเฟดค่อย ๆ ซ้อนกัน + เด้งเล็กน้อยตอนเปลี่ยนสีหน้า
/// และเปิด "หายใจ" (ขยับขึ้นลง/ขยายนิดเดียว) ให้ตัวละครดูมีชีวิตตอนอยู่นิ่ง ๆ ได้
/// ใช้เวลาแบบ unscaled จึงยังเคลื่อนไหวแม้เกมถูก Pause
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class ExpressionPortrait : MonoBehaviour
{
    [Header("เปลี่ยนสีหน้า")]
    [SerializeField, Min(0f)] private float fadeSeconds = 0.18f;
    [Tooltip("ขยายเด้งเล็กน้อยตอนเปลี่ยนสีหน้า (0 = ปิด)")]
    [SerializeField, Min(0f)] private float popScale = 0.015f;
    [SerializeField, Min(0.05f)] private float popSeconds = 0.25f;

    [Header("ขยับเบา ๆ ตอนนิ่ง (หายใจ)")]
    [SerializeField] private bool idleMotion = false;
    [SerializeField, Min(0f)] private float breatheScale = 0.004f;
    [SerializeField, Min(0f)] private float breatheBobPixels = 2f;
    [SerializeField, Min(0.1f)] private float breatheSpeed = 1.6f;

    private Image main;
    private Image fade;
    private RectTransform rt;
    private Sprite target;
    private Coroutine fadeRoutine;
    private int enabledFrame = -1;
    private Vector3 baseScale = Vector3.one;
    private Vector2 basePos;
    private bool motionApplied;
    private float popT = -1f;

    private void Awake()
    {
        main = GetComponent<Image>();
        rt = (RectTransform)transform;
    }

    private void OnEnable()
    {
        if (main == null) main = GetComponent<Image>();
        if (rt == null) rt = (RectTransform)transform;
        enabledFrame = Time.frameCount;
        baseScale = rt.localScale;
        basePos = rt.anchoredPosition;
        motionApplied = false;
        popT = -1f;
        target = main.sprite;
    }

    private void OnDisable()
    {
        FinishFade();
        if (motionApplied) RestoreTransform();
        popT = -1f;
    }

    public void SetIdleMotion(bool on)
    {
        idleMotion = on;
    }

    /// เปลี่ยนเป็นสีหน้านั้น (ถ้าไม่มีไฟล์รูป ใช้ Neutral หรือ fallback)
    public void Show(FaceExpression expression, Sprite fallback = null, bool instant = false)
    {
        SetSprite(ExpressionLibrary.Resolve(expression, fallback), instant);
    }

    /// เปลี่ยนรูปแบบเฟด (instant = true เปลี่ยนทันที ไม่มีเฟด/เด้ง)
    public void SetSprite(Sprite sprite, bool instant = false)
    {
        if (sprite == null) return;
        if (main == null) main = GetComponent<Image>();
        if (sprite == target) return;

        if (!isActiveAndEnabled)
        {
            main.sprite = sprite;
            target = sprite;
            return;
        }

        FinishFade();
        target = sprite;
        if (main.sprite == sprite) return;

        bool animate = !instant && fadeSeconds > 0f && Time.frameCount != enabledFrame && main.sprite != null;
        if (!animate)
        {
            main.sprite = sprite;
            return;
        }

        EnsureFadeImage();
        Color c = main.color;
        fade.sprite = sprite;
        fade.color = new Color(c.r, c.g, c.b, 0f);
        fade.gameObject.SetActive(true);
        if (popScale > 0f) popT = 0f;
        fadeRoutine = StartCoroutine(FadeRoutine(sprite));
    }

    private IEnumerator FadeRoutine(Sprite sprite)
    {
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            Color c = main.color;
            fade.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(t / fadeSeconds));
            yield return null;
        }
        main.sprite = sprite;
        fade.gameObject.SetActive(false);
        fadeRoutine = null;
    }

    private void FinishFade()
    {
        if (fadeRoutine == null) return;
        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
        if (fade != null)
        {
            if (fade.sprite != null) main.sprite = fade.sprite;
            fade.gameObject.SetActive(false);
        }
    }

    private void EnsureFadeImage()
    {
        if (fade != null) return;
        GameObject go = new GameObject("ExpressionFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        RectTransform frt = (RectTransform)go.transform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        fade = go.GetComponent<Image>();
        fade.raycastTarget = false;
        fade.preserveAspect = main.preserveAspect;
        fade.type = main.type;
        go.transform.SetAsLastSibling();
        go.SetActive(false);
    }

    private void Update()
    {
        bool popping = popT >= 0f;
        if (!idleMotion && !popping)
        {
            if (motionApplied) RestoreTransform();
            return;
        }

        float mul = 1f;
        float bob = 0f;
        if (idleMotion)
        {
            float s = Mathf.Sin(Time.unscaledTime * breatheSpeed);
            mul += breatheScale * s;
            bob = breatheBobPixels * s;
        }
        if (popping)
        {
            popT += Time.unscaledDeltaTime / popSeconds;
            if (popT >= 1f) popT = -1f;
            else mul += popScale * Mathf.Sin(popT * Mathf.PI);
        }

        rt.localScale = baseScale * mul;
        rt.anchoredPosition = basePos + new Vector2(0f, bob);
        motionApplied = true;
    }

    private void RestoreTransform()
    {
        rt.localScale = baseScale;
        rt.anchoredPosition = basePos;
        motionApplied = false;
    }
}
