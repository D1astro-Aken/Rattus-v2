using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Player & Input")]
    public Transform playerTransform;
    public KeyCode interactKey = KeyCode.F;

    [Header("Detection")]
    public float interactRange = 2f; // vzdálenost pro interakci, pokud nepoužiješ trigger
    public bool useTriggerDetection = true; // pokud má objekt 2D collider s isTrigger

    [Header("Target Scene")]
    public string nextSceneName; // volitelné: jméno scény
    public int nextSceneBuildIndex = -1; // volitelné: build index
    
    [Header("Spawn In Next Scene (Optional)")]
    public string nextSceneSpawnPointName = "PlayerSpawn";

    private bool playerInTrigger = false;

    private void Start()
    {
        // Autodetekce hráče, pokud není přiřazen
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        bool inRange = playerInTrigger;
        if (!useTriggerDetection)
        {
            inRange = Vector2.Distance(transform.position, playerTransform.position) <= interactRange;
        }

        if (inRange && Input.GetKeyDown(interactKey))
        {
            LoadTargetScene();
        }
    }

    private void LoadTargetScene()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SetNextSpawnPoint(nextSceneSpawnPointName);

        // Zajistíme existenci SceneTransitionManageru
        if (SceneTransitionManager.Instance == null)
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            managerObj.AddComponent<SceneTransitionManager>();
        }

        // 1) explicitní jméno scény
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // DIAGNOSTIKA: Ověření, zda scéna existuje v Build Settings
            bool sceneFound = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (sceneName == nextSceneName)
                {
                    sceneFound = true;
                    break;
                }
            }

            if (!sceneFound)
            {
                Debug.LogError($"CHYBA: Scéna '{nextSceneName}' nebyla nalezena v Build Settings!");
                Debug.LogError("Dostupné scény v Build Settings:");
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    Debug.LogError($" [{i}] {name} (Path: {path})");
                }
                return; // Nepokoušej se načíst, pokud tam není
            }

            // Použijeme nový transition manager místo přímého načtení
            SceneTransitionManager.Instance.LoadSceneWithTransition(nextSceneName);
            return;
        }

        // 2) explicitní build index
        if (nextSceneBuildIndex >= 0)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(nextSceneBuildIndex);
            return;
        }

        // 3) fallback: následující scéna v Build Settings
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(nextIndex);
        }
        else
        {
            Debug.LogWarning("[ScenePortal] V Build Settings není další scéna – nastav 'nextSceneName' nebo 'nextSceneBuildIndex'.");
        }
    }

    // Trigger detekce (pokud má objekt 2D collider s isTrigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTriggerDetection) return;
        if (other.CompareTag("Player"))
            playerInTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!useTriggerDetection) return;
        if (other.CompareTag("Player"))
            playerInTrigger = false;
    }
}
