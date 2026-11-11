using UnityEngine;
using UnityEngine.UI;


// 金币系统：全局单例，负责金币数值与屏幕计数 UI。
// 计数显示在血条下方，格式为 "000"（不足三位补零，超过三位正常显示）。
public class CoinManager : MonoBehaviour
{

    public static CoinManager Instance;

    // 当前金币总数
    public static int Coins = 0;

    private Text coinText;
    private GameObject coinCanvas;

    // 健壮地加载内置/系统字体：必须验证字体确实含有字形，
    // 避免拿到"空字体"（CreateDynamicFontFromOSFont 找不到字体时也会返回非 null 对象）导致文字不显示。
    public static Font LoadUIFont()
    {
        Font[] candidates = new Font[]
        {
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
            Resources.GetBuiltinResource<Font>("Arial.ttf"),
            Font.CreateDynamicFontFromOSFont("PingFang SC", 40),
            Font.CreateDynamicFontFromOSFont("Hiragino Sans GB", 40),
            Font.CreateDynamicFontFromOSFont("Helvetica", 40),
            Font.CreateDynamicFontFromOSFont("Arial", 40),
            Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 40),
            Font.CreateDynamicFontFromOSFont("Noto Sans CJK SC", 40),
            Font.CreateDynamicFontFromOSFont("Liberation Sans", 40)
        };
        foreach (var f in candidates)
        {
            if (f != null && HasGlyphs(f))
            {
                Debug.Log("[CoinManager] 选用字体: " + f.name);
                return f;
            }
        }
        Font fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Debug.Log("[CoinManager] 兜底字体: " + (fallback != null ? fallback.name : "NULL"));
        return fallback;
    }

    static bool HasGlyphs(Font f)
    {
        if (f == null)
            return false;
        try
        {
            f.RequestCharactersInTexture("0123456789金币", 40, FontStyle.Normal);
            return f.HasCharacter('0') && f.HasCharacter('1') && f.HasCharacter('币');
        }
        catch
        {
            return false;
        }
    }



    void Awake()
    {

        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureCoinUI();

    }

    void Update()
    {
        // 自愈：若 UI 因异常未创建成功，每帧重试，直到成功为止
        if(coinText == null)
            EnsureCoinUI();
    }

    void EnsureCoinUI()
    {
        if(coinText != null)
            return;
        try
        {
            CreateCoinUI();
        }
        catch(System.Exception e)
        {
            Debug.LogError("[CoinManager] 创建金币 UI 失败: " + e);
        }
    }



    // 金币 +amount，并刷新计数
    public static void AddCoin(int amount = 1)
    {

        Coins += amount;

        if(Instance != null)
            Instance.Refresh();

    }



    // 重置金币为 0（玩家死亡后调用），并刷新计数
    public static void ResetCoins()
    {
        Coins = 0;
        if(Instance != null)
            Instance.Refresh();
    }

    // 显示或隐藏金币计数 UI（例如在 "You Died" 失败界面隐藏）
    public static void SetCoinUIVisible(bool visible)
    {
        if(Instance == null)
            return;
        if(Instance.coinCanvas != null)
            Instance.coinCanvas.SetActive(visible);
    }

    // 尝试花费 amount 金币，成功扣除并返回 true
    public static bool TrySpend(int amount)
    {

        if(Coins < amount)
            return false;

        Coins -= amount;

        if(Instance != null)
            Instance.Refresh();

        return true;

    }



    // 在屏幕左上角血条正下方创建金币计数
    void CreateCoinUI()
    {

        // 画布必须作为根对象（与血条 Canvas 一致），
        // 挂到子对象下会导致 ScreenSpaceOverlay 画布无法正确渲染。
        GameObject canvasGO = new GameObject("CoinCanvas");
        DontDestroyOnLoad(canvasGO);
        coinCanvas = canvasGO;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textGO = new GameObject("CoinText");
        textGO.transform.SetParent(canvasGO.transform, false);

        coinText = textGO.AddComponent<Text>();
        // 金币只显示数字，直接用 ugui 内置字体（ugui 内部默认就是它，渲染最可靠）。
        // 不要用 LoadUIFont()：它可能返回动态 OS 字体，在 ugui Text 组件里有时不渲染。
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(font == null)
            font = LoadUIFont();
        Debug.Log("[CoinManager] 金币字体: " + (font != null ? font.name : "NULL"));
        coinText.font = font;
        coinText.fontSize = 40;
        coinText.color = Color.yellow;
        coinText.alignment = TextAnchor.MiddleRight;
        coinText.horizontalOverflow = HorizontalWrapMode.Overflow;
        coinText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rt = coinText.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -62f); // 血条下方
        rt.sizeDelta = new Vector2(300f, 30f);

        Refresh();

    }



    void Refresh()
    {

        if(coinText != null)
            coinText.text = Coins.ToString("D3");

    }

}