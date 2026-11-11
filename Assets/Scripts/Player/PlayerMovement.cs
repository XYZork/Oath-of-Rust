using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    Rigidbody2D rb;

    Vector2 movement;

    PlayerAttack attack;

    Collider2D bodyCol;

    // 墙滑动检测缓存的对象（避免每帧分配）
    RaycastHit2D[] slideHits = new RaycastHit2D[1];



    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        attack = GetComponent<PlayerAttack>();
        bodyCol = GetComponent<Collider2D>();
    }


    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement.Normalize();
    }


    void FixedUpdate()
    {
        // 攻击僵直：攻击时不能移动
        if (attack != null && attack.attacking)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 墙滑动：把速度拆成 x / y 两个轴，分别检测该轴方向是否被墙挡住。
        // 若被挡住则清零该轴速度、保留另一轴速度，让玩家能贴着墙滑动；
        // 否则当敌人/Boss 把玩家压到墙边时，玩家 velocity 仍持续指向墙内，
        // 物理引擎反复把玩家推回，导致在墙边抽搐抖动。
        Vector2 vel = movement * speed;
        vel.x = SlideAxis(vel.x, Vector2.right);
        vel.y = SlideAxis(vel.y, Vector2.up);
        rb.velocity = vel;
    }

    // 若沿 axisDir 方向即将撞到墙，则把该轴速度清零（贴墙滑动），否则原样返回。
    float SlideAxis(float axisSpeed, Vector2 axisDir)
    {
        if (Mathf.Abs(axisSpeed) < 0.0001f || bodyCol == null)
            return axisSpeed;

        float sign = Mathf.Sign(axisSpeed);
        // 只需检测本帧将要走过的距离（加一点余量），不会误伤远处的障碍
        float dist = Mathf.Abs(axisSpeed) * Time.fixedDeltaTime + 0.02f;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.useLayerMask = false;
        filter.useDepth = false;
        filter.useNormalAngle = false;

        int hits = bodyCol.Cast(axisDir * sign, filter, slideHits, dist);
        return hits > 0 ? 0f : axisSpeed;
    }
}