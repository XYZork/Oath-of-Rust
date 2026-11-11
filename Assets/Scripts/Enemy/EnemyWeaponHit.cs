using UnityEngine;
using System.Collections.Generic;

public class EnemyWeaponHit : MonoBehaviour
{
    public int damage = 1;
    public GameObject owner; // 攻击者（敌人），用于避免攻击到自己

    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    void OnEnable()
    {
        hitTargets.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner)
            return;

        // 只对玩家生效
        if (!other.CompareTag("Player"))
            return;

        if (hitTargets.Contains(other.gameObject))
            return;

        hitTargets.Add(other.gameObject);

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
            playerHealth.TakeDamage(damage, knockbackDirection);
        }
    }
}
