using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NumberGuessingGame : MonoBehaviour, ICooldownProvider
{
    [Header("UI Windows & Toggle")]
    [SerializeField] private Button iconButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject menuButtonPanel;

    [Header("Minigame Elements")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button guessButton;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI attemptsText;

    [Header("Result Card")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultDetailText;
    [SerializeField] private TMP_Text resultCooldownText;
    [SerializeField] private Button resultHomeButton;

    [Header("Reward Settings")]
    [SerializeField] private int winRewardMoney = 500;

    [Header("Game Settings")]
    [SerializeField] private int minNumber = 1;
    [SerializeField] private int maxNumber = 100;
    [SerializeField] private int maxAttempts = 7;

    [Tooltip("จำนวนวินาทีคูลดาวน์ตามเวลาในเกม (เช่น 60 = 1 นาทีในเกม)")]
    [SerializeField] private int cooldownInGameSeconds = 60;
    [Tooltip("ความหิวที่ลดลงทุกครั้งที่จบรอบ (ชนะหรือแพ้)")]
    [SerializeField] private float hungerCostPerRound = 3f;
    private int lastPaidReward; // เงินที่จ่ายจริงรอบล่าสุด (หลังหักผลจากความหิว)

    [Header("Tutorial")]
    [SerializeField] private HowToPlayOverlay howToPlay;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;   
    [SerializeField] private AudioClip guessSound;      
    [SerializeField, Range(0f, 1f)] private float guessVolume = 1f;

    private int targetNumber;
    private int currentAttempts;
    private bool isGameOver = false;

    private int targetTotalInGameSeconds = -1; // วินาทีรวมในวันนั้นที่จะหมด Cooldown
    private bool isCooldownActive = false;
    private Coroutine cooldownCoroutine;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        guessButton.onClick.AddListener(OnGuessSubmitted);
        if (resultHomeButton != null) resultHomeButton.onClick.AddListener(CloseGamePanel);
        inputField.onSubmit.AddListener(delegate { OnGuessSubmitted(); });

        if (iconButton != null)
        {
            iconButton.onClick.AddListener(OpenGamePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGamePanel);
        }
}

    private void Start()
    {
        if (gamePanel != null)
        {
            gamePanel.SetActive(false);
        }
    }

    public void OpenGamePanel()
    {
        if (gamePanel != null)
        {
            gamePanel.SetActive(true);

            if (menuButtonPanel != null)
            {
                menuButtonPanel.SetActive(false);
            }

            int currentInGameSeconds = GetCurrentInGameSeconds();

            if (IsCoolingDown)
            {
                LockUIForCooldown();
                if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
                cooldownCoroutine = StartCoroutine(InGameCooldownRoutine());
            }
            else
            {
                isCooldownActive = false;
                if (howToPlay != null && howToPlay.ShowIfFirstTime(HowToPlayText(), StartNewGame)) return;
                StartNewGame();
            }
        }
    }

    public void CloseGamePanel()
    {
        if (gamePanel != null)
        {
            gamePanel.SetActive(false);

            if (menuButtonPanel != null)
            {
                menuButtonPanel.SetActive(true);
            }
        }
    }

    public void StartNewGame()
    {
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
        }

        isCooldownActive = false;
        targetNumber = UnityEngine.Random.Range(minNumber, maxNumber + 1);
        currentAttempts = maxAttempts;
        isGameOver = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        feedbackText.text = "ทายเลขลับระหว่าง 1 - 100!";
        attemptsText.text = $"เหลือโอกาสทาย: {currentAttempts} ครั้ง";
        inputField.text = "";
        inputField.interactable = true;
        guessButton.interactable = true;

        inputField.ActivateInputField();
    }

    private void OnGuessSubmitted()
    {
        if (isGameOver) return;

        PlayGuessSound();

        if (int.TryParse(inputField.text, out int userGuess))
        {
            ProcessGuess(userGuess);
        }
        else
        {
            feedbackText.text = "กรุณากรอกเฉพาะตัวเลขเท่านั้น!";
        }

        inputField.text = "";
        inputField.ActivateInputField();
    }

    private void PlayGuessSound()
    {
        if (guessSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(guessSound, guessVolume);
        }
    }

    private void ProcessGuess(int guess)
    {
        currentAttempts--;

        if (guess == targetNumber)
        {
            lastPaidReward = HungerSystem.Instance != null ? HungerSystem.Instance.AdjustIncome(winRewardMoney) : winRewardMoney;
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddMoney(lastPaidReward);
            }

            feedbackText.text = $"ถูกต้อง! ได้รับเงิน +{lastPaidReward} (เลขลับคือ {targetNumber})";
            EndGame(true);
        }
        else if (currentAttempts <= 0)
        {
            feedbackText.text = $"คุณแพ้! เลขลับคือ {targetNumber}";
            EndGame(false);
        }
        else if (guess > targetNumber)
        {
            feedbackText.text = "มากไป!";
            attemptsText.text = $"เหลือโอกาสทาย: {currentAttempts} ครั้ง";
        }
        else if (guess < targetNumber)
        {
            feedbackText.text = "น้อยไป!";
            attemptsText.text = $"เหลือโอกาสทาย: {currentAttempts} ครั้ง";
        }
    }

    private void EndGame(bool isWin)
    {
        isGameOver = true;
        isCooldownActive = true;
        if (HungerSystem.Instance != null) HungerSystem.Instance.Drain(hungerCostPerRound);

        targetTotalInGameSeconds = GetCurrentInGameSeconds() + cooldownInGameSeconds;
        cooldownStartDay = GameClock.Instance != null ? GameClock.Instance.Day : 0;

        lastResultWin = isWin;
        lastResultTitle = isWin ? "ถูกต้อง!" : "คุณแพ้!";
        lastResultDetail = isWin ? $"ได้รับเงิน +{lastPaidReward}" : $"เลขลับคือ {targetNumber}";

        LockUIForCooldown();

        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(InGameCooldownRoutine());
    }

    private void LockUIForCooldown()
    {
        inputField.interactable = false;
        ShowResultCard();
        guessButton.interactable = false;
    }

    private bool lastResultWin;
    private string lastResultTitle = "";
    private string lastResultDetail = "";

    private void ShowResultCard()
    {
        if (resultPanel == null) return;
        resultPanel.SetActive(true);

        if (resultTitleText != null)
        {
            resultTitleText.text = lastResultTitle;
            resultTitleText.color = lastResultWin ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(1f, 0.5f, 0.45f, 1f);
        }
        if (resultDetailText != null)
        {
            resultDetailText.text = lastResultDetail;
            resultDetailText.color = lastResultWin ? new Color(0.55f, 1f, 0.6f, 1f) : Color.white;
        }
    }

    public void ResetForNewDay()
    {
        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = null;
        isCooldownActive = false;
        targetTotalInGameSeconds = -1;

        if (gamePanel != null) gamePanel.SetActive(false);
        if (menuButtonPanel != null) menuButtonPanel.SetActive(true);
    }

    private int cooldownStartDay = -1;

    // ใช้โดย CooldownBadge / CooldownWatcher: คูลดาวน์ข้ามวันถือว่าหมดแล้ว (ตรงกับที่ตั้งใจไว้ใน InGameCooldownRoutine)
    public bool IsCoolingDown => isCooldownActive
        && (GameClock.Instance == null || GameClock.Instance.Day == cooldownStartDay)
        && targetTotalInGameSeconds > GetCurrentInGameSeconds();
    public int CooldownRemainingGameSeconds => IsCoolingDown ? targetTotalInGameSeconds - GetCurrentInGameSeconds() : 0;

    private string HowToPlayText()
    {
        return "<size=130%><b>เกมทายเลข</b></size>\n\n" +
               $"- คอมสุ่มเลขลับไว้ 1 ตัว ระหว่าง {minNumber} ถึง {maxNumber}\n" +
               "- พิมพ์เลขที่คิดว่าใช่ แล้วกด Enter หรือกดปุ่มทาย\n" +
               "- ระบบจะบอกว่า 'มากไป' หรือ 'น้อยไป' ให้ใช้เป็นคำใบ้\n" +
               $"- ทายได้ {maxAttempts} ครั้ง ถ้าทายถูกจะได้เงิน +{winRewardMoney}\n" +
               "- เล่นจบแล้วต้องรอคูลดาวน์ก่อนเล่นใหม่\n\n" +
               "เคล็ดลับ: เริ่มจากเลขตรงกลาง แล้วแบ่งครึ่งช่วงที่เหลือไปเรื่อยๆ";
    }

    private int GetCurrentInGameSeconds()
    {
        if (GameClock.Instance != null)
        {
            // คำนวณเฉพาะชั่วโมงและนาทีภายในวัน
            return (GameClock.Instance.Hour * 3600) + (GameClock.Instance.Minute * 60);
        }

        return (int)Time.time;
    }

    private IEnumerator InGameCooldownRoutine()
    {
        int currentInGameSeconds = GetCurrentInGameSeconds();
        int lastInGameSeconds = currentInGameSeconds;

        while (isCooldownActive && targetTotalInGameSeconds > currentInGameSeconds)
        {
            // 💡 ดักจับการข้ามวัน: ถ้าเวลาปัจจุบันน้อยกว่าเวลารอบที่แล้ว แสดงว่ามีการกดข้ามวัน ให้หลุดคูลดาวน์ทันที
            if (currentInGameSeconds < lastInGameSeconds)
            {
                break;
            }

            lastInGameSeconds = currentInGameSeconds;

            int targetHour = (targetTotalInGameSeconds / 3600) % 24;
            int targetMinute = (targetTotalInGameSeconds % 3600) / 60;
            int targetSecond = targetTotalInGameSeconds % 60;

            string targetTimeString = $"{targetHour:D2}:{targetMinute:D2}:{targetSecond:D2}";
            attemptsText.text = $"ติดคูลดาวน์\nเล่นได้อีกครั้งเวลา {targetTimeString.Substring(0, 5)} น.";
            if (resultCooldownText != null) resultCooldownText.text = attemptsText.text;

            yield return new WaitForSeconds(0.2f);
            currentInGameSeconds = GetCurrentInGameSeconds();
        }

        StartNewGame();
    }
}