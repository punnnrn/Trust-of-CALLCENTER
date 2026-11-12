using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// หน้าต่าง Cheat แบบ UI (มีปุ่มคลิกได้) กด Alt+F เพื่อเปิด/ปิดระหว่างเล่นเกม
/// สร้าง UI ด้วยโค้ดตอนเริ่มเกม ไม่ต้องไปวางอะไรใน Canvas เอง (มี Canvas ของตัวเองลอยอยู่บนสุด)
/// สั่งงานผ่าน DebugCheats เพื่อให้เงิน/ความเร็ว/ความหิวตรงกับคีย์ลัด F2-F10 เสมอ จึงต้องมี DebugCheats ในซีนด้วย
/// ทำงานเฉพาะใน Unity Editor และ Development Build (Build จริงจะลบตัวเองทิ้ง)
/// หมายเหตุ: ปุ่ม YOU WIN / YOU LOSE จะลบไฟล์เซฟเหมือนจบเกมจริง จึงต้องกด 2 ครั้งเพื่อยืนยัน
/// </summary>
[DisallowMultipleComponent]
public class UICheat : MonoBehaviour
{
    [Header("เปิด/ปิด")]
    [SerializeField] private bool cheatsEnabled = true;
    [Tooltip("เปิด = ให้ใช้โกงใน Build ปกติ (ไม่ใช่ Development Build) ด้วย ระวัง: ถ้าส่ง Build ให้คนอื่นเล่น ทุกคนจะกดโกงได้ ควรปิดก่อนปล่อยเกมจริง")]
    [SerializeField] private bool allowInReleaseBuild = false;
    [Tooltip("โชว์หน้าต่างตั้งแต่เริ่มเกมเลยไหม")]
    [SerializeField] private bool startVisible = false;

    [Header("คีย์ลัด (เปลี่ยนได้)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;
    [Tooltip("ต้องกด Alt ค้างไว้พร้อมกันไหม (Alt+F เปิด/ปิดหน้าต่าง)")]
    [SerializeField] private bool toggleNeedsAlt = true;
    [Tooltip("คีย์สำรองเปิด/ปิดโดยไม่ต้องกด Alt (เผื่อ Unity Editor ดักคีย์ Alt+F ไว้) None = ไม่ใช้")]
    [SerializeField] private KeyCode alternateToggleKey = KeyCode.F11;

    [Header("จำนวนที่เปลี่ยนต่อการกด 1 ครั้ง")]
    [Tooltip("เรียงจากน้อยไปมาก จะได้ปุ่ม -มาก -น้อย +น้อย +มาก")]
    [SerializeField] private int[] moneySteps = { 500, 5000 };
    [SerializeField] private float hungerStep = 20f;

    [Header("หน้าตา")]
    [SerializeField] private int sortingOrder = 999;
    [SerializeField] private float panelWidth = 720f;

    private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.11f, 0.94f);
    private static readonly Color ButtonColor = new Color(0.23f, 0.27f, 0.35f, 1f);
    private static readonly Color GoodColor = new Color(0.17f, 0.55f, 0.30f, 1f);
    private static readonly Color BadColor = new Color(0.72f, 0.22f, 0.22f, 1f);
    private static readonly Color CloseColor = new Color(0.45f, 0.20f, 0.20f, 1f);
    private const float ConfirmWindow = 2.5f;

    private DebugCheats cheats;
    private GameObject canvasRoot;
    private GameObject panel;
    private Font font;
    private Text moneyText, speedText, hungerText, dayText, winLabel, loseLabel;
    private string pendingConfirm;
    private float pendingTime;
    private bool visible;

    private DebugCheats Cheats
    {
        get
        {
            if (cheats == null) cheats = FindFirstObjectByType<DebugCheats>();
            return cheats;
        }
    }

    private string HotkeyLabel
    {
        get { return (toggleNeedsAlt ? "Alt+" : "") + toggleKey + (alternateToggleKey != KeyCode.None ? " / " + alternateToggleKey : ""); }
    }

    private void Awake()
    {
        // Build จริง (ไม่ใช่ Development) ไม่ให้มีโกงหลงไป
        if (!Application.isEditor && !Debug.isDebugBuild && !allowInReleaseBuild)
        {
            Destroy(gameObject);
            return;
        }
        BuildUI();
        SetVisible(startVisible);
    }

    private void Update()
    {
        if (!cheatsEnabled)
        {
            if (visible) SetVisible(false);
            return;
        }

        if (Pressed(toggleKey) && (!toggleNeedsAlt || AltHeld())) SetVisible(!visible);
        else if (Pressed(alternateToggleKey)) SetVisible(!visible);

        if (!visible) return;

        if (pendingConfirm != null && Time.unscaledTime - pendingTime > ConfirmWindow) pendingConfirm = null;
        Refresh();
    }

    // ---------------- Actions ----------------

    private void SetVisible(bool v)
    {
        visible = v;
        pendingConfirm = null;
        if (canvasRoot != null) canvasRoot.SetActive(v);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (v) { Refresh(); ClampPanel(); }
    }

    private void Run(System.Action<DebugCheats> action)
    {
        DebugCheats dc = Cheats;
        if (dc == null)
        {
            Debug.LogWarning("[UICheat] ไม่พบ DebugCheats ในซีน");
            return;
        }
        action(dc);
    }

    private void OnGameOverClicked(bool win)
    {
        string id = win ? "win" : "lose";
        if (pendingConfirm == id && Time.unscaledTime - pendingTime <= ConfirmWindow)
        {
            pendingConfirm = null;
            SetVisible(false);
            Run(dc => { if (win) dc.GoToWin(); else dc.GoToLose(); });
            return;
        }
        pendingConfirm = id;
        pendingTime = Time.unscaledTime;
        Refresh();
    }

    private void Refresh()
    {
        PlayerStats ps = PlayerStats.Instance;
        HungerSystem h = HungerSystem.Instance;
        GameClock clock = GameClock.Instance;
        DebugCheats dc = Cheats;

        moneyText.text = "Money: " + (ps != null ? ps.Money.ToString("N0") : "-");
        speedText.text = "Speed: " + (dc != null ? dc.SpeedLabel : "-");
        hungerText.text = "Hunger: " + (h != null ? Mathf.RoundToInt(h.Normalized * 100f) + "%" : "-");
        dayText.text = clock != null ? "Day " + clock.Day + "    " + clock.GetTimeString() : "";
        winLabel.text = pendingConfirm == "win" ? "CLICK AGAIN" : "YOU WIN";
        loseLabel.text = pendingConfirm == "lose" ? "CLICK AGAIN" : "YOU LOSE";
    }

    // ---------------- Drag ----------------

    // กันหน้าต่างหลุดออกนอกจอ
    private void ClampPanel()
    {
        if (panel == null || canvasRoot == null) return;
        RectTransform prt = (RectTransform)panel.transform;
        RectTransform crt = (RectTransform)canvasRoot.transform;
        Vector2 cs = crt.rect.size;
        if (cs.x <= 1f || cs.y <= 1f) return; // Canvas ยังไม่คำนวณขนาด
        Vector2 ps = prt.rect.size;
        Vector2 pos = prt.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -Mathf.Max(0f, cs.x - ps.x), 0f);
        pos.y = Mathf.Clamp(pos.y, -Mathf.Max(0f, cs.y - ps.y), 0f);
        prt.anchoredPosition = pos;
    }

