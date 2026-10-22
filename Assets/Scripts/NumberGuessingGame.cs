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
    [SerializeField] private int cooldownInGameMinutes = 1; // คูลดาวน์กี่นาทีในเกม

    private int targetNumber;
    private int currentAttempts;
    private bool isGameOver = false;
    
    private int targetHour = -1;
    private int targetMinute = -1;
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

            // ถ้าอยู่ในระหว่าง Cooldown ให้แสดงผลนับเวลาต่อ
            if (isCooldownActive && IsStillInCooldown())
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
            if (cooldownCoroutine != null)
            {
                StopCoroutine(cooldownCoroutine);
            }

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

            feedbackText.text = $" ถูกต้อง! ได้รับเงิน +{winRewardMoney} (เลขลับคือ {targetNumber})";
            EndGame(true);
        }
        else if (currentAttempts <= 0)
        {
            feedbackText.text = $" คุณแพ้! เลขลับคือ {targetNumber}";
            EndGame(false);
        }
        else if (guess > targetNumber)
        {
            feedbackText.text = " มากไป!";
            attemptsText.text = $"เหลือโอกาสทาย: {currentAttempts} ครั้ง";
        }
        else if (guess < targetNumber)
        {
            feedbackText.text = " น้อยไป!";
            attemptsText.text = $"เหลือโอกาสทาย: {currentAttempts} ครั้ง";
        }
    }

    private void EndGame(bool isWin)
    {
        isGameOver = true;
        isCooldownActive = true;

        // คำนวณเวลาที่จะหมดคูลดาวน์ (เวลาในเกมปัจจุบัน + คูลดาวน์)
        CalculateTargetTime();
        
        LockUIForCooldown();

        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(InGameCooldownRoutine());
    }

    private void LockUIForCooldown()
    {
        inputField.interactable = false;
        guessButton.interactable = false;
    }

    private void CalculateTargetTime()
    {
        int curHour = 0;
        int curMin = 0;

        if (GameClock.Instance != null)
        {
            curHour = GameClock.Instance.Hour;
            curMin = GameClock.Instance.Minute;
        }

        int totalMins = (curHour * 60) + curMin + cooldownInGameMinutes;
        targetHour = (totalMins / 60) % 24;
        targetMinute = totalMins % 60;
    }

    private bool IsStillInCooldown()
    {
        if (GameClock.Instance == null || targetHour == -1) return false;

        int curTotal = (GameClock.Instance.Hour * 60) + GameClock.Instance.Minute;
        int targetTotal = (targetHour * 60) + targetMinute;

        // รองรับกรณีเวลาข้ามเที่ยงคืน (เช่น 23:59 ไป 00:01)
        if (targetTotal < curTotal && (curTotal - targetTotal) > 720) 
        {
            targetTotal += 1440; // เพิ่ม 24 ชั่วโมง
        }

        return curTotal < targetTotal;
    }

    private IEnumerator InGameCooldownRoutine()
    {
        while (IsStillInCooldown())
        {
            string targetTimeString = $"{targetHour:D2}:{targetMinute:D2}";
            attemptsText.text = $" ติด Cooldown... รอบต่อไปจะเริ่มเวลาในเกม: {targetTimeString}";
            
            yield return new WaitForSeconds(0.2f);
        }

        StartNewGame();
    }
}