using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using System;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private float zoomAmount = 0.5f; // Target scale factor (e.g. 0.5 = half size = zoom in)
    [SerializeField] private Color fadeColor = Color.black;

    private Canvas transitionCanvas;
    private Image fadeImage;
    private float originalOrthoSize;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCanvas();
            DisableTransitionRaycasts();
            SaveManager.EnsureExists();
            EnsureEventSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystem();
    }

    private void DisableTransitionRaycasts()
    {
        if (fadeImage != null)
            fadeImage.raycastTarget = false;

        GraphicRaycaster raycaster = GetComponentInChildren<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = false;
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

    private void InitializeCanvas()
    {
        // Vytvoříme Canvas pro Fade efekt, pokud neexistuje
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform);
        
        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999; // Aby byl vždy nahoře

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        // Vytvoříme Image pro Fade
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // Začínáme průhlední
        fadeImage.raycastTarget = false;
        
        // Roztáhneme přes celou obrazovku
        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }

    public void LoadSceneWithTransition(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    public void LoadSceneWithTransition(int sceneIndex)
    {
        if (isTransitioning) return;
        // Získáme jméno scény z indexu (pro konzistenci) nebo jen použijeme index v korutině
        // Pro jednoduchost načteme pomocí indexu
        StartCoroutine(TransitionRoutineIndex(sceneIndex));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        yield return StartCoroutine(TransitionOut());
        SceneManager.LoadScene(sceneName);
        yield return StartCoroutine(TransitionIn());
    }

    private IEnumerator TransitionRoutineIndex(int sceneIndex)
    {
        yield return StartCoroutine(TransitionOut());
        SceneManager.LoadScene(sceneIndex);
        yield return StartCoroutine(TransitionIn());
    }

    private IEnumerator TransitionOut()
    {
        isTransitioning = true;
        float timer = 0f;

        Camera cam = Camera.main;
        float startSize = 5f;
        if (cam != null) startSize = cam.orthographicSize;
        float targetSize = startSize * zoomAmount;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / transitionDuration);

            // Fade to color
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = t;
                fadeImage.color = c;
            }

            // Zoom In (zmenšujeme orthographicSize)
            if (cam != null)
            {
                cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            }

            yield return null;
        }

        // Ujistíme se, že je fade 100%
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
        }
    }

    private IEnumerator TransitionIn()
    {
        // Počkáme jeden frame, aby se nová scéna stihla inicializovat a našli jsme kameru
        yield return null;

        Camera cam = Camera.main;
        float targetSize = 5f;
        if (cam != null) targetSize = cam.orthographicSize;
        
        // Začneme "přiblížení" (zoomed in) a budeme se oddalovat
        float startSize = targetSize * zoomAmount;

        if (cam != null)
        {
            cam.orthographicSize = startSize;
        }

        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / transitionDuration);

            // Fade from color
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 1f - t;
                fadeImage.color = c;
            }

            // Zoom Out (zvětšujeme orthographicSize zpět na normál)
            if (cam != null)
            {
                cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            }

            yield return null;
        }

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        isTransitioning = false;
    }
}
