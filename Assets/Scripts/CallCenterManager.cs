using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// สายเข้าแบบสุ่มระหว่างวันทำงาน (ตัดการเชื่อมกับ WorkMinigame ออกไปก่อน)
/// วันจบเมื่อ GameClock ถึงเวลาเลิกงาน แล้ววนเข้าวันถัดไปอัตโนมัติ
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

    [Header("UI - Call Panel")]
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

        if (nextDayButton != null)
            nextDayButton.onClick.AddListener(() => nextDayPressed = true);

        if (resultBackToMenuButton != null)
            resultBackToMenuButton.onClick.AddListener(BackToMainMenu);
    }

    private void Start()
    {
        callPanel.SetActive(false);
        SetChoiceButtons(false);
        continueButton.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        runGameCoroutine = StartCoroutine(RunGame());
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

    /// สายเข้า 1 สาย: รับ/วาง → (ถ้ารับ) คุยจนจบ → เลือก → ผลลัพธ์ ระหว่างนี้มินิเกมจะพักไว้
    private IEnumerator HandleCall(CallData call)
    {
        yield return RingPhase(call);

        if (!accepted)
        {
            callsMissedToday++;
            PlayerStats.Instance?.AddMoney(call.hangUpMoney);
            yield return ShowResult(call.hangUpResult, call.hangUpMoney);
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

        callPanel.SetActive(false);

        // ไม่ว่าจบแบบไหน (คุยจบ/ไม่รับ/ขาดสาย) ให้จำวันไว้กันโทรซ้ำถี่เกินไป
        int today = GameClock.Instance != null ? GameClock.Instance.Day : 1;
        lastResolvedDay[call] = today;
    }

    /// สายดัง รอผู้เล่นเลือก รับสาย (accepted = true) หรือ วางสาย/หมดเวลา (accepted = false)
    private IEnumerator RingPhase(CallData call)
    {
        choiceMade = false;
        accepted = false;

        callPanel.SetActive(true);
        callerNameText.text = call.callerName;
        SetPortrait(call.callerPortrait);
        dialogueText.maxVisibleCharacters = int.MaxValue;
        dialogueText.text = "สายเข้า...";
        continueButton.gameObject.SetActive(false);

        SetButtonLabels(answerLabel, hangUpLabel);
        SetChoiceButtons(true);

        if (ringtone != null) ringtone.Play();

        float timeout = ringTimeoutRange.x > 0f
            ? Random.Range(ringTimeoutRange.x, Mathf.Max(ringTimeoutRange.x, ringTimeoutRange.y))
            : 0f;

        float waited = 0f;
        while (!choiceMade)
        {
            if (timeout > 0f)
            {
                waited += Time.deltaTime;
                if (waited >= timeout)
                {
                    accepted = false; // ไม่ได้รับสายทันเวลา ถือว่าวางสายไปเอง
                    break;
                }
            }
            yield return null;
        }

        if (ringtone != null) ringtone.Stop();
        SetChoiceButtons(false);
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

        hunger?.RefillFull(); // นอนเต็มอิ่มทุกครั้งที่จบวัน ไม่ว่าจบเพราะหมดเวลาหรือกดนอนเอง

        int totalMoney = PlayerStats.Instance != null ? PlayerStats.Instance.Money : 0;
        int moneyToday = totalMoney - moneyAtDayStart;
        int day = GameClock.Instance != null ? GameClock.Instance.Day : 1;

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

        List<CallData> eligible = new List<CallData>();
        foreach (CallData c in calls)
        {
            if (c == null) continue;
            if (day < c.minDay) continue;
            if (c.maxDay > 0 && day > c.maxDay) continue;

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
