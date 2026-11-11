using UnityEngine;
using System.Collections;

public class HitImpactEffect : MonoBehaviour
{
    //==============================
    // 特效参数
    //==============================
    public float duration = 0.3f;       // 特效持续时间（秒）
    public float ringMaxRadius = 0.8f;  // 冲击波最大扩散半径
    public int shardCount = 8;          // 飞散碎片数量
    public float shardDistance = 1.2f;  // 碎片飞散距离
    public Color effectColor = new Color(1f, 0.25f, 0.2f, 1f); // 特效主色（红）

    private GameObject effectRoot;      // 特效根节点

    // 受击时调用：在所在位置生成冲击波 + 碎片
    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlayEffect());
    }

    IEnumerator PlayEffect()
    {
        Cleanup();

        // 特效根节点（固定在受击位置）
        effectRoot = new GameObject("HitImpactEffect");
        effectRoot.transform.position = transform.position;

        // 冲击波圆环
        GameObject ringGO = new GameObject("HitRing");
        ringGO.transform.SetParent(effectRoot.transform, false);
        SpriteRenderer ringSr = ringGO.AddComponent<SpriteRenderer>();
        ringSr.sprite = CreateRingSprite();
        ringSr.color = effectColor;
        ringSr.sortingOrder = 60;

        // 飞散碎片（向四周均匀飞散）
        GameObject[] shards = new GameObject[shardCount];
        SpriteRenderer[] shardSrs = new SpriteRenderer[shardCount];
        Vector2[] shardDirs = new Vector2[shardCount];
        for (int i = 0; i < shardCount; i++)
        {
            float angle = (360f / shardCount) * i + Random.Range(-10f, 10f);
            shardDirs[i] = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject shard = new GameObject("HitShard");
            shard.transform.SetParent(effectRoot.transform, false);
            shardSrs[i] = shard.AddComponent<SpriteRenderer>();
            shardSrs[i].sprite = CreateWhiteSprite();
            shardSrs[i].color = effectColor;
            shardSrs[i].sortingOrder = 60;
            shard.transform.localScale = Vector3.one * 0.15f;
            shards[i] = shard;
        }

        // 特效动画：冲击波扩散淡出 + 碎片飞散缩小
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float ease = 1f - (1f - k) * (1f - k); // 加速扩散，更有冲击感

            // 冲击波：扩散 + 淡出
            ringGO.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, ringMaxRadius, ease);
            Color ringColor = effectColor;
            ringColor.a = Mathf.Lerp(0.9f, 0f, k);
            ringSr.color = ringColor;

            // 碎片：飞散 + 缩小 + 淡出
            for (int i = 0; i < shardCount; i++)
            {
                shards[i].transform.localPosition = shardDirs[i] * (shardDistance * ease);
                shards[i].transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.02f, k);
                Color shardColor = effectColor;
                shardColor.a = Mathf.Lerp(1f, 0f, k);
                shardSrs[i].color = shardColor;
            }

            yield return null;
        }

        Cleanup();
    }

    void Cleanup()
    {
        if (effectRoot != null)
        {
            Destroy(effectRoot);
            effectRoot = null;
        }
    }

    // 物体销毁时清理残留特效
    void OnDestroy()
    {
        Cleanup();
    }

    Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    // 程序化生成圆环纹理（白色，渲染时着色）
    Sprite CreateRingSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool ring = dist > 0.32f && dist < 0.48f;
                tex.SetPixel(x, y, ring ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
