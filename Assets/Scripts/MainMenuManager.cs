using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ควบคุมปุ่มในหน้า Main Menu: New Game, Continue และออกจากเกม
/// - New Game: ลบเซฟเก่าทิ้งแล้วเริ่มวันที่ 1 ใหม่หมด (ถ้ามีเซฟอยู่ จะถามยืนยันก่อน)
/// - Continue: โหลดเซฟล่าสุด (กลับไปเริ่มวันที่เซฟไว้ตั้งแต่ต้นวัน) ปุ่มจะซ่อนถ้ายังไม่มีเซฟ
/// ผูกกับปุ่มผ่าน OnClick() ใน Inspector ได้โดยตรง
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("ชื่อซีนเกมเพลย์ (ต้องตรงกับชื่อไฟล์ซีนเป๊ะๆ และถูกเพิ่มใน Build Settings แล้ว)")]
    [SerializeField] private string gameplaySceneName = "Game";

    [Header("UI")]
    [Tooltip("ปุ่ม Continue ที่จะถูกซ่อนเมื่อยังไม่มีเซฟ")]
    [SerializeField] private Button continueButton;

    [Tooltip("แผงถามยืนยันตอนกด New Game ทั้งที่มีเซฟอยู่ (ไม่ใส่ก็ได้ จะเริ่มเกมใหม่ทันทีเหมือนเดิม)")]
    [SerializeField] private GameObject newGameConfirmPanel;

    private void Start()
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(SaveSystem.HasSave());

        if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
    }

    /// เรียกจากปุ่ม "New Game": ถ้ามีเซฟอยู่จะเปิดแผงถามยืนยันก่อน ไม่มีเซฟก็เริ่มเลย
    public void NewGame()
    {
        if (newGameConfirmPanel != null && SaveSystem.HasSave())
        {
            newGameConfirmPanel.SetActive(true);
            return;
        }
        ConfirmNewGame();
    }

    /// เรียกจากปุ่ม "ยืนยัน" ในแผงถาม: ลบเซฟเก่าแล้วเริ่มวันที่ 1 ใหม่
    public void ConfirmNewGame()
    {
        if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
        SaveSystem.StartNewGame();
        LoadGameplay();
    }

    /// เรียกจากปุ่ม "ยกเลิก" ในแผงถาม: ปิดแผง กลับมาที่เมนู เซฟเก่ายังอยู่
    public void CancelNewGame()
    {
        if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
    }

    /// เรียกจากปุ่ม "Continue"
    public void Continue()
    {
        if (!SaveSystem.TryLoadForContinue())
        {
            // ไฟล์เซฟหาย/เสีย: ซ่อนปุ่มแล้วไม่ทำอะไร
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            return;
        }
        LoadGameplay();
    }

    private void LoadGameplay()
    {
        Time.timeScale = 1f;      // กันเวลาค้างถ้าก่อนหน้านี้มีการ Pause ไว้
        AudioListener.pause = false;
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// เรียกจากปุ่ม "ออกจากเกม"
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // ทดสอบใน Editor ให้กดแล้วหยุด Play
#else
        Application.Quit(); // มีผลตอน Build จริงเท่านั้น
#endif
    }
}
