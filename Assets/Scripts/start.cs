using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    void Update()
    {
        // 点击屏幕任意处开始游戏
        if (Input.GetMouseButtonDown(0))
        {
            StartGame();
        }
    }

    // 加载主游戏场景（此处场景名需与 Build Settings 中一致）
    public void StartGame()
    {
        SceneManager.LoadScene("MainGame");
    }
}
