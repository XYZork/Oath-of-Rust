using UnityEngine;
using System.Collections.Generic;



public class WeaponAttack : MonoBehaviour
{


    public int damage = 1;

    private float mouseAngle;



    //记录本次攻击已经攻击过的敌人

    private HashSet<EnemyHealth>
    hitEnemies =
    new HashSet<EnemyHealth>();

    




    //剑出现时自动清空

    void OnEnable()
    {

        hitEnemies.Clear();

    }






    public void ResetAttack()
    {

        hitEnemies.Clear();

    }






    private void OnTriggerEnter2D(
        Collider2D other
    )
    {


        EnemyHealth enemy =
        other.GetComponent<EnemyHealth>();



        if(enemy == null)
            return;




        //避免一次挥剑多次伤害

        if(!hitEnemies.Contains(enemy))
        {


            hitEnemies.Add(enemy);



            Vector2 knockbackDirection =
            (
                enemy.transform.position
                -
                transform.position

            ).normalized;



            enemy.TakeDamage(
                damage,
                knockbackDirection
            );


        }


    }



}