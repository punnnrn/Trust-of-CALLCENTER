using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// คีย์ลัดสำหรับเทสเกม (ไม่มีปุ่มบนจอ): เสกเงิน เพิ่ม/ลดความเร็วเกม เพิ่ม/ลดความหิว และข้ามไปหน้า You Win / You Lose
/// ทำงานเฉพาะใน Unity Editor และ Development Build เท่านั้น (Build จริงจะลบตัวเองทิ้ง)
/// เปลี่ยนคีย์และจำนวนที่เปลี่ยนต่อครั้งได้ที่ Inspector ของ Object นี้ (DebugCheats) กด F1 เพื่อเปิด/ปิดแผงรายการคีย์
/// หมายเหตุ: Win/Lose จะลบไฟล์เซฟเหมือนจบเกมจริง
/// </summary>
[DisallowMultipleComponent]
public class DebugCheats : MonoBehaviour
{
    [Header("เปิด/ปิด")]
    [SerializeField] private bool cheatsEnabled = true;
    [Tooltip("เปิด = ให้ใช้โกงใน Build ปกติ (ไม่ใช่ Development Build) ด้วย ระวัง: ถ้าส่ง Build ให้คนอื่นเล่น ทุกคนจะกดโกงได้ ควรปิดก่อนปล่อยเกมจริง")]
    [SerializeField] private bool allowInReleaseBuild = false;
    [Tooltip("โชว์แผงรายการคีย์ตั้งแต่เริ่มเกมเลยไหม (กดคีย์ Toggle เพื่อสลับได้ตลอด)")]
    [SerializeField] private bool showOverlayOnStart = false;
    [Tooltip("โชว์ป้ายเล็กๆ มุมซ้ายบน (เช่น [Alt+F1] Debug) ตอนแผงปิดอยู่ไหม")]
    [SerializeField] private bool showHintWhenHidden = false;

    [Header("คีย์ลัด (เปลี่ยนได้)")]
    [SerializeField] private KeyCode toggleOverlayKey = KeyCode.F;
    [Tooltip("ต้องกด Alt ค้างไว้พร้อมกันไหม (Alt+F เปิด/ปิดแผง Cheat)")]
    [SerializeField] private bool toggleOverlayNeedsAlt = true;
    [SerializeField] private KeyCode moneyUpKey = KeyCode.F2;
    [SerializeField] private KeyCode moneyDownKey = KeyCode.F3;
    [SerializeField] private KeyCode speedUpKey = KeyCode.F4;
    [SerializeField] private KeyCode speedDownKey = KeyCode.F5;
    [SerializeField] private KeyCode hungerUpKey = KeyCode.F6;
    [SerializeField] private KeyCode hungerDownKey = KeyCode.F7;
    [SerializeField] private KeyCode winKey = KeyCode.F8;
    [SerializeField] private KeyCode loseKey = KeyCode.F9;
    [SerializeField] private KeyCode speedResetKey = KeyCode.F10;

    [Header("จำนวนที่เปลี่ยนต่อการกด 1 ครั้ง")]
    [SerializeField] private int moneyStep = 500;
    [Tooltip("ความหิวเต็มหลอด = 100")]
    [SerializeField] private float hungerStep = 20f;
    [Tooltip("ตัวคูณความเร็วนาฬิกาในเกมที่ไล่ขึ้น/ลงตามลำดับ (1 = ความเร็วปกติที่ตั้งไว้ใน GameClock)")]
    [SerializeField] private float[] speedLevels = { 0.25f, 0.5f, 1f, 2f, 4f, 8f, 16f };

    private bool overlayVisible;
    private bool gameOverTriggered;
    private float baseSpeed = 1f;
    private int speedIndex;
    private string lastAction = "";
    private float lastActionTime = -100f;
    private GUIStyle boxStyle;
    private GUIStyle hintStyle;

