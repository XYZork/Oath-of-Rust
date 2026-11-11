using UnityEngine;


public class PlayerDirection : MonoBehaviour
{

    public SpriteRenderer spriteRenderer;


    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;



    void Update()
    {

        // 暂停时停止面向鼠标
        if(Time.timeScale == 0f)
            return;

        // 获取鼠标世界坐标

        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(
                Input.mousePosition
            );


        mousePosition.z = 0;


        // 鼠标相对玩家的位置

        Vector2 direction =
            mousePosition - transform.position;



        // 判断主要方向

        if(Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {

            // 水平方向

            if(direction.x > 0)
            {
                // 右

                spriteRenderer.sprite =
                    rightSprite;
            }
            else
            {
                // 左

                spriteRenderer.sprite =
                    leftSprite;
            }

        }

        // 垂直方向（上/下）：不再切换精灵，保持当前朝向

    }

}