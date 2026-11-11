using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    //==============================
    // 目标
    //==============================
    public Transform player;            // 玩家目标（Inspector 拖入，或自动从 EnemyAI 读取）

    //==============================
    // 攻击参数
    //==============================
    public float attackIntervalMin = 0.6f; // 攻击间隔最小值（秒）
    public float attackIntervalMax = 1.5f; // 攻击间隔最大值（秒）
    public int damage = 1;              // 伤害值
    public float attackRange = 0.75f;    // 玩家在多大距离内会发起攻击
    public float attackAngle = 60f;     // 挥击角度范围
    public float attackSpeed = 0.2f;    // 挥击时长（秒）
    public float weaponLength = 1f;     // 武器长度
    public float weaponWidth = 0.3f;    // 武器宽度

    //==============================
    // 状态
    //==============================
    private float timer;
    private float nextAttackInterval;   // 本次攻击间隔（随机得出）
    public bool attacking = false;      // 攻击中（用于攻击僵直，攻击时不能移动）
    private EnemyAI enemyAI;
    private GameObject pivotGO;         // 当前挥击特效的枢轴（用于死亡时清理）

    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        if (player == null && enemyAI != null)
            player = enemyAI.player;

        // 初始随机一个攻击间隔
        RollNextInterval();
    }

    // 在 0.6 ~ 1.5 秒范围内随机下一次攻击间隔
    void RollNextInterval()
    {
        nextAttackInterval = Random.Range(attackIntervalMin, attackIntervalMax);
    }

    void Update()
    {
        // 暂停时（Time.timeScale == 0）不攻击
        if (Time.timeScale == 0f)
            return;

        if (player == null || attacking)
            return;

        timer += Time.deltaTime;

        // 到达随机攻击间隔时触发一次攻击
        if (timer >= nextAttackInterval)
        {
            float dist = Vector2.Distance(player.position, transform.position);
            if (dist <= attackRange)
            {
                timer = 0f;
                RollNextInterval(); // 每次攻击后重新随机间隔
                StartCoroutine(Swing());
            }
        }
    }

    // 类似主角的挥击逻辑：锁定玩家方向 + 旋转挥动 + 碰撞检测
    IEnumerator Swing()
    {
        attacking = true;

        // 锁定攻击方向（朝向玩家）
        Vector2 dir = player.position - transform.position;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 创建临时武器枢轴（位于敌人中心）
        GameObject pivotGO = new GameObject("EnemyWeaponPivot");
        pivotGO.transform.position = transform.position;
        this.pivotGO = pivotGO;

        // 武器本体：红色条状 + Trigger 碰撞体
        GameObject weapon = new GameObject("EnemyWeapon");
        SpriteRenderer sr = weapon.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWhiteSprite();
        sr.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        sr.sortingOrder = 50;

        BoxCollider2D col = weapon.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(weaponLength, weaponWidth);

        EnemyWeaponHit hit = weapon.AddComponent<EnemyWeaponHit>();
        hit.damage = damage;
        hit.owner = gameObject;

        weapon.transform.SetParent(pivotGO.transform, false);
        weapon.transform.localPosition = new Vector3(weaponLength * 0.5f, 0f, 0f);

        // 挥击起点与终点
        Quaternion startRotation = Quaternion.Euler(0, 0, baseAngle - attackAngle * 0.5f);
        Quaternion endRotation = Quaternion.Euler(0, 0, baseAngle + attackAngle * 0.5f);
        pivotGO.transform.rotation = startRotation;

        float t = 0f;
        while (t < attackSpeed)
        {
            t += Time.deltaTime;

            float k = Mathf.SmoothStep(0, 1, t / attackSpeed);
            pivotGO.transform.rotation = Quaternion.Lerp(startRotation, endRotation, k);

            yield return null;
        }

        pivotGO.transform.rotation = endRotation;

        // 销毁临时武器
        Destroy(pivotGO);
        this.pivotGO = null;

        attacking = false;
    }

    // 敌人被击败销毁时，确保挥击特效一并消失
    void OnDestroy()
    {
        if (pivotGO != null)
            Destroy(pivotGO);
    }

    Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
