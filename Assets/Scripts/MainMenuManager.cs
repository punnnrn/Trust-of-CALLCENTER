using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ควบคุมปุ่มในหน้า Main Menu: เริ่มเกม (โหลดซีนเกมเพลย์) และออกจากเกม
/// ผูกกับปุ่มผ่าน OnClick() ใน Inspector ได้โดยตรง ไม่ต้องมีสคริปต์อื่นเรียก
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("ชื่อซีนเกมเพลย์ (ต้องตรงกับชื่อไฟล์ซีนเป๊ะๆ และถูกเพิ่มใน Build Settings แล้ว)")]
    [SerializeField] private string gameplaySceneName = "Game";

    /// เรียกจากปุ่ม "เริ่มเกม"
    public void StartGame()
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
