using UnityEngine;


public class RoomManager : MonoBehaviour
{

    public GameObject[] enemies;


    public GameObject door;


    void Update()
    {

        bool allDead = true;


        foreach(GameObject enemy in enemies)
        {

            if(enemy != null)
            {
                allDead = false;
            }

        }


        if(allDead)
        {

            door.SetActive(false);

        }


    }

}