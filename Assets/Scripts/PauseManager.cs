using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ระบบ Pause: กด P หรือ Esc เพื่อหยุด/เล่นต่อ
/// ใช้ Time.timeScale = 0 หยุดทุกอย่างที่อิงเวลา (GameClock, coroutine ของ CallCenterManager, ความหิว)
/// และ AudioListener.pause = true หยุดเสียงทั้งหมดในซีนไปพร้อมกัน (เช่น เสียงเรียกเข้า)
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("ชื่อซีน Main Menu (ต้องตรงกับชื่อไฟล์ซีนเป๊ะๆ และถูกเพิ่มใน Build Settings แล้ว)")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("UI (optional)")]
    [Tooltip("แผง Pause ที่จะโผล่ขึ้นมา ไม่ใส่ก็ได้ถ้าแค่อยากให้เกมหยุดเฉยๆ")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button quitButton;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(BackToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            // เวลาถูกหยุดโดยระบบอื่นอยู่แล้ว (หน้าชนะ/หน้าแพ้) ไม่ให้กด Pause แล้ว Resume ทำให้เกมเดินต่อ
            if (!IsPaused && Time.timeScale == 0f) return;

            Toggle();
        }
    }

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    /// เรียกจากปุ่ม "กลับเมนูหลัก" ในแผง Pause
    public void BackToMainMenu()
    {
        // คืนค่าเวลา/เสียงให้ปกติก่อนออกจากซีนนี้ ไม่งั้นซีนถัดไปจะค้างที่ timeScale 0
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// เรียกจากปุ่ม "ออกจากเกม" ในแผง Pause
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // ทดสอบใน Editor ให้กดแล้วหยุด Play
#else
        Application.Quit(); // มีผลตอน Build จริงเท่านั้น
#endif
    }

    private void OnDestroy()
    {
        // กันเวลา/เสียงค้างตอนปิดซีน/จบเกมขณะยัง Pause อยู่
        if (IsPaused)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
