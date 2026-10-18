using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// สายเข้าแบบสุ่มระหว่างวันทำงาน โผล่เป็นป๊อปอัปมุมขวาล่าง คลิกแล้วโทรศัพท์เลื่อนขึ้นมาให้รับ/วางสาย
/// มินิเกมบนคอมไม่หยุดระหว่างมีสาย วันจบเมื่อ GameClock ถึงเวลาเลิกงาน แล้ววนเข้าวันถัดไปอัตโนมัติ
/// </summary>
public class CallCenterManager : MonoBehaviour
{
    public static CallCenterManager Instance { get; private set; }

    [Header("Calls (สุ่มเลือกจากลิสต์นี้)")]
    [SerializeField] private List<CallData> calls = new List<CallData>();
    [Tooltip("หลังจากคุยสายนี้จบ (หรือไม่รับสาย) แล้ว ต้องรอกี่วันถึงจะโทรมาซ้ำได้")]
    [SerializeField] private int repeatCooldownDays = 1;

    // จำวันล่าสุดที่สายนี้ถูก "จบ" ไปแล้ว (คุยจบ/กดไม่รับ/ปล่อยขาดสาย) ใช้กันโทรซ้ำถี่เกินไป
    private readonly Dictionary<CallData, int> lastResolvedDay = new Dictionary<CallData, int>();
    [Tooltip("ช่วงเวลาจริง (วินาที) ระหว่างที่วางสาย/จบสายก่อนสายถัดไปจะเริ่มนับ")]
    [SerializeField] private Vector2 delayBetweenCalls = new Vector2(8f, 20f);

    [Header("Ringing")]
    [Tooltip("ช่วงเวลาที่สายดังรอก่อนวางสายไปเอง (วินาที) สุ่มใหม่ทุกครั้งที่มีสายเข้า ตั้ง X เป็น 0 = ดังรอไปเรื่อยๆ ไม่มีขาดสาย")]
    [SerializeField] private Vector2 ringTimeoutRange = new Vector2(5f, 10f);
    [SerializeField] private string answerLabel = "รับสาย";
    [SerializeField] private string hangUpLabel = "วางสาย";

    [Header("UI - Incoming Popup (ป๊อปอัปมุมขวาล่าง คลิกแล้วโทรศัพท์เลื่อนขึ้นมา)")]
    [Tooltip("Object ราก ของป๊อปอัป (ปกติคือตัวเดียวกับปุ่ม) ถ้าไม่ใส่ โทรศัพท์จะเด้งขึ้นมาเลยแบบเดิม")]
    [SerializeField] private GameObject incomingPopup;
    [SerializeField] private Button popupButton;
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private float phoneSlideDuration = 0.35f;
    [SerializeField] private float missedToastSeconds = 1.5f;
    [Tooltip("หลังคุยจบ: เวลา (วินาที) ที่รอให้ข้อความหายไปก่อน แล้วโทรศัพท์ค่อยเลื่อนลง")]
    [SerializeField] private float dialogueHideDelay = 0.2f;

    [Header("UI - Call Panel (โทรศัพท์เต็มเครื่อง ตำแหน่งที่วางใน Scene = ตำแหน่งตอนเลื่อนขึ้นมาสุด)")]
    [SerializeField] private GameObject callPanel;
    [SerializeField] private TMP_Text callerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image portraitImage;

    [Header("UI - Buttons (ใช้ 2 ปุ่มนี้ทั้งตอนสายเข้าและตอนตัดสินใจ)")]
    [SerializeField] private Button acceptButton;   // ปุ่มซ้าย: รับสาย / ตกลง
    [SerializeField] private Button declineButton;  // ปุ่มขวา: วางสาย / ปฏิเสธ
    [SerializeField] private Button continueButton;

    [Header("Typewriter")]
    [SerializeField] private float charsPerSecond = 40f;

    [Header("Audio (optional)")]
    [Tooltip("ถ้าอยากให้เสียงดังวนซ้ำ ให้ติ๊ก Loop ที่ AudioSource")]
    [SerializeField] private AudioSource ringtone;

    [Header("Other Systems")]
    [SerializeField] private HungerSystem hunger;
    [Tooltip("ตอนจบวันจะปิดมินิเกม/แผงจ่ายหนี้/คอม ให้เหมือนผู้เล่นเปิดคอมใหม่ ไม่ใส่ก็ได้")]
    [SerializeField] private SpaceMonneyManager spaceMonney;
    [SerializeField] private DebtPaymentUI debtPayment;
    [Tooltip("ร้านค้าในคอม จบวันจะปิดแผงให้ ไม่ใส่ก็ได้")]
    [SerializeField] private ShopUI shop;
    [SerializeField] private RoomManager room;

