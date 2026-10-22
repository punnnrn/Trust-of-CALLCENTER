using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NumberGuessingGame : MonoBehaviour
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

    [Header("Reward Settings")]
    [SerializeField] private int winRewardMoney = 500;

    [Header("Game Settings")]
    [SerializeField] private int minNumber = 1;
    [SerializeField] private int maxNumber = 100;
    [SerializeField] private int maxAttempts = 7;

    [Tooltip("จำนวนวินาทีคูลดาวน์ตามเวลาในเกม (เช่น 60 = 1 นาทีในเกม)")]
    [SerializeField] private int cooldownInGameSeconds = 60;

    private int targetNumber;
    private int currentAttempts;
    private bool isGameOver = false;

    private int targetTotalInGameSeconds = -1; // วินาทีรวมในวันนั้นที่จะหมด Cooldown
    private bool isCooldownActive = false;
    private Coroutine cooldownCoroutine;

    private void Awake()
    {
        guessButton.onClick.AddListener(OnGuessSubmitted);
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

            if (isCooldownActive && targetTotalInGameSeconds > currentInGameSeconds)
            {
                LockUIForCooldown();
                if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
                cooldownCoroutine = StartCoroutine(InGameCooldownRoutine());
            }
            else
            {
                isCooldownActive = false;
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

    private void ProcessGuess(int guess)
    {
        currentAttempts--;

        if (guess == targetNumber)
        {
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddMoney(winRewardMoney);
            }

            feedbackText.text = $"ถูกต้อง! ได้รับเงิน +{winRewardMoney} (เลขลับคือ {targetNumber})";
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

        targetTotalInGameSeconds = GetCurrentInGameSeconds() + cooldownInGameSeconds;

        LockUIForCooldown();

        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(InGameCooldownRoutine());
    }

    private void LockUIForCooldown()
    {
        inputField.interactable = false;
        guessButton.interactable = false;
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
            attemptsText.text = $"ติด Cooldown... รอบต่อไปจะเริ่มเวลาในเกม: {targetTimeString}";

            yield return new WaitForSeconds(0.2f);
            currentInGameSeconds = GetCurrentInGameSeconds();
        }

        StartNewGame();
    }
}