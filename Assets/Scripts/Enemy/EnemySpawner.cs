using UnityEngine;


public class EnemySpawner : MonoBehaviour
{


    public GameObject enemyPrefab;


    public int enemyCount = 3;


    public Transform[] spawnPoints;


    // 障碍检测参数

    public float checkRadius = 0.5f;

    public float maxSearchRadius = 5f;

    public float searchStep = 0.5f;




    public void Spawn()
    {


        for(int i=0;i<enemyCount
            && spawnPoints != null
            && i<spawnPoints.Length;i++)
        {


            // 在指定生成点生成，若该点与障碍物重叠则移到障碍物外

            Vector2 safePos =
            FindSafePosition(
                spawnPoints[i].position
            );


            Instantiate(
                enemyPrefab,
                safePos,
                Quaternion.identity
            );


        }


    }




    // 从期望位置找一个“不在障碍物内”的安全位置：
    // 若期望点安全则直接返回；否则螺旋搜索最近的安全点

    public static Vector2 FindSafePosition(
        Vector2 desired,
        float checkRadius = 0.5f,
        float maxSearchRadius = 5f,
        float searchStep = 0.5f
    )
    {

        if(!OverlapsStatic(desired, checkRadius))
            return desired;


        for(float radius = searchStep;
            radius <= maxSearchRadius;
            radius += searchStep)
        {

            int steps =
            Mathf.CeilToInt(
                2f * Mathf.PI * radius
                / searchStep
            );


            for(int i=0;i<steps;i++)
            {

                float angle =
                (360f / steps) * i;


                Vector2 candidate =
                desired
                + new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                ) * radius;


                if(!OverlapsStatic(candidate, checkRadius))
                    return candidate;

            }

        }


        // 未找到安全点，返回期望位置兜底

        return desired;

    }




    // 检测位置是否与静态障碍碰撞箱重叠
    // （跳过动态物体与触发器）

    static bool OverlapsStatic(
        Vector2 position,
        float checkRadius
    )
    {

        Collider2D[] hits =
        Physics2D.OverlapCircleAll(
            position,
            checkRadius
        );


        foreach(Collider2D hit in hits)
        {

            // 跳过动态物体（带 Rigidbody2D 的敌人/主角）

            if(hit.GetComponent<Rigidbody2D>() != null)
                continue;


            // 跳过触发器（玩家检测区等）

            if(hit.isTrigger)
                continue;


            return true; // 与静态障碍重叠

        }


        return false;

    }


}