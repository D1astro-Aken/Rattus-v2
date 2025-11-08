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
        // 1) explicitní jméno scény
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        // 2) explicitní build index
        if (nextSceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(nextSceneBuildIndex);
            return;
        }

        // 3) fallback: následující scéna v Build Settings
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
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