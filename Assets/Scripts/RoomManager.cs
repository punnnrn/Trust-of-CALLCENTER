using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// คุม UI ห้อง: เตียง (กดแล้วนอน/จบวันทันที) และคอมพิวเตอร์ (กดแล้วเปิดหน้าจอคอมเต็มจอ)
/// ตอนเปิดคอม จะซ่อนปุ่มเตียงกับปุ่มคอมไว้ก่อน จนกว่าจะปิดคอมกลับมาที่ห้อง
///
/// แอปในคอม: วางเป็นไอคอนปุ่มธรรมดาใน computerScreenPanel แล้วผูก OnClick() ตรงๆ ใน Inspector
/// เช่น แอปชำระหนี้ -> เรียก DebtPaymentUI.OpenPanel() ของพาแนลที่ทำไว้แล้ว ไม่ต้องแก้โค้ดตัวนั้นเลย
/// แอปใหม่ในอนาคตก็ทำแบบเดียวกัน: สร้างไอคอน + พาแนลของแอปนั้น + ผูก OnClick ไม่ต้องแก้ RoomManager
/// </summary>
public class RoomManager : MonoBehaviour
{
    [Header("ห้อง")]
    [SerializeField] private Button bedButton;
    [SerializeField] private Button computerButton;

    [Header("หน้าจอคอมพิวเตอร์")]
    [SerializeField] private GameObject computerScreenPanel;
    [Tooltip("ปุ่มปิดคอม อยู่ข้างใน computerScreenPanel")]
    [SerializeField] private Button closeComputerButton;

    [Header("ยืนยันการนอน")]
    [Tooltip("แผงถามยืนยัน เช่น \"แน่ใจไหมว่าจะนอน?\" โผล่ขึ้นมาก่อนกดเตียงจะจบวันจริง")]
    [SerializeField] private GameObject sleepConfirmPanel;
    [SerializeField] private Button confirmSleepButton; // ปุ่ม "นอน" ในแผงยืนยัน
    [SerializeField] private Button cancelSleepButton;   // ปุ่ม "ไม่นอน" ในแผงยืนยัน

    private void Awake()
    {
        if (bedButton != null) bedButton.onClick.AddListener(OpenSleepConfirm);
        if (computerButton != null) computerButton.onClick.AddListener(OpenComputer);
        if (closeComputerButton != null) closeComputerButton.onClick.AddListener(CloseComputer);

        if (confirmSleepButton != null) confirmSleepButton.onClick.AddListener(ConfirmSleep);
        if (cancelSleepButton != null) cancelSleepButton.onClick.AddListener(CancelSleep);

        if (computerScreenPanel != null) computerScreenPanel.SetActive(false);
        if (sleepConfirmPanel != null) sleepConfirmPanel.SetActive(false);
        ShowRoomButtons(true);
    }

    /// กดปุ่มเตียง: ยังไม่นอนทันที ให้โผล่แผงถามยืนยันก่อน
    public void OpenSleepConfirm()
    {
        if (sleepConfirmPanel != null) sleepConfirmPanel.SetActive(true);
        ShowRoomButtons(false);
    }

    /// กด "นอน" ในแผงยืนยัน: จบวันจริง นอนเป็นใหญ่สุด ตัดสาย/บทสนทนาที่ค้างอยู่ทิ้งทันที
    /// (ความหิวจะถูกเติมเต็มเองจาก CallCenterManager.EndDay())
    public void ConfirmSleep()
    {
        if (sleepConfirmPanel != null) sleepConfirmPanel.SetActive(false);
        ShowRoomButtons(true);
        CallCenterManager.Instance?.ForceSleepNow();
    }

    /// กด "ไม่นอน" ในแผงยืนยัน: ปิดแผง กลับมาที่ห้องเหมือนเดิม
    public void CancelSleep()
    {
        if (sleepConfirmPanel != null) sleepConfirmPanel.SetActive(false);
        ShowRoomButtons(true);
    }

    /// เปิดหน้าจอคอม ซ่อนปุ่มห้องไว้ก่อน
    public void OpenComputer()
    {
        if (computerScreenPanel != null) computerScreenPanel.SetActive(true);
        ShowRoomButtons(false);
    }

    /// ปิดคอม กลับมาที่ห้อง (ปิดแผงถามยืนยันการนอนที่อาจค้างอยู่ด้วย เผื่อวันจบตอนแผงนั้นเปิดอยู่)
    public void CloseComputer()
    {
        if (computerScreenPanel != null) computerScreenPanel.SetActive(false);
        if (sleepConfirmPanel != null) sleepConfirmPanel.SetActive(false);
        ShowRoomButtons(true);
    }

    /// จบเกม (แพ้): ปิดคอม/แผงถามนอนที่เปิดค้างอยู่ แล้วซ่อนปุ่มเตียงกับปุ่มคอมถาวร ไม่ให้กดอะไรต่อได้
    public void LockForGameOver()
    {
        if (computerScreenPanel != null) computerScreenPanel.SetActive(false);
        if (sleepConfirmPanel != null) sleepConfirmPanel.SetActive(false);
        ShowRoomButtons(false);
    }

    private void ShowRoomButtons(bool show)
    {
        if (bedButton != null) bedButton.gameObject.SetActive(show);
        if (computerButton != null) computerButton.gameObject.SetActive(show);
    }
}