    [Header("UI - Day Result (โผล่หลังจบวัน)")]
    [Tooltip("UI อื่นๆ ทั้งหมดที่ไม่ใช่แผงสรุปผล (เช่น ห้อง/เตียง/คอม/ปุ่มจ่ายหนี้/Stats Text) " +
             "รวมไว้เป็นลูกของ Object เดียวแล้วลากใส่ตรงนี้ จะถูกซ่อนตอนโชว์แผงสรุปผล แล้วเปิดกลับมาเองหลังกดวันถัดไป")]
    [SerializeField] private GameObject gameplayUIRoot;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultBodyText;
    [SerializeField] private Button nextDayButton;
    [SerializeField] private Button resultBackToMenuButton;
    [Tooltip("ชื่อซีน Main Menu (ต้องตรงกับชื่อไฟล์ซีนเป๊ะๆ และถูกเพิ่มใน Build Settings แล้ว)")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private TMP_Text acceptLabelText;
    private TMP_Text declineLabelText;

    private bool choiceMade;
    private bool accepted;
    private bool continuePressed;
    private bool isTyping;
    private bool skipTyping;
    private bool nextDayPressed;
    private bool popupClicked;
    private bool phoneOpened;

    private RectTransform phoneRect;
    private Vector2 phoneShownPos;
    private Vector2 phoneHiddenPos;
    private bool phonePosReady;

    // สถิติของวันปัจจุบัน ใช้สร้างหน้าสรุปผลตอนจบวัน
    private int moneyAtDayStart;
    private int callsAnsweredToday;
    private int callsMissedToday;
    private int jobsAcceptedToday;
    private int jobsDeclinedToday;

    private Coroutine runGameCoroutine;

    private void Awake()
    {
        Instance = this;

        acceptLabelText = acceptButton.GetComponentInChildren<TMP_Text>(true);
        declineLabelText = declineButton.GetComponentInChildren<TMP_Text>(true);

        acceptButton.onClick.AddListener(() => { accepted = true; choiceMade = true; });
        declineButton.onClick.AddListener(() => { accepted = false; choiceMade = true; });
        continueButton.onClick.AddListener(OnContinueClicked);

        if (popupButton != null)
            popupButton.onClick.AddListener(() => popupClicked = true);

        if (nextDayButton != null)
            nextDayButton.onClick.AddListener(() => nextDayPressed = true);

        if (resultBackToMenuButton != null)
            resultBackToMenuButton.onClick.AddListener(BackToMainMenu);
    }

    private void Start()
    {
        EnsurePhonePositions();
        HidePopup();
        callPanel.SetActive(false);
        SetChoiceButtons(false);
        continueButton.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        RestoreCooldowns();

        // New Game: เขียนเซฟวันที่ 1 ทันที เผื่อผู้เล่นออกกลางวันแรกแล้วกด Continue
        if (SaveSystem.NewGameRequested)
        {
            SaveGame(1);
            SaveSystem.NewGameRequested = false;
        }

        runGameCoroutine = StartCoroutine(RunGame());
    }

    // ---------------- Save / Load ----------------

    /// เซฟสถานะ "ต้นวัน" ของวัน dayToSave (Continue จะกลับมาเริ่มวันนั้นใหม่ตั้งแต่ต้น)
    private void SaveGame(int dayToSave)
    {
        HungerSystem h = hunger != null ? hunger : HungerSystem.Instance;

        SaveData data = new SaveData
        {
            day = dayToSave,
            money = PlayerStats.Instance != null ? PlayerStats.Instance.Money : 0,
            debt = PlayerStats.Instance != null ? PlayerStats.Instance.Debt : 0,
            hunger = h != null ? h.Current : 100f
        };

        foreach (KeyValuePair<CallData, int> kv in lastResolvedDay)
        {
            if (kv.Key == null) continue;
            data.callCooldowns.Add(new CallCooldownEntry
            {
                callName = kv.Key.name,
                lastResolvedDay = kv.Value
            });
        }

        SaveSystem.Save(data);
    }

    /// คืนค่า cooldown ของสายจากเซฟ (จับคู่ด้วยชื่อไฟล์ CallData)
    private void RestoreCooldowns()
    {
        SaveData save = SaveSystem.Loaded;
        if (save == null) return;

        foreach (CallCooldownEntry entry in save.callCooldowns)
        {
            CallData match = calls.Find(c => c != null && c.name == entry.callName);
            if (match != null) lastResolvedDay[match] = entry.lastResolvedDay;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// เรียกจากปุ่มนอน: นอนเป็นใหญ่สุด ตัดทุกอย่างทิ้งทันที ไม่ว่าจะมีสายดังอยู่ หรือกำลังติด dialogue อยู่ตรงไหนก็ตาม
    /// แล้วข้ามไปหน้าสรุปผลจบวันเลย สายที่ค้างอยู่ตอนนั้นจะถูกยกเลิกไปเฉยๆ (ไม่นับเป็นรับ/ไม่รับ/พลาด)
    public void ForceSleepNow()
    {
        // หยุด coroutine หลักทั้งสาย (RunGame -> PlayOneDay -> HandleCall -> RingPhase/TalkPhase/...)
        // ทุกเมธอดข้างบนรันอยู่ใน Unity Coroutine เดียวกัน (เชื่อมด้วย yield return) จึงหยุดทีเดียวจบ
        if (runGameCoroutine != null) StopCoroutine(runGameCoroutine);

        // เคลียร์ UI ของสาย/บทสนทนาที่ค้างอยู่ทั้งหมด
        if (ringtone != null) ringtone.Stop();
        callPanel.SetActive(false);
        if (phoneRect != null) phoneRect.anchoredPosition = phoneShownPos;
        HidePopup();
        popupClicked = false;
        phoneOpened = false;
        SetChoiceButtons(false);
        continueButton.gameObject.SetActive(false);
        isTyping = false;
        skipTyping = false;
        choiceMade = false;

        GameClock.Instance?.ForceEndDay();

        runGameCoroutine = StartCoroutine(ForceSleepRoutine());
    }

    private IEnumerator ForceSleepRoutine()
    {
        yield return EndDay();

        if (GameClock.Instance != null)
            GameClock.Instance.StartNextDay();

        runGameCoroutine = StartCoroutine(RunGame());
    }

    // ---------------- Day / Game loop ----------------

    /// วนไปเรื่อยๆ ทีละวัน: เล่นมินิเกม+รับสายจนหมดเวลา → สรุปวัน → เริ่มวันใหม่
    private IEnumerator RunGame()
    {
        while (true)
        {
            yield return PlayOneDay();

            if (GameClock.Instance != null)
                GameClock.Instance.StartNextDay();
        }
    }

    private IEnumerator PlayOneDay()
    {
        moneyAtDayStart = PlayerStats.Instance != null ? PlayerStats.Instance.Money : 0;
        callsAnsweredToday = 0;
        callsMissedToday = 0;
        jobsAcceptedToday = 0;
        jobsDeclinedToday = 0;

        while (GameClock.Instance == null || !GameClock.Instance.IsDayEnded)
        {
            float wait = Random.Range(delayBetweenCalls.x, delayBetweenCalls.y);
            float t = 0f;
            while (t < wait)
            {
                if (GameClock.Instance != null && GameClock.Instance.IsDayEnded) break;
                t += Time.deltaTime;
                yield return null;
            }

            if (GameClock.Instance != null && GameClock.Instance.IsDayEnded) break;

            CallData call = PickRandomEligibleCall();
            if (call == null) continue; // ยังไม่มีสายไหนเข้าเงื่อนไขวันนี้ รอสุ่มรอบถัดไป

            yield return HandleCall(call);
        }

        yield return EndDay();
    }

    /// สายเข้า 1 สาย: ป๊อปอัป → รับ/วาง → (ถ้ารับ) คุยจนจบ → เลือก → ผลลัพธ์ → โทรศัพท์เลื่อนลง (มินิเกมเล่นต่อได้ตลอด)
    private IEnumerator HandleCall(CallData call)
    {
        yield return RingPhase(call);

        if (!accepted)
        {
            callsMissedToday++;
            PlayerStats.Instance?.AddMoney(call.hangUpMoney);

            if (phoneOpened)
                yield return ShowResult(call.hangUpResult, call.hangUpMoney);
            else
                yield return ShowMissedToast(call); // ไม่ได้คลิกป๊อปอัปเลย แค่แจ้งสั้นๆ ไม่ดึงผู้เล่นเข้าหน้าโทรศัพท์
        }
        else
        {
            callsAnsweredToday++;
            yield return TalkPhase(call);

            SetButtonLabels(
                string.IsNullOrEmpty(call.acceptLabel) ? "รับงาน" : call.acceptLabel,
                string.IsNullOrEmpty(call.declineLabel) ? "ไม่รับ" : call.declineLabel);

            choiceMade = false;
            SetChoiceButtons(true);
            yield return new WaitUntil(() => choiceMade);
            SetChoiceButtons(false);
            hunger?.OnWorkCompleted(); // ความหิวลดตอนนี้ คือ "ทำงานเสร็จ 1 สาย" ไม่ใช่ลดตามเวลาแล้ว

            if (accepted)
            {
                jobsAcceptedToday++;
                PlayerStats.Instance?.AddMoney(call.acceptMoney);
                yield return ShowResult(call.acceptResult, call.acceptMoney);
            }
            else
            {
                jobsDeclinedToday++;
                PlayerStats.Instance?.AddMoney(call.declineMoney);
                yield return ShowResult(call.declineResult, call.declineMoney);
            }
        }

        if (phoneOpened)
        {
            // dialog หายไปก่อน แล้วโทรศัพท์ค่อยเลื่อนลงตาม
            SetDialogueVisible(false);
            continueButton.gameObject.SetActive(false);
            SetChoiceButtons(false);
            if (dialogueHideDelay > 0f) yield return new WaitForSeconds(dialogueHideDelay);

            yield return SlidePhone(false);
            phoneOpened = false;
        }
        callPanel.SetActive(false);

        // ไม่ว่าจบแบบไหน (คุยจบ/ไม่รับ/ขาดสาย) ให้จำวันไว้กันโทรซ้ำถี่เกินไป
        int today = GameClock.Instance != null ? GameClock.Instance.Day : 1;
        lastResolvedDay[call] = today;
    }

    /// สายดัง: โชว์ป๊อปอัปมุมขวาล่าง → ผู้เล่นคลิก → โทรศัพท์เลื่อนขึ้น → รับสาย/วางสาย
    /// ถ้าไม่คลิกจนหมดเวลา สายจะขาดไปเอง (accepted = false, phoneOpened = false)
    private IEnumerator RingPhase(CallData call)
    {
        choiceMade = false;
        accepted = false;
        popupClicked = false;
        phoneOpened = false;

        callerNameText.text = call.callerName;
        SetPortrait(call.callerPortrait);
        dialogueText.maxVisibleCharacters = int.MaxValue;
        dialogueText.text = "";
        SetDialogueVisible(false); // ตอนสายดัง/เพิ่งหยิบโทรศัพท์ ไม่มี dialog แค่ปุ่มรับสาย-วางสาย
        continueButton.gameObject.SetActive(false);
        SetChoiceButtons(false);
        SetButtonLabels(answerLabel, hangUpLabel);

        if (ringtone != null) ringtone.Play();

        float timeout = ringTimeoutRange.x > 0f
            ? Random.Range(ringTimeoutRange.x, Mathf.Max(ringTimeoutRange.x, ringTimeoutRange.y))
            : 0f;

        if (incomingPopup == null || popupButton == null)
        {
            // ไม่ได้ต่อ UI ป๊อปอัป: เปิดโทรศัพท์เลยแบบเดิม
            popupClicked = true;
        }
        else
        {
            if (popupText != null) popupText.text = $"สายเข้า: {call.callerName}";
            popupButton.interactable = true;
            incomingPopup.SetActive(true);
            yield return PopIn(incomingPopup.transform);
        }

        float startTime = Time.time;
        while (!choiceMade)
        {
            if (!phoneOpened)
            {
                if (popupClicked)
                {
                    HidePopup();
                    phoneOpened = true;
                    yield return SlidePhone(true);
                    SetChoiceButtons(true);
                    continue;
                }

                if (timeout > 0f && Time.time - startTime >= timeout)
                {
                    accepted = false; // ไม่ได้รับสายทันเวลา ถือว่าวางสายไปเอง
                    break;
                }

                if (incomingPopup != null)
                {
                    float pulse = 1f + 0.05f * Mathf.Sin((Time.time - startTime) * 10f);
                    incomingPopup.transform.localScale = Vector3.one * pulse;
                }
            }

            yield return null;
        }

        if (ringtone != null) ringtone.Stop();
        HidePopup();
        SetChoiceButtons(false);
    }

    // ---------------- Popup / Phone animation ----------------

    private void HidePopup()
    {
        if (incomingPopup == null) return;
        incomingPopup.transform.localScale = Vector3.one;
        incomingPopup.SetActive(false);
    }

    private IEnumerator PopIn(Transform t)
    {
        const float duration = 0.25f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float x = Mathf.Clamp01(time / duration) - 1f;
            float ease = 1f + 2.70158f * x * x * x + 1.70158f * x * x; // EaseOutBack
            t.localScale = Vector3.one * ease;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private IEnumerator ShowMissedToast(CallData call)
    {
        if (incomingPopup == null || popupText == null) yield break;

        popupText.text = $"พลาดสายจาก {call.callerName}";
        popupButton.interactable = false;
        incomingPopup.transform.localScale = Vector3.one;
        incomingPopup.SetActive(true);

        float t = 0f;
        while (t < missedToastSeconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        popupButton.interactable = true;
        HidePopup();
    }

    private void EnsurePhonePositions()
    {
        if (phonePosReady) return;
        phoneRect = callPanel.GetComponent<RectTransform>();
        phoneShownPos = phoneRect.anchoredPosition;

        Canvas canvas = callPanel.GetComponentInParent<Canvas>(true);
        float canvasH = canvas != null ? ((RectTransform)canvas.rootCanvas.transform).rect.height : 1080f;
        phoneHiddenPos = phoneShownPos + Vector2.down * (canvasH + phoneRect.rect.height);
        phonePosReady = true;
    }

    /// เลื่อนโทรศัพท์ขึ้น (show) / ลง (hide) ด้วย SmoothStep
    private IEnumerator SlidePhone(bool show)
    {
        EnsurePhonePositions();

        Vector2 from = show ? phoneHiddenPos : phoneShownPos;
        Vector2 to = show ? phoneShownPos : phoneHiddenPos;

        phoneRect.anchoredPosition = from;
        callPanel.SetActive(true);

        float t = 0f;
        float dur = Mathf.Max(0.01f, phoneSlideDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            phoneRect.anchoredPosition = Vector2.LerpUnclamped(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        phoneRect.anchoredPosition = to;

        if (!show) callPanel.SetActive(false);
    }

    /// จบวัน/นอน: ปิดมินิเกม แผงจ่ายหนี้ และหน้าคอม ให้เหมือนผู้เล่นเปิดคอมขึ้นมาใหม่ตอนเช้า
    private void ResetComputerForNewDay()
    {
        if (spaceMonney != null) spaceMonney.ResetForNewDay();
        if (debtPayment != null) debtPayment.ClosePanel();
        if (shop != null) shop.ClosePanel();
        if (room != null) room.CloseComputer();
    }

    private IEnumerator TalkPhase(CallData call)
    {
        string[] lines = call.dialogueLines;

        if (lines == null || lines.Length == 0)
        {
            yield return TypeLine("(ไม่มีเสียงตอบกลับ...)");
            yield break;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            bool isLast = i == lines.Length - 1;

            if (!isLast)
            {
                yield return ShowLineAndWait(lines[i]);
            }
            else
            {
                // ประโยคสุดท้าย: พิมพ์จบแล้วขึ้นปุ่มตัดสินใจเลย
                continueButton.gameObject.SetActive(true);
                yield return TypeLine(lines[i]);
                continueButton.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator ShowResult(string message, int moneyChange)
    {
        string summary = $"\n\n<size=70%>เงิน {Signed(moneyChange)}</size>";
        yield return ShowLineAndWait(message + summary);
    }

    private IEnumerator EndDay()
    {
        callPanel.SetActive(false); // ปิดแผงสาย ใช้แผงสรุปผลแยกต่างหากแทน
        if (phoneRect != null) phoneRect.anchoredPosition = phoneShownPos;
        HidePopup();

        hunger?.RefillFull(); // นอนเต็มอิ่มทุกครั้งที่จบวัน ไม่ว่าจบเพราะหมดเวลาหรือกดนอนเอง
        ResetComputerForNewDay(); // ปิดมินิเกม/คอม เหมือนเปิดคอมใหม่ในวันถัดไป

        int totalMoney = PlayerStats.Instance != null ? PlayerStats.Instance.Money : 0;
        int moneyToday = totalMoney - moneyAtDayStart;
        int day = GameClock.Instance != null ? GameClock.Instance.Day : 1;

        // เซฟอัตโนมัติตอนจบวัน: เก็บสถานะต้นวันถัดไป (ถ้าออกระหว่างดูสรุปผล Continue จะเริ่มวันถัดไปเลย)
        SaveGame(day + 1);

        string body =
            $"เงินที่ได้วันนี้: {Signed(moneyToday)}\n" +
            $"เงินรวมทั้งหมด: {totalMoney}\n\n" +
            $"รับสาย: {callsAnsweredToday} สาย\n" +
            $"พลาด/วางสาย: {callsMissedToday} สาย\n\n" +
            $"ตกลงรับงาน: {jobsAcceptedToday} ครั้ง\n" +
            $"ปฏิเสธงาน: {jobsDeclinedToday} ครั้ง";

        if (resultPanel != null)
        {
            if (gameplayUIRoot != null) gameplayUIRoot.SetActive(false); // เหลือแค่แผงสรุปผลบนจอ
            resultPanel.SetActive(true);
            if (resultTitleText != null) resultTitleText.text = $"สรุปผลวันที่ {day}";
            if (resultBodyText != null) resultBodyText.text = body;

            nextDayPressed = false;
            yield return new WaitUntil(() => nextDayPressed);

            resultPanel.SetActive(false);
            if (gameplayUIRoot != null) gameplayUIRoot.SetActive(true); // กลับมาเล่นต่อตามปกติ
        }
        else
        {
            // เผื่อยังไม่ได้ต่อ UI แผงสรุปผล ใช้แผงสายเดิมแสดงแทนไปก่อน
            callPanel.SetActive(true);
            callerNameText.text = $"สรุปผลวันที่ {day}";
            SetPortrait(null);
            SetChoiceButtons(false);
            yield return ShowLineAndWait(body);
            callPanel.SetActive(false);
        }
    }

    /// เรียกจากปุ่ม "กลับเมนูหลัก" ในหน้าสรุปผล
    private void BackToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// สุ่มเลือกสายที่ "เข้าเงื่อนไขวันที่" ในตอนนี้ ถ้าไม่มีสายไหนเข้าเงื่อนไขจะได้ null
    private CallData PickRandomEligibleCall()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.Day : 1;
        int money = PlayerStats.Instance != null ? PlayerStats.Instance.Money : 0;

        List<CallData> eligible = new List<CallData>();
        foreach (CallData c in calls)
        {
            if (c == null) continue;
            if (day < c.minDay) continue;
            if (c.maxDay > 0 && day > c.maxDay) continue;
            if (money < c.minMoney) continue; // เงินสดยังไม่ถึงเกณฑ์ของสายนี้

            if (lastResolvedDay.TryGetValue(c, out int resolvedDay) &&
                day - resolvedDay < repeatCooldownDays)
                continue; // ยังอยู่ในช่วงพัก ไม่ให้โทรมาซ้ำ

            eligible.Add(c);
        }

        if (eligible.Count == 0) return null;
        return eligible[Random.Range(0, eligible.Count)];
    }

    // ---------------- Dialogue helpers ----------------

    /// พิมพ์ข้อความ แล้วรอให้ผู้เล่นกดต่อ (กดระหว่างพิมพ์ = ข้ามไปแสดงทั้งประโยค)
    private IEnumerator ShowLineAndWait(string line)
    {
        continuePressed = false;
        continueButton.gameObject.SetActive(true);
        yield return TypeLine(line);
        yield return new WaitUntil(() => continuePressed);
        continueButton.gameObject.SetActive(false);
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        skipTyping = false;
        SetDialogueVisible(true);
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int total = dialogueText.textInfo.characterCount;
        float delay = 1f / charsPerSecond;

        for (int i = 0; i <= total; i++)
        {
            if (skipTyping) break;
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }

    private void OnContinueClicked()
    {
        if (isTyping) { skipTyping = true; return; }
        continuePressed = true;
    }

    // ---------------- Misc ----------------

    private void SetButtonLabels(string acceptText, string declineText)
    {
        if (acceptLabelText != null) acceptLabelText.text = acceptText;
        if (declineLabelText != null) declineLabelText.text = declineText;
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialogueText != null) dialogueText.gameObject.SetActive(visible);
    }

    private void SetChoiceButtons(bool active)
    {
        acceptButton.gameObject.SetActive(active);
        declineButton.gameObject.SetActive(active);
    }

    private void SetPortrait(Sprite sprite)
    {
        if (portraitImage == null) return;
        portraitImage.gameObject.SetActive(sprite != null);
        portraitImage.sprite = sprite;
    }

    private static string Signed(int v) => v > 0 ? $"+{v}" : v.ToString();
}
