using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แปะบน IncomingPopup — ทำให้ป๊อปอัปสายเข้า "ดึงสายตา" ตอนสายดัง
///  - ย้ายไปกลางจอ (บังมินิเกมได้) และขยายใหญ่ขึ้น
///  - สั่นแบบโทรศัพท์สั่น (สั่น/พักเป็นจังหวะ) + กรอบเรืองแสงกะพริบ
///  - ขอบจอมีแสงแดงเต้นเบา ๆ ให้เห็นในหางตาแม้กำลังเพ่งมินิเกม
/// ไม่แตะ scale (CallCenterManager คุม PopIn/pulse อยู่) จึงไม่ต้องแก้สคริปต์เดิม
/// ทำงานเฉพาะตอน popupButton.interactable == true ส่วนข้อความ "พลาดสาย" จะแสดงที่มุมเดิม
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class IncomingCallAttention : MonoBehaviour
{
    [Header("ตำแหน่ง/ขนาดตอนสายดัง (พิกัด Canvas 1920x1080, 0,0 = กลางจอ)")]
    [SerializeField] private Vector2 ringingCenter = new Vector2(0f, -360f);
    [SerializeField] private Vector2 ringingSize = new Vector2(540f, 120f);
    [SerializeField] private float ringingFontScale = 1.6f;

    [Header("สั่นแบบโทรศัพท์")]
    [SerializeField] private float shakeAngle = 4f;
    [SerializeField] private float shakePixels = 6f;
    [SerializeField] private float shakeFrequency = 50f;
    [SerializeField] private float shakeCycle = 1.1f;
    [SerializeField, Range(0.1f, 0.9f)] private float shakeActiveRatio = 0.55f;

    [Header("แสง (ช้ากว่า 3 ครั้ง/วินาที เพื่อไม่ให้ปวดตา)")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private float glowSpeed = 9f;
    [SerializeField] private float glowDistance = 9f;
    [SerializeField] private Color screenFlashColor = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField, Range(0f, 1f)] private float screenFlashAlpha = 0.5f;

    private RectTransform rt;
    private RectTransform canvasRt;
    private Button button;
    private TMP_Text label;
    private Outline outline;
    private Image flash;

    private Vector2 homePivot, homePos, homeSize;
    private float homeFont, homeFontMin, homeFontMax;
    private bool homeAuto;
    private bool ringing;
    private float startTime;

    private static Sprite vignetteSprite;

    private void Awake()
    {
        rt = (RectTransform)transform;
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>(true);
        Canvas canvas = GetComponentInParent<Canvas>(true);
        canvasRt = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;

        homePivot = rt.pivot;
        homePos = rt.anchoredPosition;
        homeSize = rt.sizeDelta;
        if (label != null)
        {
            homeFont = label.fontSize;
            homeFontMin = label.fontSizeMin;
            homeFontMax = label.fontSizeMax;
            homeAuto = label.enableAutoSizing;
        }

        outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();
        outline.useGraphicAlpha = false;
        outline.enabled = false;
    }

    private void OnEnable()
    {
        ringing = false;
    }

    private void OnDisable()
    {
        LeaveRinging();
    }

    private void OnDestroy()
    {
        if (flash != null) Destroy(flash.gameObject);
    }

    private void Update()
    {
        bool isRinging = button != null && button.interactable;
        if (isRinging != ringing)
        {
            if (isRinging) EnterRinging();
            else LeaveRinging();
        }
        if (!ringing) return;

        float t = Time.unscaledTime - startTime;

        // สั่นเป็นจังหวะ: สั่น (ขึ้นลงนุ่ม ๆ) แล้วพัก
        float cycle = Mathf.Max(0.2f, shakeCycle);
        float phase = (t % cycle) / cycle;
        float k = 0f;
        if (phase < shakeActiveRatio)
            k = Mathf.Sin(phase / shakeActiveRatio * Mathf.PI);

        float angle = Mathf.Sin(t * shakeFrequency) * shakeAngle * k;
        Vector2 jitter = new Vector2(
            (Mathf.PerlinNoise(t * 40f, 0.1f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(0.7f, t * 40f) - 0.5f) * 2f) * (shakePixels * k);

        if (canvasRt != null)
        {
            Vector2 c = ringingCenter + jitter;
            rt.position = canvasRt.TransformPoint(new Vector3(c.x, c.y, 0f));
        }
        else
        {
            rt.anchoredPosition = homePos + jitter;
        }
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        // กรอบเรืองแสง + แสงขอบจอ เต้นตามจังหวะเดียวกัน
        float s = (Mathf.Sin(t * glowSpeed) + 1f) * 0.5f;
        outline.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Lerp(0.3f, 1f, s));
        outline.effectDistance = Vector2.one * Mathf.Lerp(glowDistance * 0.4f, glowDistance, s);

        if (flash != null)
        {
            float a = screenFlashAlpha * Mathf.Lerp(0.25f, 1f, s);
            flash.color = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, a);
        }
    }

    private void EnterRinging()
    {
        ringing = true;
        startTime = Time.unscaledTime;

        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = ringingSize;

        if (label != null)
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = homeFont * ringingFontScale;
            label.fontSizeMin = homeFont * 0.6f;
        }

        outline.enabled = true;
        EnsureFlash();
    }

    private void LeaveRinging()
    {
        ringing = false;
        if (rt == null) return;

        rt.localRotation = Quaternion.identity;
        rt.pivot = homePivot;
        rt.sizeDelta = homeSize;
        rt.anchoredPosition = homePos;

        if (label != null)
        {
            label.enableAutoSizing = homeAuto;
            label.fontSizeMin = homeFontMin;
            label.fontSizeMax = homeFontMax;
            label.fontSize = homeFont;
        }

        if (outline != null) outline.enabled = false;
        if (flash != null) flash.gameObject.SetActive(false);
    }

    /// <summary>แสงขอบจอเต็ม Canvas วางไว้ใต้ป๊อปอัป (เหนือมินิเกม) และไม่บังการคลิก</summary>
    private void EnsureFlash()
    {
        if (transform.parent == null) return;

        if (flash == null)
        {
            GameObject go = new GameObject("IncomingCallFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform.parent, false);
            flash = go.GetComponent<Image>();
            flash.sprite = GetVignette();
            flash.raycastTarget = false;
            flash.color = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, 0f);
        }

        RectTransform frt = (RectTransform)flash.transform;
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.pivot = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = Vector2.zero;
        frt.localScale = Vector3.one;
        frt.sizeDelta = canvasRt != null ? canvasRt.rect.size + new Vector2(8f, 8f) : new Vector2(1928f, 1088f);

        if (frt.GetSiblingIndex() > transform.GetSiblingIndex())
            frt.SetSiblingIndex(transform.GetSiblingIndex());
        flash.gameObject.SetActive(true);
    }

    private static Sprite GetVignette()
    {
        if (vignetteSprite != null) return vignetteSprite;

        const int n = 128;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x / (n - 1f) * 2f - 1f;
                float dy = y / (n - 1f) * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0.35f, 1f, d)));
            }
        }
        tex.Apply();
        vignetteSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        vignetteSprite.hideFlags = HideFlags.HideAndDontSave;
        return vignetteSprite;
    }
}
