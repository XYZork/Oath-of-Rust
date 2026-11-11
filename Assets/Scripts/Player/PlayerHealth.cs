using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int health = 6;
    public float knockbackForce = 5f;
    public float invincibleTime = 0.5f; // 受击后的无敌时间，防止连续掉血

    private int maxHealth;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool invincible = false;

    // UI 血条（红底绿色条，固定左上角）
    private RectTransform fillBar;      // 绿色填充条
    private RectTransform barBG;        // 红色底条

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        maxHealth = Mathf.Max(1, health);

        // 确保金币系统存在（创建金币计数 UI）
        if(CoinManager.Instance == null)
        {
            GameObject coinGO = new GameObject("CoinManager");
            coinGO.AddComponent<CoinManager>();
        }
        // 玩家出生时恢复显示金币计数（可能刚从失败界面回来被隐藏）
        CoinManager.SetCoinUIVisible(true);

        CreateHealthBarUI();
        UpdateFill();
    }

    // 回满生命（商店药水效果）
    public void HealFull()
    {
        health = maxHealth;
        UpdateFill();
    }

    public void TakeDamage(int damage, Vector2 direction)
    {
        if (invincible)
            return;

        if (health <= 0)
            return;

        health -= damage;
        UpdateFill(); // 血条减少一个单位

        // 击退
        if (rb != null)
        {
            rb.velocity = direction * knockbackForce;
        }

        // 受击闪红
        StartCoroutine(HitFlash());

        // 受击特效（冲击波 + 碎片）
        HitImpactEffect hitEffect = GetComponent<HitImpactEffect>();
        if (hitEffect == null)
            hitEffect = gameObject.AddComponent<HitImpactEffect>();
        hitEffect.Play();

        // 0.5 秒受击冷却（无敌）
        StartCoroutine(InvincibleTimer());

        if (health <= 0)
        {
            Die();
        }
    }

    // 创建左上角血条：红色底 + 绿色填充
    void CreateHealthBarUI()
    {
        // 专属 Canvas（屏幕空间，永远显示在左上角）
        GameObject canvasGO = new GameObject("PlayerHealthCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 红色底条（左上角，pivot 左上）
        GameObject bgGO = new GameObject("BarBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.75f, 0.1f, 0.1f, 1f); // 红底
        barBG = bgGO.GetComponent<RectTransform>();
        barBG.anchorMin = new Vector2(0f, 1f);
        barBG.anchorMax = new Vector2(0f, 1f);
        barBG.pivot = new Vector2(0f, 1f);
        barBG.anchoredPosition = new Vector2(20f, -20f); // 左上角留边距
        barBG.sizeDelta = new Vector2(300f, 30f);

        // 绿色填充条（左对齐，满血时盖满红底，掉血时从右侧收缩露出红底）
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        Image fill = fillGO.AddComponent<Image>();
        fill.color = new Color(0.1f, 0.85f, 0.15f, 1f); // 绿条
        fillBar = fillGO.GetComponent<RectTransform>();
        fillBar.anchorMin = new Vector2(0f, 0f);
        fillBar.anchorMax = new Vector2(1f, 1f);
        fillBar.offsetMin = Vector2.zero;
        fillBar.offsetMax = Vector2.zero;
    }

    // 按当前血量更新绿色填充条宽度
    void UpdateFill()
    {
        if (fillBar == null)
            return;

        float ratio = Mathf.Clamp01((float)health / maxHealth);
        float bgWidth = barBG.rect.width;
        // 右侧收缩：露出红色底
        fillBar.offsetMax = new Vector2(-(1f - ratio) * bgWidth, 0f);
    }

    IEnumerator HitFlash()
    {
        if (spriteRenderer != null)
        {
            Color oldColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = oldColor;
        }
    }

    IEnumerator InvincibleTimer()
    {
        invincible = true;
        yield return new WaitForSeconds(invincibleTime);
        invincible = false;
    }

    void Die()
    {
        // 玩家死亡：禁用移动与攻击组件
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        PlayerAttack pa = GetComponent<PlayerAttack>();
        if (pa != null) pa.enabled = false;

        // 玩家死亡时重置金币
        CoinManager.ResetCoins();

        // 恢复时间后跳转到失败场景
        Time.timeScale = 1f;
        SceneManager.LoadScene("Fail");
    }
}