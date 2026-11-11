using UnityEngine;


public class RoomController : MonoBehaviour
{

    public GameObject[] enemies;


    public GameObject door;


    bool activated = false;


    bool completed = false;



    private void Start()
    {
        // 游戏开始门隐藏

        if(door != null)
        {
            door.SetActive(false);
        }


        // 敌人隐藏

        foreach(GameObject enemy in enemies)
        {
            enemy.SetActive(false);
        }

    }



    private void OnTriggerEnter2D(Collider2D other)
    {

        if(other.CompareTag("Player") && !activated)
        {

            activated = true;

            StartRoom();

        }

    }



    void StartRoom()
    {

        // 出现门

        if(door != null)
        {
            door.SetActive(true);
        }


        // 激活敌人（原地出现，不传送）

        foreach(GameObject enemy in enemies)
        {

            if(enemy != null)
            {

                enemy.SetActive(true);

            }

        }

    }



    void Update()
    {

        if(!activated || completed)
            return;



        int alive = 0;



        foreach(GameObject enemy in enemies)
        {

            if(enemy != null)
            {
                alive++;
            }

        }



        if(alive == 0)
        {

            CompleteRoom();

        }


    }



    void CompleteRoom()
    {

        completed = true;


        // 删除门

        if(door != null)
        {
            door.SetActive(false);
        }


    }

}