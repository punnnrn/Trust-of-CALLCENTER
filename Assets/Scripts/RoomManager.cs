using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// คุม UI ห้อง: เตียง (กดแล้วขึ้นคัทซีนให้เลือกนอน/ไม่นอน) และคอมพิวเตอร์ (กดแล้วเปิดหน้าจอคอมเต็มจอ)
/// ตอนเปิดคอม จะซ่อนปุ่มเตียงกับปุ่มคอมไว้ก่อน จนกว่าจะปิดคอมกลับมาที่ห้อง
///
/// แอปในคอม: วางเป็นไอคอนปุ่มธรรมดาใน computerScreenPanel แล้วผูก OnClick() ตรงๆ ใน Inspector
/// เช่น แอปชำระหนี้ -> เรียก DebtPaymentUI.OpenPanel() ของพาแนลที่ทำไว้แล้ว ไม่ต้องแก้โค้ดตัวนั้นเลย
/// แอปใหม่ในอนาคตก็ทำแบบเดียวกัน: สร้างไอคอน + พาแนลของแอปนั้น + ผูก OnClick ไม่ต้องแก้ RoomManager
/// </summary>
public class RoomManager : MonoBehaviour
{
    [Header("ห้อง")]
    [SerializeField] private Button bedButton;
    [SerializeField] private Button computerButton;

    [Header("หน้าจอคอมพิวเตอร์")]
    [SerializeField] private GameObject computerScreenPanel;
    [Tooltip("ปุ่มปิดคอม อยู่ข้างใน computerScreenPanel")]
    [SerializeField] private Button closeComputerButton;

    [Header("คัทซีนตอนนอน (กดเตียงแล้วขึ้นมา มีปุ่มให้เลือก นอน / ไม่นอน)")]
    [Tooltip("Object คัทซีนเต็มจอ: ต้องมี CanvasGroup, มีรูปคัทซีน และมีปุ่ม นอน/ไม่นอน เป็นลูก " +
             "ต้องอยู่นอก gameplayUIRoot และวางไว้ล่างสุดของ Canvas (ให้บังทุกอย่าง)")]
    [SerializeField] private CanvasGroup sleepConfirmGroup;
    [SerializeField] private Button confirmSleepButton; // ปุ่ม "นอน"
    [SerializeField] private Button cancelSleepButton;  // ปุ่ม "ไม่นอน"
    [Tooltip("เวลา (วินาที) ที่คัทซีนค่อยๆ ขึ้นมาตอนกดเตียง")]
    [SerializeField] private float fadeInSeconds = 0.4f;
    [Tooltip("เวลา (วินาที) ที่คัทซีนเฟดหายหลังกด \"นอน\" (ระหว่างเฟดจะเห็นหน้าสรุปผลวันโผล่ขึ้นมา)")]
    [SerializeField] private float sleepFadeOutSeconds = 5f;
    [Tooltip("เวลา (วินาที) ที่คัทซีนเฟดหายหลังกด \"ไม่นอน\"")]
    [SerializeField] private float cancelFadeOutSeconds = 0.5f;

    [Header("เวลานอน")]
    [Tooltip("นอนได้ตั้งแต่ชั่วโมงนี้เป็นต้นไป (เวลาในเกม) 16 = หลัง 16:00")]
    [SerializeField, Range(0, 23)] private int sleepStartHour = 16;
    [Tooltip("ข้อความเตือนตอนกดเตียงเร็วเกินไป ไม่ใส่ก็ได้ (ถ้าไม่ใส่ จะกดแล้วไม่มีอะไรเกิดขึ้น)")]
    [SerializeField] private TMP_Text sleepHintText;
    [SerializeField] private string tooEarlyMessage = "นอนได้หลัง {0:00}:00 เท่านั้น";
    [SerializeField] private float hintSeconds = 2f;

    private Coroutine sleepRoutine;
    private Coroutine hintRoutine;

    private void Awake()
    {
        if (bedButton != null) bedButton.onClick.AddListener(OpenSleepConfirm);
        if (computerButton != null) computerButton.onClick.AddListener(OpenComputer);
        if (closeComputerButton != null) closeComputerButton.onClick.AddListener(CloseComputer);

        if (confirmSleepButton != null) confirmSleepButton.onClick.AddListener(ConfirmSleep);
        if (cancelSleepButton != null) cancelSleepButton.onClick.AddListener(CancelSleep);

        if (computerScreenPanel != null) computerScreenPanel.SetActive(false);
        HideCutsceneInstant();
        if (sleepHintText != null) sleepHintText.gameObject.SetActive(false);
        ShowRoomButtons(true);
    }

    // ---------------- เตียง / คัทซีนนอน ----------------

    /// กดปุ่มเตียง: คัทซีนค่อยๆ ขึ้นมา พร้อมปุ่มให้เลือกนอน/ไม่นอน
    public void OpenSleepConfirm()
    {
        if (sleepRoutine != null) return; // กำลังเล่นคัทซีนอยู่

        // ยังไม่ถึงเวลานอน
        if (GameClock.Instance != null && GameClock.Instance.Hour < sleepStartHour)
        {
            ShowSleepHint();
            return;
        }

        if (sleepConfirmGroup == null)
        {
            // ไม่ได้ต่อคัทซีน: นอนทันทีแบบเดิม
            CallCenterManager.Instance?.ForceSleepNow();
            return;
        }

        // ระหว่างเลือกนอน/กำลังนอน: ไม่ให้มีสายใหม่โทรเข้ามา และหยุดเวลาในเกม
        CallCenterManager.Instance?.SetCallsBlocked(true);
        GameClock.Instance?.Pause();

        ShowRoomButtons(false);
        sleepRoutine = StartCoroutine(OpenCutsceneRoutine());
    }

