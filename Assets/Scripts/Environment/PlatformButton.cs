using UnityEngine;
using System.Collections;

public class PlatformButton : MonoBehaviour
{
    public enum ButtonType
    {
        Basic,
        Timer  // Activates for a set duration
    }

    [Header("Configuration")]
    [SerializeField] private ButtonType buttonType = ButtonType.Basic;
    [SerializeField] private MovablePlatforms targetPlatform;
    
    [Header("Interaction Settings")]
    [Tooltip("Key to press to interact with the totem")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    [Header("Timer Settings")]
    [Tooltip("Duration in seconds the platform stays active after pressing.")]
    [SerializeField] private float activeDuration = 3.0f;

    [Header("Animation Settings")]
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private string pressedBoolParam = "IsPressed";
    [Tooltip("Bool parameter for Timer Active state")]
    [SerializeField] private string timerBoolParam = "IsTimerActive";
    [Tooltip("Name of the float parameter in Animator to control timer speed")]
    [SerializeField] private string speedFloatParam = "TimerSpeed";
    [Tooltip("Length of your Timer Active animation clip in seconds (usually 1.0)")]
    [SerializeField] private float baseAnimationLength = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isPressed = false;
    private bool isTimerActive = false;
    private bool playerInRange = false;
    private Coroutine timerCoroutine;

    private void Start()
    {
        if (showDebugLogs) Debug.Log($"[PlatformButton] Initialized on {gameObject.name}");

        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogError($"[PlatformButton] NO COLLIDER2D FOUND on {gameObject.name}! Interaction requires a Collider2D trigger.");
        }

        if (buttonAnimator == null)
        {
            buttonAnimator = GetComponent<Animator>();
        }

        // Initialize visuals
        UpdateVisuals(false, false);
    }

    public void SetTargetPlatform(MovablePlatforms platform)
    {
        targetPlatform = platform;
    }

    private void Update()
    {
        // Totem interaction logic: Only activates via Interact key when in range
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            OnInteract();
        }
    }

    private void OnInteract()
    {
        if (showDebugLogs) Debug.Log($"[PlatformButton] Interaction detected on {gameObject.name}");

        // Toggle logic for Basic (Blue/Toggle) buttons
        if (buttonType == ButtonType.Basic)
        {
            if (isPressed)
            {
                DeactivateButton();
            }
            else
            {
                ActivateButton();
            }
        }
        else
        {
            // For Timer (Red) buttons, always activate (restart timer if already running)
            ActivateButton();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (showDebugLogs) Debug.Log($"[PlatformButton] Player in range of {gameObject.name}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (showDebugLogs) Debug.Log($"[PlatformButton] Player left range of {gameObject.name}");
        }
    }
    
    // Collision handling for non-trigger colliders
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void ActivateButton()
    {
        if (showDebugLogs) Debug.Log($"[PlatformButton] Activating button {gameObject.name}");

        if (buttonType == ButtonType.Basic)
        {
            isPressed = true;
            UpdateVisuals(true, false);
        }
        else if (buttonType == ButtonType.Timer)
        {
            isTimerActive = true;
            
            // Handle Animation Speed for Timer
            if (buttonAnimator != null)
            {
                // Calculate speed multiplier: BaseLength / DesiredDuration
                float speedMultiplier = baseAnimationLength / Mathf.Max(activeDuration, 0.01f);
                buttonAnimator.SetFloat(speedFloatParam, speedMultiplier);
                if (showDebugLogs) Debug.Log($"[PlatformButton] Setting Anim Speed to {speedMultiplier} (Duration: {activeDuration}s)");
            }
            
            UpdateVisuals(false, true);
        }

        if (targetPlatform != null)
        {
            targetPlatform.Activate();
        }

        if (buttonType == ButtonType.Timer)
        {
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(TimerRoutine());
        }
    }

    private void DeactivateButton()
    {
        if (showDebugLogs) Debug.Log($"[PlatformButton] Deactivating button {gameObject.name}");
        
        isPressed = false;
        isTimerActive = false;
        
        UpdateVisuals(false, false);
        
        if (targetPlatform != null)
        {
            if (showDebugLogs) Debug.Log($"[PlatformButton] Sending Stop signal to platform {targetPlatform.name}");
            targetPlatform.Stop();
        }
    }

    private IEnumerator TimerRoutine()
    {
        // Wait for the active duration
        yield return new WaitForSeconds(activeDuration);
        
        DeactivateButton();
        timerCoroutine = null;
    }

    private void UpdateVisuals(bool pressed, bool timerActive)
    {
        if (buttonAnimator != null)
        {
            buttonAnimator.SetBool(pressedBoolParam, pressed);
            buttonAnimator.SetBool(timerBoolParam, timerActive);
        }
    }

    private void OnDrawGizmos()
    {
        if (targetPlatform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPlatform.transform.position);
        }
    }
}
