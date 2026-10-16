using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ควบคุมปุ่มในหน้า Main Menu: New Game, Continue และออกจากเกม
/// - New Game: ลบเซฟเก่าทิ้งแล้วเริ่มวันที่ 1 ใหม่หมด
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

    private void Start()
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(SaveSystem.HasSave());
    }

    /// เรียกจากปุ่ม "New Game"
    public void NewGame()
    {
        SaveSystem.StartNewGame();
        LoadGameplay();
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