    private void ShowSleepHint()
    {
        if (sleepHintText == null) return;
        if (hintRoutine != null) StopCoroutine(hintRoutine);
        hintRoutine = StartCoroutine(HintRoutine());
    }

    private IEnumerator HintRoutine()
    {
        sleepHintText.text = string.Format(tooEarlyMessage, sleepStartHour);
        sleepHintText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(hintSeconds);
        sleepHintText.gameObject.SetActive(false);
        hintRoutine = null;
    }

    /// กด "นอน": จบวันจริง (ตัดสาย/บทสนทนาที่ค้างทิ้งทันที) แล้วคัทซีนเฟดหายให้เห็นหน้าสรุปผลวัน
    /// (ความหิวจะถูกเติมเต็มเองจาก CallCenterManager.EndDay())
    public void ConfirmSleep()
    {
        if (sleepConfirmGroup == null || !sleepConfirmGroup.interactable) return;
        sleepRoutine = StartCoroutine(SleepRoutine());
    }

    /// กด "ไม่นอน": คัทซีนเฟดหาย กลับมาที่ห้องเหมือนเดิม
    public void CancelSleep()
    {
        if (sleepConfirmGroup == null || !sleepConfirmGroup.interactable) return;
        sleepRoutine = StartCoroutine(CancelRoutine());
    }

    private IEnumerator OpenCutsceneRoutine()
    {
        sleepConfirmGroup.gameObject.SetActive(true);
        sleepConfirmGroup.alpha = 0f;
        sleepConfirmGroup.blocksRaycasts = true;
        sleepConfirmGroup.interactable = false; // ยังกดปุ่มไม่ได้จนกว่าจะขึ้นมาเต็ม

        yield return FadeCutscene(0f, 1f, fadeInSeconds);

        sleepConfirmGroup.interactable = true;
        sleepRoutine = null;
    }

    private IEnumerator SleepRoutine()
    {
        sleepConfirmGroup.interactable = false;

        // เฟดรูปออกให้หมดก่อน (ปุ่มห้องยังซ่อนอยู่ และรูปยังบังการกดอยู่ระหว่างเฟด)
        yield return FadeCutscene(1f, 0f, sleepFadeOutSeconds);

        HideCutsceneInstant();
        ShowRoomButtons(true);
        sleepRoutine = null;

        // เฟดจบแล้วค่อยจบวัน หน้าสรุปผลถึงจะขึ้นมา
        CallCenterManager.Instance?.ForceSleepNow();
    }

    private IEnumerator CancelRoutine()
    {
        sleepConfirmGroup.interactable = false;

        yield return FadeCutscene(1f, 0f, cancelFadeOutSeconds);

        HideCutsceneInstant();
        ShowRoomButtons(true);
        sleepRoutine = null;

        // ไม่นอนแล้ว: ให้สายโทรเข้ามาได้อีก และเวลาเดินต่อ
        CallCenterManager.Instance?.SetCallsBlocked(false);
        GameClock.Instance?.Resume();
    }

    private IEnumerator FadeCutscene(float from, float to, float seconds)
    {
        float dur = Mathf.Max(0.01f, seconds);
        float t = 0f;
        sleepConfirmGroup.alpha = from;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            sleepConfirmGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        sleepConfirmGroup.alpha = to;
    }

    private void HideCutsceneInstant()
    {
        if (sleepConfirmGroup == null) return;
        sleepConfirmGroup.alpha = 0f;
        sleepConfirmGroup.interactable = false;
        sleepConfirmGroup.blocksRaycasts = false;
        sleepConfirmGroup.gameObject.SetActive(false);
    }

    // ---------------- คอมพิวเตอร์ ----------------

    /// เปิดหน้าจอคอม ซ่อนปุ่มห้องไว้ก่อน
    public void OpenComputer()
    {
        if (sleepRoutine != null) return;
        if (computerScreenPanel != null) computerScreenPanel.SetActive(true);
        ShowRoomButtons(false);
    }

    /// ปิดคอม กลับมาที่ห้อง
    /// (ไม่แตะคัทซีนนอน เพราะ CallCenterManager.EndDay() เรียกเมธอดนี้ตอนคัทซีนกำลังเฟดอยู่)
    public void CloseComputer()
    {
        if (computerScreenPanel != null) computerScreenPanel.SetActive(false);
        ShowRoomButtons(true);
    }

    /// จบเกม (แพ้): ปิดคอม/คัทซีนที่เปิดค้างอยู่ แล้วซ่อนปุ่มเตียงกับปุ่มคอมถาวร ไม่ให้กดอะไรต่อได้
    public void LockForGameOver()
    {
        if (sleepRoutine != null) { StopCoroutine(sleepRoutine); sleepRoutine = null; }
        if (computerScreenPanel != null) computerScreenPanel.SetActive(false);
        HideCutsceneInstant();
        ShowRoomButtons(false);
    }

    private void ShowRoomButtons(bool show)
    {
        if (bedButton != null) bedButton.gameObject.SetActive(show);
        if (computerButton != null) computerButton.gameObject.SetActive(show);
    }
}