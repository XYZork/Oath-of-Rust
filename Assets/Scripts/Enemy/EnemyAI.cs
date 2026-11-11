using UnityEngine;
using UnityEngine.AI;


public class EnemyAI : MonoBehaviour
{


    public Transform player;


    public float moveSpeed = 2f;

    // NavMeshAgent 的半径。<=0 时自动按物理碰撞体大小计算（推荐，Boss 也会正确适配）；
    // 手动设 >0 可覆盖（例如想要更宽的绕障余量）。
    public float agentRadius = 0f;

    // 自动计算半径时额外附加的绕障余量（只在 agentRadius<=0 时生效）。
    // agent 半径必须比身体碰撞体半尺寸更大，寻路路径才能与障碍保持足够间距，
    // 否则身体边缘会蹭到障碍的尖角/端部（如 EnemyRoom 里旋转 30° 的
    // TableAndChairs 桌角），被物理引擎顶住 → 敌人在空地上"卡住"（agent 想走、
    // desiredVelocity 满速，但刚体每帧 MovePosition 都被碰撞推回）。
    public float radiusMargin = 0.35f;


    //受击硬直控制

    public bool canMove = true;

    // 是否激活追击。普通敌人默认 true 立即追；
    // Boss 用则设为 false，待玩家进入 BossRoom 后由
    // BossRoomController 调用 Activate() 才开始朝主角移动。
    public bool startChasing = true;


    private EnemyAttack attack;

    private NavMeshAgent agent;

    // 物理刚体：敌人是 Dynamic Rigidbody2D，必须用 MovePosition 物理移动，
    // 否则直接用 transform.position 硬写位置会绕过物理碰撞，导致
    // 1) 推玩家时把玩家硬挤进墙 2) 撞障碍物时直接穿透（不避障）。
    private Rigidbody2D body;

    // 激活后首帧是否已把 agent 精确对齐到自身位置。
    // AddComponent<NavMeshAgent>() 时 agent 会自动 placed 到最近的
    // NavMesh 点，可能与自身位置有偏移；若直接读取 nextPosition，
    // 敌人会瞬移到该点。因此首帧必须强制 Warp 对齐后才开始寻路。
    private bool initialized = false;



    void Start()
    {
        attack = GetComponent<EnemyAttack>();
        agent = GetComponent<NavMeshAgent>();
        body = GetComponent<Rigidbody2D>();

        // 保险：若序列化的 player 引用为空（例如玩家死亡/复活后引用失效），
        // 按标签重新查找主角，避免敌人因 player==null 而永远站桩不动。
        if(player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if(p != null)
                player = p.transform;
        }

        // 敌人绘制层级：场景中门/背景/敌人 sortingOrder 都是 0 且同 z。
        // 敌人的 Dynamic Rigidbody2D 与 NavMeshAgent 硬写位置冲突时，
        // 敌人可能被物理引擎推入门(不透明黑色精灵)的碰撞箱内，被门盖住而"隐藏"。
        // 提高敌人排序层级，保证敌人始终可见（门/背景仍在下层）。
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if(sr != null && sr.sortingOrder <= 0)
            sr.sortingOrder = 10;

        // 注意：这里不立即 AddComponent<NavMeshAgent>()。
        // 若此时 NavMesh 尚未烘焙完成，Unity 会报
        // "Failed to create agent because there is no valid NavMesh"（agent 无效）。
        // 改为在 Update 中等待 NavMesh2DSetup.IsReady 后再创建（见 EnsureAgent）。
    }

