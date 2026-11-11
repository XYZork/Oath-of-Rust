using UnityEngine;
using System.Collections;



public class EnemyHealth : MonoBehaviour
{


    public int health = 3;


    public float knockbackForce = 5f;



    private Rigidbody2D rb;


    private SpriteRenderer spriteRenderer;


    private EnemyAI enemyAI;




    void Start()
    {

        rb =
        GetComponent<Rigidbody2D>();


        spriteRenderer =
        GetComponent<SpriteRenderer>();


        enemyAI =
        GetComponent<EnemyAI>();

    }





    public void TakeDamage(
        int damage,
        Vector2 direction
    )
    {


        health -= damage;



        //击退

        if(rb != null)
        {

            rb.velocity =
            direction *
            knockbackForce;

        }



        //受击闪白

        StartCoroutine(
            HitFlash()
        );

        //受击特效（冲击波 + 碎片）

        HitImpactEffect hitEffect =
        GetComponent<HitImpactEffect>();

        if(hitEffect == null)
        {
            hitEffect =
            gameObject.AddComponent<HitImpactEffect>();
        }

        hitEffect.Play();

        //受击硬直

        StartCoroutine(
            Stun()
        );



        if(health <= 0)
        {

            // 击杀 tag 为 "Enemy" 的敌人奖励 1 金币
            if(CompareTag("Enemy"))
            {

                CoinManager.AddCoin();

            }

            Destroy(gameObject);

        }


    }





    IEnumerator HitFlash()
    {


        if(spriteRenderer != null)
        {


            Color oldColor =
            spriteRenderer.color;



            spriteRenderer.color =
            Color.white;



            yield return
            new WaitForSeconds(0.1f);



            spriteRenderer.color =
            oldColor;


        }


    }





    IEnumerator Stun()
    {


        if(enemyAI != null)
        {


            enemyAI.canMove =
            false;



            yield return
            new WaitForSeconds(0.2f);



            enemyAI.canMove =
            true;


        }


    }



}