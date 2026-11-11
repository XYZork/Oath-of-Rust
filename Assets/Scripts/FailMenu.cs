using UnityEngine;
using UnityEngine.SceneManagement;

public class FailMenu : MonoBehaviour
{
    void Start()
    {
        // 在 "You Died" 失败界面隐藏金币计数
        CoinManager.SetCoinUIVisible(false);
    }

    // 这个函数会被 "Try Again" 按钮调用
    public void TryAgain()
    {
        // 回到主菜单场景（先恢复时间，防止残留暂停状态）
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
