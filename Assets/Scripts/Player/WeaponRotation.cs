using UnityEngine;


public class WeaponRotation : MonoBehaviour
{


    // 玩家Transform
    public Transform player;


    // 是否固定四方向
    public bool fourDirection = true;


    private float currentAngle;

    private PlayerAttack playerAttack;



    void Start()
    {

        playerAttack =
        GetComponentInParent<PlayerAttack>();

    }



    void Update()
    {


        //攻击时由 PlayerAttack 控制挥击旋转，此处不再覆盖

        if(playerAttack != null
            && playerAttack.attacking)
            return;



        Vector3 mousePosition =
        Camera.main.ScreenToWorldPoint(
            Input.mousePosition
        );



        Vector2 direction =
        mousePosition -
        player.position;



        currentAngle =
        Mathf.Atan2(
            direction.y,
            direction.x
        )
        *
        Mathf.Rad2Deg;




        if(fourDirection)
        {

            currentAngle =
            GetDirection(currentAngle);

        }



        transform.rotation =
        Quaternion.Euler(
            0,
            0,
            currentAngle
        );


    }






    float GetDirection(float angle)
    {


        //右

        if(angle >= -45 &&
           angle <45)
        {

            return 0;

        }



        //上

        else if(angle >=45 &&
                angle <135)
        {

            return 90;

        }



        //左

        else if(angle >=135 ||
                angle < -135)
        {

            return 180;

        }



        //下

        else
        {

            return 270;

        }


    }


}