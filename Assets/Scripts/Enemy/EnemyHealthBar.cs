using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("血条外观")]
    public float barWidth = 1.2f;      // 血条总宽度
    public float barHeight = 0.15f;    // 血条高度
    public float offsetAbove = 0.9f;   // 血条相对敌人中心向上偏移

    private EnemyHealth enemyHealth;
    private float maxHealth;
    private Transform barRoot;
    private Transform barFill;
    private float currentRatio = 1f;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            enabled = false;
            return;
        }
        maxHealth = Mathf.Max(1, enemyHealth.health);
        CreateBar();
    }

    void LateUpdate()
    {
        if (enemyHealth == null || barRoot == null)
            return;

        // 跟随敌人，始终悬浮在头顶
        barRoot.position = transform.position + new Vector3(0f, offsetAbove, 0f);

        // 根据当前血量更新红色填充条宽度（血量越低条越短，从左侧开始减少）
        float ratio = Mathf.Clamp01((float)enemyHealth.health / maxHealth);
        if (Mathf.Abs(ratio - currentRatio) > 0.001f)
        {
            currentRatio = ratio;
            barFill.localScale = new Vector3(barWidth * ratio, barHeight, 1f);
        }
    }

    void OnDestroy()
    {
        // 敌人被销毁时，一并删除血条
        if (barRoot != null)
            Destroy(barRoot.gameObject);
    }

    // 创建血条：黑色底色 + 红色填充
    void CreateBar()
    {
        barRoot = new GameObject("HealthBar_" + gameObject.name).transform;
        barRoot.position = transform.position + new Vector3(0f, offsetAbove, 0f);

        // 黑色底色
        SpriteRenderer bg = CreateQuad("BG", new Color(0f, 0f, 0f, 1f), barWidth, barHeight, new Vector2(0.5f, 0.5f));
        bg.transform.SetParent(barRoot, false);

        // 红色填充（左对齐，受击时从左侧收窄）
        SpriteRenderer fillSr = CreateQuad("Fill", new Color(1f, 0f, 0f, 1f), barWidth, barHeight, new Vector2(0f, 0.5f));
        fillSr.sortingOrder = 101; // 高于黑底，确保红色显示在黑底之上
        barFill = fillSr.transform;
        barFill.SetParent(barRoot, false);
        barFill.localPosition = new Vector3(-barWidth / 2f, 0f, 0f);
    }

    SpriteRenderer CreateQuad(string name, Color color, float width, float height, Vector2 pivot)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(CreateWhiteTexture(), new Rect(0, 0, 1, 1), pivot, 1f); // 1px = 1 世界单位
        sr.color = color;
        sr.sortingOrder = 100; // 保证血条显示在敌人与场景之上
        go.transform.localScale = new Vector3(width, height, 1f);
        return sr;
    }

    Texture2D CreateWhiteTexture()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return tex;
    }
}
