using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Scenes (Optional)")]
    [SerializeField] private string mainMenuSceneName;

    private bool isPaused;

    private void Awake()
    {
        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;
    }

    private void OnDisable()
    {
        if (isPaused)
            Resume();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        isPaused = true;
        if (pauseRoot != null) pauseRoot.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    public void Resume()
    {
        isPaused = false;
        if (pauseRoot != null) pauseRoot.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void RestartScene()
    {
        Resume();
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Resume();
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            LoadScene(mainMenuSceneName);
    }

    public void Quit()
    {
        Resume();
        Application.Quit();
    }

    private void LoadScene(string sceneName)
    {
        EnsureTransitionManager();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadSceneWithTransition(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    private void LoadScene(int sceneBuildIndex)
    {
        EnsureTransitionManager();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadSceneWithTransition(sceneBuildIndex);
        else
            SceneManager.LoadScene(sceneBuildIndex);
    }

    private void EnsureTransitionManager()
    {
        if (SceneTransitionManager.Instance != null) return;
        GameObject managerObj = new GameObject("SceneTransitionManager");
        managerObj.AddComponent<SceneTransitionManager>();
    }
}
