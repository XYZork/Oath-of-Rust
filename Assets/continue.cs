using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel; // 拖入刚才创建的 PausePanel

    private bool isPaused = false;

    void Update()
    {
        // 检测按下 Esc 键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    // 继续游戏
    public void ResumeGame()
    {
        pausePanel.SetActive(false); // 隐藏面板
        Time.timeScale = 1f;        // 恢复游戏时间
        isPaused = false;
    }

    // 暂停游戏
    void PauseGame()
    {
        pausePanel.SetActive(true); // 显示面板
        Time.timeScale = 0f;        // 冻结游戏时间
        isPaused = true;
    }

    // 退出到主菜单
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // 先恢复时间，防止影响主菜单
        // 确保主菜单场景名与 Build Settings 里一致
        SceneManager.LoadScene("MainMenu");
    }
}