    private void Awake()
    {
        // Build จริง (ไม่ใช่ Development) ไม่ให้มีโกงหลงไป
        if (!Application.isEditor && !Debug.isDebugBuild && !allowInReleaseBuild)
        {
            Destroy(gameObject);
            return;
        }
        overlayVisible = showOverlayOnStart;
    }

    private void Start()
    {
        if (speedLevels == null || speedLevels.Length == 0) speedLevels = new[] { 1f };
        if (GameClock.Instance != null) baseSpeed = Mathf.Max(0.0001f, GameClock.Instance.MinutesPerRealSecond);
        speedIndex = NearestLevelIndex(1f);
    }

    private void Update()
    {
        if (!cheatsEnabled) return;

        if (Pressed(toggleOverlayKey) && (!toggleOverlayNeedsAlt || AltHeld())) overlayVisible = !overlayVisible;

        if (Pressed(moneyUpKey)) ChangeMoney(+moneyStep);
        if (Pressed(moneyDownKey)) ChangeMoney(-moneyStep);

        if (Pressed(speedUpKey)) ChangeSpeed(+1);
        if (Pressed(speedDownKey)) ChangeSpeed(-1);
        if (Pressed(speedResetKey)) ResetSpeed();

        if (Pressed(hungerUpKey)) ChangeHunger(+hungerStep);
        if (Pressed(hungerDownKey)) ChangeHunger(-hungerStep);

        if (Pressed(winKey)) GoToWin();
        if (Pressed(loseKey)) GoToLose();
    }

    // ---------------- Actions ----------------

    public void ChangeMoney(int delta)
    {
        if (PlayerStats.Instance == null) { Report("PlayerStats not found"); return; }
        PlayerStats.Instance.AddMoney(delta);
        Report($"Money {(delta > 0 ? "+" : "")}{delta}  (now {PlayerStats.Instance.Money})");
    }

    public void ChangeSpeed(int direction)
    {
        if (GameClock.Instance == null) { Report("GameClock not found"); return; }
        speedIndex = Mathf.Clamp(speedIndex + direction, 0, speedLevels.Length - 1);
        ApplySpeed();
    }

    public void ResetSpeed()
    {
        if (GameClock.Instance == null) { Report("GameClock not found"); return; }
        speedIndex = NearestLevelIndex(1f);
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        float mul = speedLevels[speedIndex];
        GameClock.Instance.SetSpeed(baseSpeed * mul);
        Report($"Game speed x{mul:0.##}");
    }

    public void ChangeHunger(float delta)
    {
        HungerSystem h = HungerSystem.Instance;
        if (h == null) { Report("HungerSystem not found"); return; }
        // ใช้ RefillBy/Drain (ไม่ใช่ Eat) เพื่อไม่ให้นับว่า "วันนี้ได้กินอาหารแล้ว"
        if (delta >= 0f) h.RefillBy(delta); else h.Drain(-delta);
        Report($"Hunger {(delta > 0 ? "+" : "")}{delta:0}  (now {Mathf.RoundToInt(h.Normalized * 100f)}%)");
    }

    public void GoToWin()
    {
        if (gameOverTriggered) { Report("Game over screen already shown"); return; }
        if (WinScreenManager.Instance == null) { Report("WinScreenManager not found"); return; }
        gameOverTriggered = true;
        Report("-> WIN screen");
        WinScreenManager.Instance.ShowWin();
    }

    public void GoToLose()
    {
        if (gameOverTriggered) { Report("Game over screen already shown"); return; }
        if (StarvationSystem.Instance == null) { Report("StarvationSystem not found"); return; }
        gameOverTriggered = true;
        Report("-> LOSE screen");
        int day = GameClock.Instance != null ? GameClock.Instance.Day : 1;
        StarvationSystem.Instance.TriggerLose(day);
    }

    // ---------------- Public API (ให้ UICheat เรียกใช้) ----------------

    public bool CheatsEnabled => cheatsEnabled;

