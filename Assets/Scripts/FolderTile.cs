using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FolderTile : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI folderNameText;

    private int folderIndex;
    private MalwareGameManager manager;

    // 💡 ฟังก์ชัน Setup สำหรับรับค่า Index, ชื่อโฟลเดอร์ และ Reference ของ GameManager
    public void Setup(int index, string name, MalwareGameManager manager)
    {
        this.folderIndex = index;
        this.manager = manager;

        if (folderNameText != null)
        {
            folderNameText.text = name;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnFolderClicked);
            button.interactable = true;
        }
    }

    private void OnFolderClicked()
    {
        if (manager != null)
        {
            manager.OpenFolder(folderIndex);
        }
    }

    // 💡 ฟังก์ชัน SetInteractable สำหรับเปิด/ปิดการกดปุ่มโฟลเดอร์ช่วงคูลดาวน์
    public void SetInteractable(bool state)
    {
        if (button != null)
        {
            button.interactable = state;
        }
    }
}