using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ปุ่มขยายเล็กน้อยตอนเอาเมาส์ชี้ (และหดนิดนึงตอนกด) แปะที่ปุ่มตัวไหนก็ได้
/// เปิด Pulse เพื่อให้ปุ่ม "เต้น" เป็นจังหวะระหว่างที่เมาส์ชี้อยู่
/// ใช้เวลาจริง (unscaled) จึงทำงานได้แม้เกมถูกหยุดด้วย Time.timeScale = 0
/// </summary>
[DisallowMultipleComponent]
public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField, Min(1f)] private float hoverScale = 1.08f;
    [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.96f;
    [Tooltip("ความเร็วการขยาย/หด (ยิ่งมากยิ่งเร็ว)")]
    [SerializeField, Min(1f)] private float speed = 14f;

    [Header("เต้นตอนเมาส์ชี้")]
    [Tooltip("เปิด = ขยายแล้วเต้นเป็นจังหวะตลอดที่เมาส์ชี้อยู่ ปิด = ขยายนิ่งๆ แบบเดิม")]
    [SerializeField] private bool pulse = false;
    [Tooltip("ปุ่มจะเต้นใหญ่ขึ้นอีกเท่านี้ (0.06 = ใหญ่ขึ้นอีก 6%)")]
    [SerializeField, Min(0f)] private float pulseAmount = 0.06f;
    [Tooltip("จำนวนจังหวะเต้นต่อวินาที")]
    [SerializeField, Min(0.1f)] private float pulseSpeed = 2.5f;

    private Vector3 baseScale;
    private float target = 1f;
    private float current = 1f;
    private bool hovering;
    private bool pressed;
    private float pulseWeight;
    private float pulseStart;
    private Button button;

    private void Awake()
    {
        baseScale = transform.localScale;
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        hovering = false;
        pressed = false;
        pulseWeight = 0f;
        target = current = 1f;
        transform.localScale = baseScale;
    }

    private void OnDisable()
    {
        transform.localScale = baseScale; // ปุ่มถูกซ่อนตอนชี้อยู่ จะไม่ค้างขนาดขยาย
    }

    private bool Interactable => button == null || button.interactable;

    public void OnPointerEnter(PointerEventData e)
    {
        hovering = true;
        pulseStart = Time.unscaledTime; // เริ่มจังหวะเต้นจากขนาดปกติ ไม่กระตุก
        target = Interactable ? hoverScale : 1f;
    }

    public void OnPointerExit(PointerEventData e) { hovering = false; target = 1f; }
    public void OnPointerDown(PointerEventData e) { if (Interactable) { pressed = true; target = pressedScale; } }
    public void OnPointerUp(PointerEventData e) { pressed = false; target = (hovering && Interactable) ? hoverScale : 1f; }

    private void Update()
    {
        bool shouldPulse = pulse && hovering && !pressed && Interactable;
        if (!shouldPulse && Mathf.Approximately(pulseWeight, 0f) && Mathf.Approximately(current, target)) return;

        current = Mathf.MoveTowards(current, target, speed * Time.unscaledDeltaTime * Mathf.Abs(hoverScale - pressedScale));
        pulseWeight = Mathf.MoveTowards(pulseWeight, shouldPulse ? 1f : 0f, Time.unscaledDeltaTime * 8f);

        // 0 -> 1 -> 0 เป็นจังหวะ (เริ่มที่ 0 เสมอ)
        float wave = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * pulseSpeed * (Time.unscaledTime - pulseStart)));
        transform.localScale = baseScale * (current + pulseAmount * wave * pulseWeight);
    }
}
