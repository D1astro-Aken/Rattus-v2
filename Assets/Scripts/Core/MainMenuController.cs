using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string playSceneName;
    [SerializeField] private int playSceneBuildIndex = -1;

    [Header("Panels (Optional)")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;
    }

    public void Play()
    {
        if (!string.IsNullOrEmpty(playSceneName))
        {
            LoadScene(playSceneName);
            return;
        }

        if (playSceneBuildIndex >= 0)
        {
            LoadScene(playSceneBuildIndex);
            return;
        }

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        LoadScene(nextIndex);
    }

    public void OpenOptions()
    {
        SetPanels(mainPanelActive: false, optionsPanelActive: true, creditsPanelActive: false);
    }

    public void OpenCredits()
    {
        SetPanels(mainPanelActive: false, optionsPanelActive: false, creditsPanelActive: true);
    }

    public void BackToMain()
    {
        SetPanels(mainPanelActive: true, optionsPanelActive: false, creditsPanelActive: false);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void LoadScene(string sceneName)
    {
        EnsureTransitionManager();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadSceneWithTransition(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(int sceneBuildIndex)
    {
        EnsureTransitionManager();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadSceneWithTransition(sceneBuildIndex);
        else
            SceneManager.LoadScene(sceneBuildIndex);
    }

    private void SetPanels(bool mainPanelActive, bool optionsPanelActive, bool creditsPanelActive)
    {
        if (mainPanel != null) mainPanel.SetActive(mainPanelActive);
        if (optionsPanel != null) optionsPanel.SetActive(optionsPanelActive);
        if (creditsPanel != null) creditsPanel.SetActive(creditsPanelActive);
    }

    private void EnsureTransitionManager()
    {
        if (SceneTransitionManager.Instance != null) return;
        GameObject managerObj = new GameObject("SceneTransitionManager");
        managerObj.AddComponent<SceneTransitionManager>();
    }
}