    public string SpeedLabel
    {
        get
        {
            if (speedLevels == null || speedLevels.Length == 0) return "-";
            return "x" + speedLevels[Mathf.Clamp(speedIndex, 0, speedLevels.Length - 1)].ToString("0.##");
        }
    }

    public void FillHunger()
    {
        HungerSystem h = HungerSystem.Instance;
        if (h == null) { Report("HungerSystem not found"); return; }
        h.RefillFull();
        Report("Hunger FULL");
    }

    // ---------------- Helpers ----------------

    private int NearestLevelIndex(float target)
    {
        int best = 0;
        float bestDiff = float.MaxValue;
        for (int i = 0; i < speedLevels.Length; i++)
        {
            float d = Mathf.Abs(speedLevels[i] - target);
            if (d < bestDiff) { bestDiff = d; best = i; }
        }
        return best;
    }

    private void Report(string message)
    {
        lastAction = message;
        lastActionTime = Time.unscaledTime;
        Debug.Log("[DebugCheats] " + message);
    }

    private static bool Pressed(KeyCode key)
    {
        if (key == KeyCode.None) return false;
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(key);
#elif ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;
        Key k;
        if (!System.Enum.TryParse(key.ToString(), out k)) return false;
        return kb[k].wasPressedThisFrame;
#else
        return false;
#endif
    }

    private static bool AltHeld()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
#elif ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.altKey.isPressed;
#else
        return false;
#endif
    }

    private string ToggleLabel => (toggleOverlayNeedsAlt ? "Alt+" : "") + toggleOverlayKey;

    // ---------------- Overlay (IMGUI ไม่ต้องมี UI ในซีน) ----------------

    private void OnGUI()
    {
        if (!cheatsEnabled) return;

        float scale = Mathf.Max(0.75f, Screen.height / 1080f);
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true };
            hintStyle = new GUIStyle(GUI.skin.label) { richText = true };
        }
        boxStyle.fontSize = Mathf.RoundToInt(15 * scale);
        hintStyle.fontSize = Mathf.RoundToInt(13 * scale);

        if (!overlayVisible)
        {
            if (showHintWhenHidden) GUI.Label(new Rect(8 * scale, 4 * scale, 260 * scale, 24 * scale), $"<color=#ffffffaa>[{ToggleLabel}] Debug</color>", hintStyle);
            return;
        }

        string money = PlayerStats.Instance != null ? PlayerStats.Instance.Money.ToString() : "-";
        string hunger = HungerSystem.Instance != null ? Mathf.RoundToInt(HungerSystem.Instance.Normalized * 100f) + "%" : "-";
        string speed = speedLevels != null && speedLevels.Length > 0 ? $"x{speedLevels[speedIndex]:0.##}" : "-";
        string day = GameClock.Instance != null ? $"Day {GameClock.Instance.Day}  {GameClock.Instance.GetTimeString()}" : "-";

        string text =
            $"<b>DEBUG CHEATS</b>  ({ToggleLabel} = hide)\n" +
            $"{moneyUpKey} / {moneyDownKey}   Money +/- {moneyStep}      now {money}\n" +
            $"{speedUpKey} / {speedDownKey}   Game speed +/-            now {speed}   ({speedResetKey} = reset)\n" +
            $"{hungerUpKey} / {hungerDownKey}   Hunger +/- {hungerStep:0}          now {hunger}\n" +
            $"{winKey}   Go to YOU WIN screen\n" +
            $"{loseKey}   Go to YOU LOSE screen\n" +
            $"{day}";

        float w = 520 * scale, h = 170 * scale;
        GUI.Box(new Rect(8 * scale, 8 * scale, w, h), text, boxStyle);

        if (Time.unscaledTime - lastActionTime < 2.5f)
            GUI.Label(new Rect(14 * scale, (8 + 172) * scale, 520 * scale, 26 * scale), $"<color=#ffe066>{lastAction}</color>", hintStyle);
    }
}