    // NavMesh 就绪后再创建并配置 agent，避免在无 NavMesh 时 AddComponent 报错。
    void EnsureAgent()
    {
        if(agent != null)
            return;

        // 关键：AddComponent<NavMeshAgent>() 时 agent 会用物体当前的
        // transform.position 就近寻找 NavMesh。但我们的 NavMesh 烘焙在
        // y=0.1 平面，而 2D 物体的 world X/Y 分别对应 NavMesh 的 X/Z，
        // 物体自身的 y 是屏幕纵坐标（例如 Boss 在 y=14.9），离 NavMesh
        // 平面太远，AddComponent 会自动放置失败并报
        // "Failed to create agent because it is not close enough to the NavMesh"，
        // 导致 agent 未正确 placed、路径不仿真 = 移动慢 + 避障失效。
        // 因此创建前先把物体临时放到当前 2D 位置对应的 NavMesh 3D 坐标上。
        Vector3 pos2D = transform.position;
        Vector3 navPos = NavMesh2DSetup.ToNav3D(pos2D);
        // 注意：必须用 transform.position 临时放置（NavMeshAgent 用 3D
        // transform 位置找 NavMesh）；不能用 body.position，因为
        // Rigidbody2D.position 只认 2D 的 x/y，会把刚体错误移到 y=0.1。
        transform.position = navPos;

        agent = gameObject.AddComponent<NavMeshAgent>();

        // 2D 寻路配置：不自动旋转、由本脚本同步位置
        agent.speed = moveSpeed;
        agent.acceleration = 8f;
        agent.angularSpeed = 360f;
        // 关键：agent 半径必须 >= 敌人物理碰撞体的实际半径。
        // 若 agent 半径过小，寻路路径只预留很窄的余量，而实际身体更宽，
        // 绕行障碍物（如 BossRoom 中间的 Cactus）时身体会蹭到障碍物被物理引擎
        // 推离 NavMesh，导致 agent.isOnNavMesh 变 false、敌人卡住/抽搐。
        // 因此按碰撞体大小自动计算半径（含 Boss 这种大碰撞体），保证绕障余量。
        agent.radius = ComputeAgentRadius();
        agent.height = 1f;
        agent.baseOffset = 0f;
        agent.updateRotation = false;

        // 注意：必须保持默认 true（XZ 平面导航）。
        // 我们的 NavMesh 由 NavMesh2DSetup 烘焙在 XZ 平面（y=0.1 恒定），
        // 若设为 false（XY 平面导航）会与烘焙平面错位，敌人将无法寻路
        agent.updateUpAxis = true;
        agent.updatePosition = false;
        agent.autoBraking = true;

        // 创建完成：把 agent 精确放置到当前 2D 位置对应的 NavMesh 点，
        // 再恢复物体回 2D 坐标（X/Y 平面，z=0）。
        // 这样 agent 从创建起就 isOnNavMesh，后续路径仿真与避障才有效。
        NavMeshHit hit;
        if(NavMesh.SamplePosition(navPos, out hit, 6f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);

            // 关键：若物体起始位置就在障碍物上（NavMesh 不可行走区域，
            // 如 Enemy 恰好放在 EnemyRoom 中间的 TableAndChairs 上），
            // Warp 会把 agent 放到旁边的可行走点，但物体若仍恢复回障碍内的
            // 原始位置，agent 与物体就会脱节 → 敌人永远卡住不动。
            // 因此 Warp 成功后，把物体也同步到采样到的可行走点（2D 坐标）。
            Vector3 walkable2D = NavMesh2DSetup.FromNav3D(hit.position);
            walkable2D.z = 0f;
            transform.position = walkable2D;
            if(body != null)
                body.position = walkable2D;
        }
        else
        {
            // 采样失败：无法确定可行走点，退回到原始 2D 位置，
            // 交给后续 TryWarp 反复尝试恢复。
            transform.position = pos2D;
            if(body != null)
                body.position = pos2D;
        }
    }

    // 计算 NavMeshAgent 半径。
    // - 手动设置了 agentRadius(>0) 时直接使用；
    // - 否则按物理碰撞体的半宽/半高取较大值，再加绕障余量 radiusMargin，
    //   保证 agent 有足够绕障余量，避免身体蹭到障碍物尖角被物理引擎推离 NavMesh
    //   （Boss 的大碰撞体也会正确适配）。
    float ComputeAgentRadius()
    {
        if(agentRadius > 0f)
            return agentRadius;

        Collider2D col = GetComponent<Collider2D>();
        if(col != null)
        {
            float halfW = col.bounds.extents.x;
            float halfH = col.bounds.extents.y;
            float r = Mathf.Max(halfW, halfH);
            if(r > 0.05f)
                return r + radiusMargin;
        }
        return 0.5f + radiusMargin;
    }



