using UnityEngine;
using System.Collections;


public class PlayerAttack : MonoBehaviour
{

    //==============================
    // Inspector拖入
    //==============================

    public Transform weaponPivot;

    public GameObject weaponSprite;

    //==============================
    // 攻击动画（精灵帧）
    //==============================

    // 攻击动画帧：按顺序播放形成攻击特效（对应 Attack_Left_Right.anim 的 11 帧）。
    // 播放期间会临时禁用 PlayerDirection，避免它把精灵覆盖成面向精灵。
    public Sprite[] attackSprites;

    private SpriteRenderer sr;          // 玩家自身的 SpriteRenderer
    private PlayerDirection dirCtrl;    // 方向精灵控制器（攻击期间禁用）
    private SpriteRenderer weaponSr;    // 剑的 SpriteRenderer（攻击期间隐藏剑，只保留命中碰撞体）


    //==============================
    // 攻击参数
    //==============================

    // 攻击动画时长（秒）
    public float attackSpeed = 0.15f;

    // 攻击结束冷却
    public float attackCooldown = 0.3f;



    //==============================
    // 状态
    //==============================

    public bool attacking = false;

    private bool canAttack = true;

    void Start()
    {

        // 游戏开始隐藏剑（攻击改为播放动画，不再显示挥剑）
        if(weaponSprite != null)
        {
            weaponSprite.SetActive(false);
            weaponSr = weaponSprite.GetComponent<SpriteRenderer>();
        }

        sr = GetComponent<SpriteRenderer>();
        dirCtrl = GetComponent<PlayerDirection>();
    }



    void Update()
    {

        // 暂停时禁止攻击
        if(Time.timeScale == 0f)
            return;

        // 点击攻击
        if(Input.GetMouseButtonDown(0)
            && canAttack)
        {

            StartCoroutine(PlayAttackAnimation());

        }

    }

    IEnumerator PlayAttackAnimation()
    {

        attacking = true;

        canAttack = false;

        // 隐藏剑的显示（攻击以动画呈现，不再显示挥剑）
        if(weaponSr != null)
            weaponSr.enabled = false;

        // 沿用上一套攻击逻辑：激活剑的触发碰撞体（WeaponAttack 命中判定），
        // 让攻击动画期间能对敌人造成伤害；剑的显示已隐藏，只保留动画帧。
        if(weaponSprite != null)
            weaponSprite.SetActive(true);

        // 暂停方向控制器，避免它每帧把精灵覆盖成面向精灵，干扰动画帧
        if(dirCtrl != null)
            dirCtrl.enabled = false;

        // 记录攻击前的精灵，攻击结束后恢复，避免攻击帧大图残留
        Sprite prevSprite = (sr != null ? sr.sprite : null);

        // 判断攻击朝向：鼠标在玩家右侧 → 向右攻击 → 播放镜像版；
        // 鼠标在左侧 → 向左攻击 → 正常播放。
        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;
        bool facingRight = (mousePosition.x - transform.position.x) >= 0f;
        if(sr != null)
            sr.flipX = facingRight;

        // 按顺序播放攻击精灵帧
        if(sr != null
            && attackSprites != null
            && attackSprites.Length > 0)
        {

            // 攻击帧图片通常比玩家正常精灵大，直接换上去人物会"变大"。
            // 因此记录玩家正常精灵的本地尺寸与基准缩放，播放每帧时按比例
            // 缩放玩家，使该攻击帧的显示尺寸与正常精灵一致，播完再恢复。
            Vector3 baseScale = transform.localScale;
            Vector2 normalLocalSize = (sr.sprite != null
                ? sr.sprite.bounds.size
                : Vector2.one);

            float frameTime = attackSpeed / attackSprites.Length;

            foreach(Sprite sp in attackSprites)
            {

                if(sp != null)
                {
                    sr.sprite = sp;

                    // 按高度比例均匀缩放，让攻击帧高度与玩家正常精灵一致
                    float scaleFactor = normalLocalSize.y / sp.bounds.size.y;
                    transform.localScale = baseScale * scaleFactor;
                }

                yield return new WaitForSeconds(frameTime);

            }

            // 恢复玩家原始缩放
            transform.localScale = baseScale;

        }
        else
        {

            // 未配置攻击帧时，按原时长停顿以保持攻击节奏
            yield return new WaitForSeconds(attackSpeed);

        }

        // 攻击结束：关闭剑的触发碰撞体，停止造成伤害，并恢复剑的显示状态
        if(weaponSprite != null)
            weaponSprite.SetActive(false);

        // 复位镜像翻转：方向精灵本身已是分好左右的图，不需要 flipX
        if(sr != null)
        {
            sr.flipX = false;
            // 恢复攻击前的方向精灵，避免残留攻击帧大图导致玩家突然变大
            if(prevSprite != null)
                sr.sprite = prevSprite;
        }

        // 恢复方向控制器：根据鼠标方向把玩家精灵恢复为面向精灵
        if(dirCtrl != null)
            dirCtrl.enabled = true;

        // 等待攻击CD
        yield return new WaitForSeconds(attackCooldown);

        attacking = false;

        canAttack = true;

    }

}