    // ---------------- Build UI ----------------

    private void BuildUI()
    {
        font = LoadFont();
        EnsureEventSystem();

        canvasRoot = new GameObject("UICheatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(transform, false);
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(canvasRoot.transform, false);
        RectTransform prt = (RectTransform)panel.transform;
        prt.anchorMin = new Vector2(1f, 1f);
        prt.anchorMax = new Vector2(1f, 1f);
        prt.pivot = new Vector2(1f, 1f);
        prt.anchoredPosition = new Vector2(-24f, -24f);
        prt.sizeDelta = new Vector2(panelWidth, 0f);
        panel.GetComponent<Image>().color = PanelColor;

        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(18, 18, 14, 18);
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fit = panel.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header
        RectTransform header = MakeRow("Header", 56f);
        // ลากที่แถบหัวข้อเพื่อเลื่อนหน้าต่างได้
        Image headerBg = header.gameObject.AddComponent<Image>();
        headerBg.color = new Color(1f, 1f, 1f, 0.08f);
        UICheatDragHandle drag = header.gameObject.AddComponent<UICheatDragHandle>();
        drag.target = prt;
        drag.canvas = canvas;
        drag.onMoved = ClampPanel;
        Text title = MakeText(header, "Title", "UI CHEAT", 32, TextAnchor.MiddleLeft, Color.white);
        title.fontStyle = FontStyle.Bold;
        title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        Text hint = MakeText(header, "Hint", HotkeyLabel, 22, TextAnchor.MiddleRight, new Color(1f, 1f, 1f, 0.55f));
        hint.gameObject.AddComponent<LayoutElement>().preferredWidth = 200f;
        MakeButton(header, "Close", "X", 56f, CloseColor, () => SetVisible(false));

        // Day / time
        dayText = MakeText(panel.transform, "Day", "", 22, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.6f));
        dayText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        // Money
        int[] steps = (moneySteps != null && moneySteps.Length > 0) ? moneySteps : new[] { 500 };
        RectTransform moneyRow = MakeRow("MoneyRow", 60f);
        moneyText = MakeLabel(moneyRow, "Money: -");
        for (int i = steps.Length - 1; i >= 0; i--)
        {
            int s = steps[i];
            MakeButton(moneyRow, "Money-" + s, "-" + s, 104f, ButtonColor, () => Run(dc => dc.ChangeMoney(-s)));
        }
        for (int i = 0; i < steps.Length; i++)
        {
            int s = steps[i];
            MakeButton(moneyRow, "Money+" + s, "+" + s, 104f, ButtonColor, () => Run(dc => dc.ChangeMoney(s)));
        }

        // Speed
        RectTransform speedRow = MakeRow("SpeedRow", 60f);
        speedText = MakeLabel(speedRow, "Speed: -");
        MakeButton(speedRow, "SpeedDown", "-", 104f, ButtonColor, () => Run(dc => dc.ChangeSpeed(-1)));
        MakeButton(speedRow, "SpeedReset", "Reset", 104f, ButtonColor, () => Run(dc => dc.ResetSpeed()));
        MakeButton(speedRow, "SpeedUp", "+", 104f, ButtonColor, () => Run(dc => dc.ChangeSpeed(+1)));

        // Hunger
        RectTransform hungerRow = MakeRow("HungerRow", 60f);
        hungerText = MakeLabel(hungerRow, "Hunger: -");
        float hs = hungerStep;
        MakeButton(hungerRow, "HungerDown", "-" + Mathf.RoundToInt(hs), 104f, ButtonColor, () => Run(dc => dc.ChangeHunger(-hs)));
        MakeButton(hungerRow, "HungerUp", "+" + Mathf.RoundToInt(hs), 104f, ButtonColor, () => Run(dc => dc.ChangeHunger(hs)));
        MakeButton(hungerRow, "HungerFull", "Full", 104f, ButtonColor, () => Run(dc => dc.FillHunger()));

        // Win / Lose
        RectTransform endRow = MakeRow("EndRow", 68f);
        Button winBtn = MakeButton(endRow, "Win", "YOU WIN", 0f, GoodColor, () => OnGameOverClicked(true));
        Button loseBtn = MakeButton(endRow, "Lose", "YOU LOSE", 0f, BadColor, () => OnGameOverClicked(false));
        winLabel = winBtn.GetComponentInChildren<Text>();
        loseLabel = loseBtn.GetComponentInChildren<Text>();

        Text note = MakeText(panel.transform, "Note", "YOU WIN / YOU LOSE ลบไฟล์เซฟเหมือนจบเกมจริง กด 2 ครั้งเพื่อยืนยัน", 18, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.45f));
        note.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
    }

    private RectTransform MakeRow(string name, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup h = go.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 8f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        go.GetComponent<LayoutElement>().preferredHeight = height;
        return (RectTransform)go.transform;
    }

    private Text MakeLabel(Transform parent, string content)
    {
        Text t = MakeText(parent, "Label", content, 28, TextAnchor.MiddleLeft, Color.white);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return t;
    }

    private Text MakeText(Transform parent, string name, string content, int size, TextAnchor anchor, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // width <= 0 = ยืดเต็มพื้นที่ที่เหลือ
    private Button MakeButton(Transform parent, string name, string label, float width, Color color, UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.color = color;

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (width > 0f) { le.preferredWidth = width; le.flexibleWidth = 0f; }
        else { le.flexibleWidth = 1f; }

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.82f, 0.88f, 1f, 1f);
        cb.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        cb.selectedColor = Color.white;
        cb.colorMultiplier = 1f;
        btn.colors = cb;
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None; // กัน Space/Enter ไปกดปุ่มโดยไม่ตั้งใจ
        btn.navigation = nav;

        Text txt = MakeText(go.transform, "Text", label, 26, TextAnchor.MiddleCenter, Color.white);
        txt.fontStyle = FontStyle.Bold;
        RectTransform trt = (RectTransform)txt.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        btn.onClick.AddListener(() =>
        {
            if (onClick != null) onClick.Invoke();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        });
        return btn;
    }

    private static Font LoadFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 16);
        return f;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    // ---------------- Input (อ่านทั้ง Input เก่าและ Input System ถ้ามีทั้งคู่) ----------------
    // Input System อ่านตามตำแหน่งปุ่มจริง ไม่ขึ้นกับภาษาคีย์บอร์ด (ไทย/อังกฤษ) จึงช่วยให้ Alt+F ติดแน่นอนขึ้น

    private static bool Pressed(KeyCode key)
    {
        if (key == KeyCode.None) return false;
        bool hit = false;
#if ENABLE_LEGACY_INPUT_MANAGER
        hit = Input.GetKeyDown(key);
#endif
#if ENABLE_INPUT_SYSTEM
        if (!hit)
        {
            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            UnityEngine.InputSystem.Key k;
            if (kb != null && System.Enum.TryParse(key.ToString(), out k)) hit = kb[k].wasPressedThisFrame;
        }
#endif
        return hit;
    }

    private static bool AltHeld()
    {
        bool alt = false;
#if ENABLE_LEGACY_INPUT_MANAGER
        alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
#endif
#if ENABLE_INPUT_SYSTEM
        if (!alt)
        {
            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            alt = kb != null && kb.altKey.isPressed;
        }
#endif
        return alt;
    }
}

/// <summary>ลากที่แถบหัวข้อของ UICheat เพื่อเลื่อนหน้าต่าง</summary>
public class UICheatDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform target;
    public Canvas canvas;
    public System.Action onMoved;

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (target == null) return;
        float s = (canvas != null && canvas.scaleFactor > 0f) ? canvas.scaleFactor : 1f;
        target.anchoredPosition += eventData.delta / s;
        if (onMoved != null) onMoved();
    }
}