    void Update()
    {


        // NavMesh 尚未烘焙完成时安全等待，避免 Warp 报
        // "Failed to create agent because there is no valid NavMesh"

        if(!NavMesh2DSetup.IsReady)
        {
            LogDiag("等待NavMesh", false);
            return;
        }

        // NavMesh 就绪后再创建 agent，避免在无 NavMesh 时 AddComponent 报错
        if(agent == null)
        {
            EnsureAgent();
            if(agent == null)
            {
                LogDiag("agent创建失败", false);
                return;
            }
        }

        // 房间未激活时（Boss 用 startChasing=false）：不追击、不寻路，
        // 只保持静止等待 BosssRoomController.Activate() 开启。
        if(!startChasing)
        {
            StopMoving();
            return;
        }

        // 受击硬直 / 攻击僵直时停止移动

        if(!canMove
            || player == null
            || (attack != null
                && attack.attacking))
        {
            LogDiag($"停止移动: canMove={canMove} playerNull={player == null} attacking={(attack != null ? attack.attacking : false)}", false);

            StopMoving();

            return;

        }


        if(agent == null
            || !agent.enabled)
        {
            LogDiag($"agent不可用: null={agent == null} enabled={(agent != null ? agent.enabled : false)}", false);
            return;
        }

        // 首帧强制对齐：把 agent 精确 Warp 到自身位置，
        // 防止 AddComponent 时自动 placed 的偏移被读取后造成瞬移。
        // 对齐成功前不读 nextPosition（每帧重试）

        if(!initialized)
        {

            bool warpOk = TryWarp();
            LogDiag($"首帧对齐: warpOk={warpOk} isOnNavMesh={(agent != null ? agent.isOnNavMesh : false)}", false);
            if(warpOk)
                initialized = true;

            return;

        }

        // agent 异常（被停止暂停 / 脱离 NavMesh）时，强制重新对齐。
        // 注意：不能销毁重建 agent，运行时反复 AddComponent/Destroy
        // NavMeshAgent 会让 NavMesh 系统不稳定，导致敌人异常消失。

        // 注意：isStopped 只能在 agent 已放置到 NavMesh 上时访问，
        // 必须先判断 !isOnNavMesh（短路），否则会抛
        // "IsStopped can only be called on an active agent that has been placed on a NavMesh"。
        if(!agent.isOnNavMesh
            || agent.isStopped)
        {
            // 注意：此处 agent 可能不在 NavMesh 上，不能访问 isStopped（会抛异常），
            // 只打印安全的 isOnNavMesh。
            bool before = agent.isOnNavMesh;
            LogDiag($"agent异常: isOnNavMesh={before}", false);

            if(!TryWarp())
            {
                // 诊断：连重新对齐都失败时，把当前位置与采样结果打出来
                NavMeshHit diagHit;
                bool sampled = NavMesh.SamplePosition(
                    NavMesh2DSetup.ToNav3D(transform.position), out diagHit, 2f, NavMesh.AllAreas);
                LogDiag($"恢复失败! onNavMesh={agent.isOnNavMesh} 采样={sampled} 采样点={diagHit.position}", true);
                return;
            }

        }


        agent.isStopped =
        false;


        // ============================================================
        // updatePosition = false 时的单向同步（关键！）
        //
        // agent 不会自动移动 transform，但它内部会沿寻路路径持续仿真
        // （推进 nextPosition，含避障）。我们只在 FixedUpdate 里把刚体
        // 移到 agent.nextPosition，再把 agent 锚定回刚体实际位置。
        //
        // 注意：这里绝不能每帧用 agent.nextPosition = transform.position
        // 覆盖 agent 的仿真位置！因为 transform.position 只在 FixedUpdate
        // 更新，渲染帧里覆盖会把 agent 的路径推进压平，
        // 导致避障(RVO)失效 → 敌人只走直线、撞障碍物。
        // 让它自由仿真，由 FixedUpdate 消费即可。
        // ============================================================

        // 瞬移保护：若 agent 状态异常（如复活重进场景后 agent 被 placed 到
        // 远处的 NavMesh 点），nextPosition 会与本帧位置相差很大，
        // 直接应用会把敌人瞬移出屏幕（表现为“隐身”但仍能攻击）。
        // 因此只允许“合理步长”内的推进，异常时强制重新对齐。
        //
        // 注意：因为 FixedUpdate 不再把 agent.nextPosition 锚定回刚体，agent
        // 会自由仿真推进。当敌人追上玩家时，agent 的 nextPosition 会推进到
        // 玩家中心，而刚体被玩家碰撞体顶住（两者中心差约 0.74 = 敌人半宽 +
        // 玩家半宽），属于“正常追到位”。因此阈值必须大于该值，否则会把
        // 正常追到玩家的敌人反复拉回、在玩家身旁抖动。
        //
        // 阈值取 speed*deltaTime + 1.0：既能容忍“追到玩家被玩家碰撞体挡住”
        // 的正常差距，又能抓住 agent 被异常 placed 到远处的瞬移。
        Vector3 next2D = NavMesh2DSetup.FromNav3D(agent.nextPosition);
        next2D.z = 0f;
        Vector3 enemyPos2D = transform.position;
        enemyPos2D.z = 0f;
        float step = Vector3.Distance(enemyPos2D, next2D);
        if (step > agent.speed * Time.deltaTime + 1.0f)
        {
            if (!TryWarp())
                return;
        }

        // 设置寻路目标：主角
        agent.SetDestination(
            NavMesh2DSetup.ToNav3D(
                player.position
            )
        );

        // 临时诊断：正常寻路时也定期输出位置与 agent 状态，追踪卡住原因
        LogDiag(
            $"运行中 body={body.position} 目标={player.position} 距离={Vector2.Distance(body.position, player.position):F2} "
            + $"hasPath={agent.hasPath} pathPending={agent.pathPending} "
            + $"desired={agent.desiredVelocity} nextPos={agent.nextPosition}",
            false
        );

    }



