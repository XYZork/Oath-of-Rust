using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class BossRoomController : MonoBehaviour
{


    // Boss 房间的门（进入房间后封锁，Boss 死亡后打开）
    public GameObject door;


    // Boss 根对象（放在 BossRoom 里的那个，含 SpriteRenderer）
    public GameObject boss;


    // Boss 自动寻路 / 行为参数（未修改时用默认值）
    public int bossHealth = 10;
    public float bossSpeed = 2f;


    // Boss 血条外观参数（配合复用的 EnemyHealthBar）
    public float barWidth = 1.6f;
    public float barHeight = 0.2f;
    public float barOffsetAbove = 1.2f;


    // Boss 攻击范围（中心点距离）。<=0 时按 Boss 实际碰撞体尺寸自动计算，
    // 保证玩家能被 Boss 身体挡住的距离之外也能触发攻击。
    public float bossAttackRange = 0f;

    // Boss 攻击后摇（秒）：两次攻击之间的间隔。<=0 时用 EnemyAttack 默认值。
    public float bossAttackCooldown = 0f;


    // Boss 的自动寻路组件（运行时补齐）
    private EnemyAI bossAI;


    bool activated = false;


    bool completed = false;



    private void Start()
    {
        // 自动生成覆盖房间的触发区（若本物体上还没有碰撞体）
        EnsureTrigger();

        // 游戏开始门隐藏
        if(door != null)
        {
            door.SetActive(false);
        }

        // 自动补齐 Boss 需要的组件（碰撞体、刚体、血量、自动寻路、攻击）
        SetupBoss();
    }



    // 自动生成覆盖房间的触发区（挂到 BossRoom 下的空子物体即可，
    // 尺寸按房间地面精灵生成；若本物体已有碰撞体则跳过）
    void EnsureTrigger()
    {
        if(GetComponent<Collider2D>() != null)
            return;

        BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        SpriteRenderer roomSR = GetComponentInParent<SpriteRenderer>();
        if(roomSR != null)
        {
            Vector3 s = transform.localScale;
            box.size = new Vector2(
                roomSR.bounds.size.x / (Mathf.Abs(s.x) > 0.0001f ? s.x : 1f),
                roomSR.bounds.size.y / (Mathf.Abs(s.y) > 0.0001f ? s.y : 1f)
            );
        }
        else
        {
            box.size = new Vector2(6f, 7f);
        }
    }



    // 玩家进入房间：封锁门 + 让 Boss 开始追击
    private void OnTriggerEnter2D(Collider2D other)
    {

        if(other.CompareTag("Player") && !activated)
        {

            activated = true;

            StartRoom();

        }

    }



    void StartRoom()
    {

        // 出现门（封锁房间）
        if(door != null)
        {
            door.SetActive(true);
        }

        // Boss 开始朝主角移动（自动寻路）
        if(bossAI != null)
        {
            bossAI.Activate();
        }

    }



    // 给 Boss 补齐物理/行为组件，避免在场景里手动添加。
    // Boss 只用保证身上有 SpriteRenderer 即可。
    void SetupBoss()
    {

        if(boss == null)
            return;

        // 刚体（用于物理碰撞）
        Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
        if(rb == null)
        {
            rb = boss.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // 碰撞体（按可见精灵大小生成）
        Collider2D col = boss.GetComponent<Collider2D>();
        if(col == null)
        {
            SpriteRenderer sr = boss.GetComponent<SpriteRenderer>();
            BoxCollider2D box = boss.AddComponent<BoxCollider2D>();
            if(sr != null)
            {
                Vector3 s = boss.transform.localScale;
                box.size = new Vector2(
                    sr.bounds.size.x / (Mathf.Abs(s.x) > 0.0001f ? s.x : 1f),
                    sr.bounds.size.y / (Mathf.Abs(s.y) > 0.0001f ? s.y : 1f)
                );
            }
        }

        // 血量（死亡时 Destroy(gameObject)，门由此打开）
        if(boss.GetComponent<EnemyHealth>() == null)
        {
            EnemyHealth eh = boss.AddComponent<EnemyHealth>();
            eh.health = bossHealth;
        }

        // 血条：显示 Boss 剩余血量，跟随并悬浮在头顶
        if(boss.GetComponent<EnemyHealthBar>() == null)
        {
            EnemyHealthBar bar = boss.AddComponent<EnemyHealthBar>();
            bar.barWidth = barWidth;
            bar.barHeight = barHeight;
            bar.offsetAbove = barOffsetAbove;
        }

        // 自动寻路（startChasing=false：进房间后才追击）
        if(boss.GetComponent<EnemyAI>() == null)
        {
            EnemyAI ai = boss.AddComponent<EnemyAI>();
            ai.startChasing = false;
            ai.moveSpeed = bossSpeed;
            Transform p = FindPlayer();
            if(p != null)
                ai.player = p;
            bossAI = ai;
        }
        else
        {
            bossAI = boss.GetComponent<EnemyAI>();
        }

        // 攻击（Boss 会攻击主角）
        if(boss.GetComponent<EnemyAttack>() == null)
        {
            EnemyAttack atk = boss.AddComponent<EnemyAttack>();
            Transform p = FindPlayer();
            if(p != null)
                atk.player = p;

            // Boss 体型大，攻击范围必须覆盖其碰撞体半径 + 玩家半径，
            // 否则玩家被 Boss 身体挡住、永远进不了默认 0.75 的攻击范围。
            // 按 Boss 实际碰撞体尺寸自动计算，也可在 Inspector 用 bossAttackRange 覆盖。
            atk.damage = 2;
            atk.attackRange = ComputeBossAttackRange();
            atk.weaponLength = Mathf.Max(1.5f, atk.attackRange * 0.6f);
            atk.weaponWidth = 0.8f;
            // 自定义攻击后摇（攻击间隔固定为该值）；未设置则用 EnemyAttack 默认随机间隔
            if(bossAttackCooldown > 0f)
            {
                atk.attackIntervalMin = bossAttackCooldown;
                atk.attackIntervalMax = bossAttackCooldown;
            }
        }

    }



    // 计算 Boss 攻击范围（中心点距离）。
    // 默认 0.75 以普通敌人（小碰撞体）为基准；Boss 碰撞体很大，
    // 玩家在 Boss 身体边缘就被挡住，中心距无法 < 0.75。
    // 因此取 Boss 碰撞体在半轴上最大半径 + 玩家半径 + 余量。
    float ComputeBossAttackRange()
    {
        if(bossAttackRange > 0f)
            return bossAttackRange;

        Collider2D col = boss.GetComponent<Collider2D>();
        if(col != null)
        {
            float radius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
            return radius + 0.9f; // + 玩家半径(约0.4) + 余量
        }
        return 2f;
    }



    Transform FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        return (p != null) ? p.transform : null;
    }



    void Update()
    {

        if(!activated || completed)
            return;


        // Boss 被击杀（EnemyHealth.Destroy(gameObject)）后开门并显示结束画面
        if(boss == null)
        {

            completed = true;

            // 门隐藏 = 打开
            if(door != null)
            {
                door.SetActive(false);
            }

            // 显示全屏"序章完结"画面
            ShowEndingScreen();

        }

    }


    // 玩家击败 Boss 后显示全屏"序章完结"画面
    void ShowEndingScreen()
    {

        // 专属 Canvas（屏幕空间，永远显示在最上层）
        GameObject canvasGO = new GameObject("EndingCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 全不透明黑色遮罩（盖住整个画面）
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 1f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // 中央大字"序章完结"
        GameObject textGO = new GameObject("Title");
        textGO.transform.SetParent(canvasGO.transform, false);
        Text title = textGO.AddComponent<Text>();
        title.text = "序章完结";
        title.font = LoadCJKFont();
        title.fontSize = 64;
        title.color = new Color(1f, 1f, 1f, 0f); // 初始透明，配合淡入动画
        title.alignment = TextAnchor.MiddleCenter;
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rt = title.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 文字淡入
        StartCoroutine(FadeInText(title, 1.5f));

    }


    // "序章完结"文字淡入动画：从完全透明平滑过渡到不透明
    IEnumerator FadeInText(Text t, float duration)
    {

        float elapsed = 0f;
        while(elapsed < duration)
        {

            elapsed += Time.deltaTime;

            float alpha = Mathf.Clamp01(elapsed / duration);
            t.color = new Color(1f, 1f, 1f, alpha);

            yield return null;

        }

        t.color = new Color(1f, 1f, 1f, 1f);

    }


    // 加载能真正渲染中文的字体。
    // 不能用 CoinManager.LoadUIFont()：它可能返回动态 OS 字体在 ugui 里不渲染，
    // 或兜底到不含中文字形的内置字体，导致文字显示为空白。
    // 这里逐个尝试系统常见中文字体并验证字形，确保 "序章完结" 能显示。
    Font LoadCJKFont()
    {

        string[] candidates = new string[]
        {
            "PingFang SC",
            "Hiragino Sans GB",
            "Heiti SC",
            "Microsoft YaHei",
            "Noto Sans CJK SC",
            "Arial Unicode MS",
            "STHeiti",
            "SimHei"
        };

        foreach(string name in candidates)
        {
            try
            {
                Font f = Font.CreateDynamicFontFromOSFont(name, 64);
                if(f != null)
                {
                    f.RequestCharactersInTexture("序章完结", 64, FontStyle.Normal);
                    if(f.HasCharacter('序') && f.HasCharacter('章') && f.HasCharacter('完') && f.HasCharacter('结'))
                        return f;
                }
            }
            catch(System.Exception)
            {
                // 该字体创建失败，尝试下一个
            }
        }

        // 兜底：内置字体（可能不含中文字形，但不至于报错）
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }


}