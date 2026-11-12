using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// ป้ายสถานะบนไอคอนมินิเกมในหน้าจอคอม: "พร้อมเล่น" หรือ "รอ 1 ชม. 20 น." และหรี่ไอคอนตอนติดคูลดาวน์
public class CooldownBadge : MonoBehaviour
{
    [Tooltip("ต้องเป็นสคริปต์ที่ implement ICooldownProvider")]
    [SerializeField] private MonoBehaviour source;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Graphic iconToDim;
    [SerializeField] private Color readyColor = new Color(0.15f, 0.6f, 0.2f, 1f);
    [SerializeField] private Color coolingColor = new Color(0.85f, 0.4f, 0.05f, 1f);
    [SerializeField] private Color dimmedIconColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private ICooldownProvider provider;
    private Color iconOriginal = Color.white;
    private float nextRefresh;

    private void Awake()
    {
        provider = source as ICooldownProvider;
        if (iconToDim != null) iconOriginal = iconToDim.color;
    }

    private void OnEnable()
    {
        nextRefresh = 0f;
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        Refresh();
    }

    private void Refresh()
    {
        if (provider == null) provider = source as ICooldownProvider;
        if (provider == null || label == null) return;

        if (provider.IsCoolingDown)
        {
            label.text = "รอ " + FormatGameTime(provider.CooldownRemainingGameSeconds);
            label.color = coolingColor;
            if (iconToDim != null) iconToDim.color = dimmedIconColor;
        }
        else
        {
            label.text = "พร้อมเล่น";
            label.color = readyColor;
            if (iconToDim != null) iconToDim.color = iconOriginal;
        }
    }

    private static string FormatGameTime(int gameSeconds)
    {
        int totalMinutes = Mathf.CeilToInt(gameSeconds / 60f);
        int h = totalMinutes / 60;
        int m = totalMinutes % 60;
        if (h > 0 && m > 0) return h + " ชม. " + m + " น.";
        if (h > 0) return h + " ชม.";
        return m + " น.";
    }
}