    void StopMoving()
    {

        if(agent != null
            && agent.isOnNavMesh)
        {

            agent.isStopped =
            true;

        }

    }

    // 物理移动：用 Rigidbody2D.MovePosition 沿 agent 推进方向移动。
    //
    // 采用 Unity 官方推荐的 NavMeshAgent + Rigidbody 单向同步方式：
    //   agent 内部会沿寻路路径（含转弯 + RVO 避障）持续推进 nextPosition，
    //   我们每物理帧把刚体朝 agent.nextPosition 移动（限制步长）。
    //
    // ★ 关键：绝不能把 agent.nextPosition 锚定回刚体实际位置！
    //   若每帧把 nextPosition 重置为刚体位置，agent 的路径推进与 RVO
    //   （agent 间的局部避碰）会被永久压平：刚体一旦被物理碰撞顶住
    //   （例如被另一个敌人挡住、或蹭到障碍物边角），agent 就永远无法
    //   推进出新的绕行方向 → 敌人原地卡死；Boss 则表现为只走直线、
    //   撞上 Cactus 不避障、且移动缓慢（刚体被障碍顶住原地挣扎）。
    //   正确做法：让 agent 自由仿真推进 nextPosition，刚体跟随即可；
    //   被物理阻挡导致二者差距拉大时，由 Update 里的瞬移保护 TryWarp 兜底。
    //
    // 相比直接读 desiredVelocity，用 nextPosition 更稳健：
    //   即使个别 agent 的 desiredVelocity 因状态异常恒为 0，
    //   只要它沿路径推进，nextPosition 就会前进，刚体仍能移动。
    //
    // 关键点：
    //   1) 步长限制 ≤ speed*fixedDeltaTime → 速度恒定、不震荡、不瞬移
    //   2) MovePosition 走物理碰撞 → 不穿透墙、不把主角硬挤进墙
    void FixedUpdate()
    {
        if(body == null || !canMove)
            return;

        // agent 尚未就绪 / 被停止 / 脱离 NavMesh / 未激活追击时，不移动。
        // isOnNavMesh 必须放在 isStopped 之前判断（短路），
        // 否则未放置到 NavMesh 时访问 isStopped 会抛异常。
        if(agent == null
            || !agent.enabled
            || !agent.isOnNavMesh
            || agent.isStopped
            || !startChasing
            || player == null
            || (attack != null && attack.attacking))
            return;

        // agent 内部沿路径仿真推进到的位置（含寻路转弯 + RVO 避障）
        Vector3 agentPos = agent.nextPosition;
        Vector2 bodyPos = body.position;

        // 本物理帧要移动的 XZ 位移 = agent 推进位置 - 刚体当前位置
        Vector2 delta = new Vector2(agentPos.x - bodyPos.x, agentPos.z - bodyPos.y);

        // 限制每步位移 ≤ speed*fixedDeltaTime：速度恒定、不震荡、不瞬移
        float maxStep = agent.speed * Time.fixedDeltaTime;
        if(delta.sqrMagnitude > maxStep * maxStep)
            delta = delta.normalized * maxStep;

        if(delta.sqrMagnitude < 0.000001f)
            return;

        body.MovePosition(bodyPos + delta);

        // 不要在这里把 agent.nextPosition 锚定回刚体！
        // 让 agent 自由仿真推进（否则避障/RVO 被压平，见方法头注释）。
    }

