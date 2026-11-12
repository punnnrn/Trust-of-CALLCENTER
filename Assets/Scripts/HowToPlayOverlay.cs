using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// การ์ดอธิบายวิธีเล่น โชว์ครั้งแรกที่ผู้เล่นเปิดมินิเกมนั้น (นับต่อการเปิดเกม 1 รอบ)
/// มินิเกมที่ต้องการเลื่อนการเริ่มรอบจะส่ง callback มา แล้วจะถูกเรียกหลังกด "เข้าใจแล้ว"
public class HowToPlayOverlay : MonoBehaviour
{
    private static readonly HashSet<string> seen = new HashSet<string>();

    [SerializeField] private string gameKey = "game";
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button okButton;

    private Action pending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSeen() { seen.Clear(); }

    private void Awake()
    {
        if (okButton != null) okButton.onClick.AddListener(OnOkClicked);
    }

    // ถ้าผู้เล่นปิดหน้าต่างเกมทั้งบานระหว่างอ่าน จะไม่เริ่มเกมให้ และไม่นับว่าอ่านแล้ว
    private void OnDisable()
    {
        pending = null;
    }

    /// คืน true = โชว์การ์ดแล้ว (onClose จะถูกเรียกหลังกดตกลง) / false = เคยอ่านแล้ว ให้เริ่มเกมต่อได้เลย
    public bool ShowIfFirstTime(string body, Action onClose = null)
    {
        if (seen.Contains(gameKey)) return false;
        Show(body, onClose);
        return true;
    }

    public void Show(string body, Action onClose = null)
    {
        pending = onClose;
        if (bodyText != null) bodyText.text = body;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void OnOkClicked()
    {
        Action callback = pending;
        pending = null;
        seen.Add(gameKey);

        // ปุ่มที่เพิ่งกดยังถูก 'เลือก' ค้างใน EventSystem ถ้าไม่ล้าง การกด Space/Enter จะไปสั่งปุ่มนั้นซ้ำ
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        gameObject.SetActive(false);
        if (callback != null) callback();
    }
}
