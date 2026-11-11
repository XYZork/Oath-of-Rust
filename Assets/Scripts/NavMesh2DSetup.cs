using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;
using UnityEngine.SceneManagement;

// 2D 场景的 NavMesh 运行时构建器
// 原理：NavMesh 烘焙只识别 3D 碰撞体，此脚本在运行时把场景中的
//       2D 地面（SpriteRenderer）与障碍（Collider2D）镜像映射到 3D 空间，
//       再调用 NavMeshSurface 同步烘焙，供敌人 NavMeshAgent 寻路使用。
public class NavMesh2DSetup : MonoBehaviour
{
    public static NavMesh2DSetup Instance { get; private set; }

    // NavMesh 是否已成功烘焙（供 EnemyAI 等待）
    public static bool IsReady { get; private set; }

    [Tooltip("3D 镜像碰撞体的厚度（y 轴），太薄会导致烘焙失败")]
    public float colliderThickness = 0.1f;

    // 记录本次烘焙的容器与表面，用于跨场景时正确清理 NavMesh 数据
    private GameObject container;
    private NavMeshSurface surface;

    // 场景加载完成后自动创建构建器。
    // 用 sceneLoaded 事件保证每次场景切换（含复活重进 MainGame）都
    // 针对当前活动场景重新烘焙，避免 AfterSceneLoad 在编辑器场景切换时
    // 只在 backup 场景烘焙、MainGame 里没有有效 NavMesh 导致敌人不动。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // 首次场景已加载，直接创建
        CreateBarker();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 每次场景加载后都重建一次，确保烘焙当前活动场景
        CreateBarker();
    }

    // 销毁旧实例（清理数据）并创建新实例重新烘焙当前场景。
    // 复用旧实例时其 Start 协程已跑过一次、不会重新烘焙，故只能重建。
    static void CreateBarker()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
            IsReady = false;
        }
        GameObject go = new GameObject("NavMesh2DSetup");
        go.AddComponent<NavMesh2DSetup>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsReady = false;
    }

    // 延迟到下一帧再烘焙：保证场景所有对象的 Awake/Start 完成，
    // 且物理引擎完成一次步进、新创建的 3D 镜像碰撞体完全注册。
    // 避免同帧烘焙时 CollectSources 收集不到几何，
    // 导致 BuildNavMeshData 返回 null（no valid NavMesh）
    void Start()
    {
        StartCoroutine(BakeNextFrame());
    }

    IEnumerator BakeNextFrame()
    {
        yield return null; // 等一帧，确保物理引擎注册新碰撞体
        Physics.SyncTransforms();
        BuildNavMesh();
    }

    void BuildNavMesh()
    {
        // 3D 镜像容器：子物体坐标 (x, 0, z)，其中 x 对应 2D 的 x，z 对应 2D 的 y
        container = new GameObject("NavMesh2DContainer");

        int groundCount = 0;
        int obstacleCount = 0;

        // 1. 地面：静态 SpriteRenderer（无 Collider2D、无 Rigidbody2D 的场景背景）
        SpriteRenderer[] sprites = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SpriteRenderer sr in sprites)
        {
            if (!sr.gameObject.activeInHierarchy)
                continue;
            if (sr.GetComponent<Rigidbody2D>() != null)
                continue; // 动态物体（敌人、主角）不是地面
            if (sr.GetComponent<Collider2D>() != null)
                continue; // 带 2D 碰撞体的是墙/门等障碍，不是地面

            if (AddGround(container, sr))
                groundCount++;
        }

        // 2. 障碍：带 Collider2D 的静态对象（墙、门、家具等）
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Collider2D col in colliders)
        {
            // 触发器（房间触发区等）不是物理障碍，必须跳过！
            // 否则触发区会被烤成不可行走区域，导致该区域内的敌人
            // 激活时被 NavMeshAgent placed 到最近的可行走点（房间边缘），
            // 表现为"敌人直接传送到脸上"
            if (col.isTrigger)
                continue;
            if (col.GetComponent<Rigidbody2D>() != null)
                continue; // 跳过敌人/主角身上的碰撞体
            // 不排除 inactive 对象：门初始隐藏（activeInHierarchy=false），
            // 战斗开始才出现，需提前烤进 NavMesh，否则敌人会穿门追击

            if (AddObstacle(container, col))
                obstacleCount++;
        }

        // 3. 同步烘焙 NavMesh
        surface = container.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        // BuildNavMesh 只会把数据烘焙到 surface.navMeshData，
        // 并不会自动注册到全局 NavMesh 系统。运行时动态创建的 NavMeshSurface
        // 其 OnEnable（自动 AddData）在数据为空时已执行过，因此必须手动 AddData，
        // 否则 NavMeshAgent 找不到 NavMesh，敌人会报
        // "Failed to create agent because there is no valid NavMesh"。
        surface.BuildNavMesh();
        if (surface.navMeshData != null)
            surface.AddData();

        // 4. 诊断：确认烘焙是否成功（在场景原点采样，命中即 NavMesh 存在）
        IsReady = (surface.navMeshData != null);
        if (IsReady)
        {
            NavMeshHit hit;
            bool sampled = NavMesh.SamplePosition(
                new Vector3(0f, colliderThickness, 0f),
                out hit,
                1f,
                NavMesh.AllAreas
            );
            Debug.Log($"[NavMesh2DSetup] 烘焙成功: 地面 {groundCount} 个, 障碍 {obstacleCount} 个, 原点采样命中={sampled}");
        }
        else
        {
            Debug.LogError($"[NavMesh2DSetup] 烘焙失败! 地面 {groundCount} 个, 障碍 {obstacleCount} 个, navMeshData = null");
        }
    }

    // 场景卸载（如玩家死亡跳到 Fail 场景、复活后重进主场景）时，
    // 必须把本次烘焙的 NavMesh 数据从全局 NavMesh 系统中移除，
    // 并重置静态状态，否则新场景烘焙会复用/污染旧数据导致敌人寻路错乱。
    void OnDestroy()
    {
        if (surface != null)
        {
            surface.RemoveData();
            surface = null;
        }
        if (container != null)
        {
            Destroy(container);
            container = null;
        }
        if (Instance == this)
        {
            Instance = null;
            IsReady = false;
        }
    }

    bool AddGround(GameObject container, SpriteRenderer sr)
    {
        Bounds b = sr.bounds;
        if (b.size.x <= 0f || b.size.y <= 0f)
            return false;

        GameObject go = new GameObject("NavGround_" + sr.name);
        go.transform.SetParent(container.transform, false);
        // 碰撞体覆盖 y=0 ~ colliderThickness，顶面 = colliderThickness，
        // 与 ToNav3D 返回的高度一致，保证 agent 底部恰好贴住 NavMesh 表面
        go.transform.position = new Vector3(b.center.x, colliderThickness * 0.5f, b.center.y);

        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(b.size.x, colliderThickness, b.size.y);
        return true;
    }

    bool AddObstacle(GameObject container, Collider2D col)
    {
        Bounds b = col.bounds;
        if (b.size.x <= 0f || b.size.y <= 0f)
            return false;

        GameObject go = new GameObject("NavObstacle_" + col.name);
        go.transform.SetParent(container.transform, false);
        go.transform.position = new Vector3(b.center.x, colliderThickness * 0.5f, b.center.y);

        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(b.size.x, colliderThickness, b.size.y);

        // 标记为不可行走区域
        NavMeshModifier mod = go.AddComponent<NavMeshModifier>();
        mod.overrideArea = true;
        mod.area = 1; // 1 = Not Walkable
        return true;
    }

    // 2D 世界坐标 -> 3D NavMesh 坐标（y 取 NavMesh 表面高度）
    public static Vector3 ToNav3D(Vector3 pos2D)
    {
        float height = (Instance != null) ? Instance.colliderThickness : 0.1f;
        return new Vector3(pos2D.x, height, pos2D.y);
    }

    // 3D NavMesh 坐标 -> 2D 世界坐标
    public static Vector3 FromNav3D(Vector3 pos3D)
    {
        return new Vector3(pos3D.x, pos3D.z, 0f);
    }
}
