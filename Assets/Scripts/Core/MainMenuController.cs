using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class MainMenuController : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string playSceneName;
    [SerializeField] private int playSceneBuildIndex = -1;

    [Header("Panels (Optional)")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Play Popup (Optional)")]
    [SerializeField] private GameObject playPopup;
    [SerializeField] private RectTransform playPopupContent;
    [SerializeField] private Button continueButton;

    private Canvas playPopupCanvas;

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;

        EnsureEventSystem();
        EnsureTransitionManager();
        SaveManager.EnsureExists();
        if (SaveManager.Instance != null)
            SaveManager.Instance.Configure(SceneManager.GetActiveScene().name, playSceneName, playSceneBuildIndex);

        if (playPopup != null)
            playPopup.SetActive(false);
    }

    private void Update()
    {
        if (playPopup == null) return;
        if (!playPopup.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePlayPopup();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (playPopupContent == null) return;

            if (playPopupCanvas == null)
                playPopupCanvas = playPopupContent.GetComponentInParent<Canvas>();

            Camera cam = null;
            if (playPopupCanvas != null && playPopupCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = playPopupCanvas.worldCamera;

            bool inside = RectTransformUtility.RectangleContainsScreenPoint(playPopupContent, Input.mousePosition, cam);
            if (!inside)
                ClosePlayPopup();
        }
    }

    public void Play()
    {
        if (playPopup != null)
        {
            OpenPlayPopup();
        }
        else
        {
            StartNewGame();
        }
    }

    public void OpenPlayPopup()
    {
        if (playPopup != null)
            playPopup.SetActive(true);

        if (continueButton != null)
            continueButton.interactable = SaveManager.Instance != null && SaveManager.Instance.HasSaveGame();
    }

    public void ClosePlayPopup()
    {
        if (playPopup != null)
            playPopup.SetActive(false);
    }

    public void Continue()
    {
        ClosePlayPopup();
        if (SaveManager.Instance != null)
            SaveManager.Instance.ContinueGame();
        else
            StartNewGame();
    }

    public void NewGame()
    {
        ClosePlayPopup();
        StartNewGame();
    }

    public void ResetSave()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.ResetSaveGame();

        if (continueButton != null)
            continueButton.interactable = false;

        ClosePlayPopup();
    }

    private void StartNewGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Configure(SceneManager.GetActiveScene().name, playSceneName, playSceneBuildIndex);
            SaveManager.Instance.StartNewGame();
            return;
        }

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

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing != null)
        {
            if (!existing.gameObject.activeInHierarchy)
                existing.gameObject.SetActive(true);
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            es.AddComponent(inputSystemModuleType);
        }
        else
        {
            es.AddComponent<StandaloneInputModule>();
        }
    }
}
