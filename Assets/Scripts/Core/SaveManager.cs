using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private string mainMenuSceneName;
    [SerializeField] private string newGameSceneName;
    [SerializeField] private int newGameSceneBuildIndex = -1;
    [SerializeField] private string defaultSpawnPointName = "PlayerSpawn";

    private string pendingSpawnPointName;

    private const string HasSaveKey = "Save_HasSave";
    private const string SceneNameKey = "Save_SceneName";
    private const string SceneBuildIndexKey = "Save_SceneBuildIndex";

    public static void EnsureExists()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("SaveManager");
        go.AddComponent<SaveManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void Configure(string menuSceneName, string defaultNewGameSceneName, int defaultNewGameBuildIndex)
    {
        if (!string.IsNullOrEmpty(menuSceneName))
            mainMenuSceneName = menuSceneName;

        if (!string.IsNullOrEmpty(defaultNewGameSceneName))
            newGameSceneName = defaultNewGameSceneName;

        if (defaultNewGameBuildIndex >= 0)
            newGameSceneBuildIndex = defaultNewGameBuildIndex;
    }

    public void SetNextSpawnPoint(string spawnPointName)
    {
        pendingSpawnPointName = spawnPointName;
    }

    public bool HasSaveGame()
    {
        if (PlayerPrefs.GetInt(HasSaveKey, 0) != 1) return false;
        string savedSceneName = PlayerPrefs.GetString(SceneNameKey, string.Empty);
        int savedBuildIndex = PlayerPrefs.GetInt(SceneBuildIndexKey, -1);
        return !string.IsNullOrEmpty(savedSceneName) || savedBuildIndex >= 0;
    }

    public void ResetSaveGame()
    {
        PlayerPrefs.DeleteKey(HasSaveKey);
        PlayerPrefs.DeleteKey(SceneNameKey);
        PlayerPrefs.DeleteKey(SceneBuildIndexKey);
        PlayerPrefs.Save();
    }

    public void StartNewGame()
    {
        ResetSaveGame();
        EnsureTransitionManager();

        if (!string.IsNullOrEmpty(newGameSceneName) && SceneExistsInBuildSettings(newGameSceneName))
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(newGameSceneName);
            return;
        }

        if (newGameSceneBuildIndex >= 0 && newGameSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(newGameSceneBuildIndex);
            return;
        }

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(nextIndex);
        }
    }

    public void ContinueGame()
    {
        if (!HasSaveGame())
        {
            StartNewGame();
            return;
        }

        string sceneName = PlayerPrefs.GetString(SceneNameKey, string.Empty);
        int buildIndex = PlayerPrefs.GetInt(SceneBuildIndexKey, -1);

        EnsureTransitionManager();

        if (!string.IsNullOrEmpty(sceneName) && SceneExistsInBuildSettings(sceneName))
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(sceneName);
            return;
        }

        if (buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(buildIndex);
            return;
        }

        StartNewGame();
    }

    public void GoToMainMenu()
    {
        EnsureTransitionManager();

        if (!string.IsNullOrEmpty(mainMenuSceneName) && SceneExistsInBuildSettings(mainMenuSceneName))
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(mainMenuSceneName);
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(0);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(scene.name)) return;
        if (!string.IsNullOrEmpty(mainMenuSceneName) && scene.name == mainMenuSceneName)
        {
            pendingSpawnPointName = null;
            return;
        }

        GameObject player = EnsureSinglePlayer();

        PlayerPrefs.SetInt(HasSaveKey, 1);
        PlayerPrefs.SetString(SceneNameKey, scene.name);
        PlayerPrefs.SetInt(SceneBuildIndexKey, scene.buildIndex);
        PlayerPrefs.Save();

        ApplySpawnPoint(player);
    }

    private GameObject EnsureSinglePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0) return null;

        GameObject keep = null;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].scene.name == "DontDestroyOnLoad")
            {
                keep = players[i];
                break;
            }
        }

        if (keep == null)
            keep = players[0];

        for (int i = 0; i < players.Length; i++)
        {
            GameObject p = players[i];
            if (p == null) continue;
            if (p == keep) continue;
            Destroy(p);
        }

        return keep;
    }

    private void ApplySpawnPoint(GameObject player)
    {
        string spawnName = !string.IsNullOrEmpty(pendingSpawnPointName) ? pendingSpawnPointName : defaultSpawnPointName;
        pendingSpawnPointName = null;

        if (string.IsNullOrEmpty(spawnName)) return;

        GameObject spawn = GameObject.Find(spawnName);
        if (spawn == null) return;

        if (player == null) return;

        player.transform.position = spawn.transform.position;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Health health = player.GetComponent<Health>();
        if (health != null)
            health.SetRespawnPoint(spawn.transform.position);
    }

    private static bool SceneExistsInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }

    private static void EnsureTransitionManager()
    {
        if (SceneTransitionManager.Instance != null) return;
        GameObject managerObj = new GameObject("SceneTransitionManager");
        managerObj.AddComponent<SceneTransitionManager>();
    }
}
