using UnityEngine;

public class IdleSit : MonoBehaviour
{
    [Header("Idle Sit Settings")]
    [SerializeField] private float idleSitDelay = 5f; // Time before entering sit animation
    [SerializeField] private float sitCleanDelay = 5f; // Time sitting before trigger sitClean
    [SerializeField] private AudioClip sitSound;
    [SerializeField] private AudioSource audioSource; // Can reference the one on Player

    private PlayerMovement playerMovement;
    private Rigidbody2D body;
    private Animator anim;
    
    private float idleTimer = 0f;
    private float sitCleanTimer = 0f;
    private bool isSitting = false;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        // Try to get audio source if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (playerMovement == null) return;

        // Idle Sit logic: enter sit state after being idle on ground for a while
        float horizontalInput = Input.GetAxis("Horizontal");
        bool noHorizontalInput = Mathf.Abs(horizontalInput) < 0.01f;
        
        // Use public methods/properties from PlayerMovement
        bool isOnGround = playerMovement.isGrounded();
        bool isGrabbingLedge = playerMovement.IsGrabbingLedge;
        bool onWall = playerMovement.IsOnWall; 
        bool isDashing = playerMovement.IsDashing;
        bool isClimbingLedge = playerMovement.IsClimbingLedge;

        bool veryStill = Mathf.Abs(body.velocity.x) < 0.05f && Mathf.Abs(body.velocity.y) < 0.05f;
        bool canSit = isOnGround && !isGrabbingLedge && !onWall && !isDashing && !isClimbingLedge;

        if (canSit && noHorizontalInput && veryStill)
        {
            if (!isSitting)
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleSitDelay)
                {
                    isSitting = true;

                    if (audioSource != null && sitSound != null)
                    {
                        audioSource.PlayOneShot(sitSound);
                    }
                }
            }
            else
            {
                // Already sitting - handle sitClean timer
                sitCleanTimer += Time.deltaTime;
                if (sitCleanTimer >= sitCleanDelay)
                {
                    if (anim != null)
                    {
                        anim.SetTrigger("sitClean");
                    }
                    sitCleanTimer = 0f; // Reset timer to allow repeating, or remove this line to play only once
                }
            }
        }
        else
        {
            idleTimer = 0f;
            sitCleanTimer = 0f;
            if (isSitting) // leave sit state
                isSitting = false;
        }

        if (anim != null)
            anim.SetBool("sit", isSitting);
    }
}