    // 玩家进入 BossRoom 后由 BossRoomController 调用，开启 Boss 追击。
    public void Activate()
    {
        startChasing = true;
    }



    // 安全地把 agent 放置到当前 2D 位置；NavMesh 不可用时返回 false
    // （不抛异常，避免 "Failed to create agent because there is no valid NavMesh"）

    // 临时诊断：限制打印频率，避免刷屏
    private float nextDiagTime = 0f;
    void LogDiag(string msg, bool force)
    {
        if(force || Time.time >= nextDiagTime)
        {
            Debug.Log($"[EnemyAI][{name}] {msg} agent={(agent != null ? agent.isOnNavMesh : false)} pos={transform.position}");
            nextDiagTime = Time.time + 1f;
        }
    }

    bool TryWarp()
    {

        if(agent == null
            || !agent.enabled)
            return false;

        Vector3 navPos = NavMesh2DSetup.ToNav3D(transform.position);

        // 若当前 2D 位置不在 NavMesh 上（例如出生点落在未烘焙地面/被障碍覆盖，
        // 或绕行中间障碍物时被挤到不可行走位置），agent.Warp 会失败返回 false，
        // 敌人会永久卡住（表现为"卡在房间中间不动"）。
        // 因此先尝试在本地采样附近(2 单位内)的可行走点；若本地没有，
        // 回退到主角附近采样——主角永远站在可行走地面上，保证能把 agent 放上去。
        NavMeshHit hit;
        bool found = NavMesh.SamplePosition(navPos, out hit, 2f, NavMesh.AllAreas);

        if(!found && player != null)
        {
            LogDiag("本地无可行走点，回退到主角附近采样", true);
            found = NavMesh.SamplePosition(
                NavMesh2DSetup.ToNav3D(player.position), out hit, 3f, NavMesh.AllAreas);
        }

        if(!found)
            return false;

        try
        {

            agent.Warp(hit.position);

            if(agent.isOnNavMesh)
            {
                // 同步 2D 物体到采样到的可行走点，避免物体残留在不可行走位置。
                // 这是异常恢复（瞬移复位），直接设置 transform/刚体位置即可。
                Vector3 walkable2D = NavMesh2DSetup.FromNav3D(hit.position);
                walkable2D.z = 0f;
                transform.position = walkable2D;
                if(body != null)
                    body.position = walkable2D;
            }

            return agent.isOnNavMesh;

        }
        catch
        {

            return false;

        }

    }


}
