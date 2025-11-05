using System.Collections.Generic;
using UnityEngine;

public class LedgeHitbox : MonoBehaviour
{
    [Header("Ledge Detection Settings")]
    [SerializeField] private float ledgeDetectionWidth = 0.6f;
    [SerializeField] private float ledgeDetectionHeight = 0.3f;
    [SerializeField] private float forwardCheckDistance = 0.9f;
    [SerializeField] private float upwardCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    
    [Header("Detection Frequency")]
    [SerializeField] private float detectionRate = 0.01f; // Ještě rychlejší detekce pro lepší responzivnost

    public bool canGrab { get; private set; }
    public Vector2 ledgePosition { get; private set; }

    private float lastDetectionTime = 0f;
    private PlayerMovement playerMovement;

    // @SFX:LedgeDetectInit
    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    // @SFX:LedgeDetectLoop
    private void Update()
    {
        // Don't run detection if already grabbing a ledge
        if (playerMovement != null && playerMovement.IsGrabbingLedge)
        {
            return;
        }
        
        // Kontroluj ledge detekci v pravidelných intervalech
        if (Time.time - lastDetectionTime >= detectionRate)
        {
            lastDetectionTime = Time.time;
            
            bool ledgeDetected = DetectLedgeWithOverlapBox();
            canGrab = ledgeDetected;
        }
    }

    // @SFX:LedgeDetect
    private bool DetectLedgeWithOverlapBox()
    {
        float direction = Mathf.Sign(transform.root.localScale.x);
        Vector2 playerPos = transform.position;

        // Debug logging removed

        // Try both directions if the character is falling straight down
        float[] directionsToCheck = { direction, -direction };
        
        foreach (float checkDirection in directionsToCheck)
        {
            if (CheckLedgeInDirection(playerPos, checkDirection))
            {
                return true;
            }
        }
        
        return false;
    }

    // @SFX:LedgeCheckDirection
    private bool CheckLedgeInDirection(Vector2 playerPos, float direction)
    {
        // Kombinovaný layer mask pro detekci ledge - zahrnuje ground i wall objekty
        LayerMask ledgeDetectionLayer = groundLayer | wallLayer;
        
        // 1. Zkontroluj, zda je nad hráčem volné místo (UpperCheck nahrazení)
        Vector2 aboveCheckPos = playerPos + Vector2.up * (upwardCheckDistance * 0.8f);
        Vector2 aboveBoxSize = new Vector2(ledgeDetectionWidth * 0.9f, ledgeDetectionHeight * 0.8f);
        
        Collider2D aboveCollider = Physics2D.OverlapBox(aboveCheckPos, aboveBoxSize, 0f, ledgeDetectionLayer);
        if (aboveCollider != null)
        {
            return false; // Nad hráčem je překážka, není to ledge
        }

        // 2. Zkontroluj, zda je před hráčem ground nebo wall na úrovni rukou
        Vector2 forwardCheckPos = playerPos + Vector2.right * direction * forwardCheckDistance;
        Vector2 forwardBoxSize = new Vector2(ledgeDetectionWidth, ledgeDetectionHeight);
        
        Collider2D forwardCollider = Physics2D.OverlapBox(forwardCheckPos, forwardBoxSize, 0f, ledgeDetectionLayer);
        if (forwardCollider == null)
        {
            // Zkus také blíže k hráči pro lepší detekci
            Vector2 closerCheckPos = playerPos + Vector2.right * direction * (forwardCheckDistance * 0.7f);
            forwardCollider = Physics2D.OverlapBox(closerCheckPos, forwardBoxSize, 0f, ledgeDetectionLayer);
            if (forwardCollider == null)
            {
                return false; // Před hráčem není ground ani wall, není co chytit
            }
            forwardCheckPos = closerCheckPos; // Použij bližší pozici
        }

        // 3. Zkontroluj, zda je nad pozicí před hráčem volné místo (skutečný ledge)
        Vector2 forwardAbovePos = forwardCheckPos + Vector2.up * (ledgeDetectionHeight + 0.1f);
        Vector2 forwardAboveSize = new Vector2(ledgeDetectionWidth * 0.9f, ledgeDetectionHeight * 0.9f);
        Collider2D forwardAboveCollider = Physics2D.OverlapBox(forwardAbovePos, forwardAboveSize, 0f, ledgeDetectionLayer);
        
        if (forwardAboveCollider != null)
        {
            return false; // Nad ledge pozicí není volné místo
        }

        // 4. Najdi skutečnou hranu ledge pro konzistentní pozicování
        // Raycast dolů z pozice nad přední hranou detekčního boxu
        Vector2 rayStart = forwardCheckPos + Vector2.right * direction * (ledgeDetectionWidth * 0.5f) + Vector2.up * (ledgeDetectionHeight + 0.2f);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, ledgeDetectionHeight + 0.4f, ledgeDetectionLayer);
        
        if (hit.collider != null)
        {
            // Použij hit point jako přesnou pozici ledge hrany
            ledgePosition = hit.point;
        }
        else
        {
            // Fallback na původní metodu pokud raycast selže
            ledgePosition = forwardCheckPos + Vector2.up * (ledgeDetectionHeight * 0.5f);
        }
        
        return true;
    }

    // @SFX:LedgeReset
    public void ResetLedge()
    {
        canGrab = false;
    }

    // --- DEBUG GIZMOS ---
    // @SFX:DebugGizmos
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        float direction = Mathf.Sign(transform.root.localScale.x);
        Vector2 playerPos = transform.position;

        // Zobrazení OverlapBox pozic
        Gizmos.color = Color.cyan;
        
        // Above check box (UpperCheck nahrazení)
        Vector2 aboveCheckPos = playerPos + Vector2.up * (upwardCheckDistance * 0.8f);
        Vector2 aboveBoxSize = new Vector2(ledgeDetectionWidth * 0.9f, ledgeDetectionHeight * 0.8f);
        Gizmos.DrawWireCube(aboveCheckPos, aboveBoxSize);

        // Forward check box
        Vector2 forwardCheckPos = playerPos + Vector2.right * direction * forwardCheckDistance;
        Vector2 forwardBoxSize = new Vector2(ledgeDetectionWidth, ledgeDetectionHeight);
        Gizmos.DrawWireCube(forwardCheckPos, forwardBoxSize);

        // Forward above check box
        Vector2 forwardAbovePos = forwardCheckPos + Vector2.up * (ledgeDetectionHeight + 0.1f);
        Vector2 forwardAboveSize = new Vector2(ledgeDetectionWidth * 0.9f, ledgeDetectionHeight * 0.9f);
        Gizmos.DrawWireCube(forwardAbovePos, forwardAboveSize);

        // Raycast pro nalezení přesné hrany ledge
        Vector2 rayStart = forwardCheckPos + Vector2.right * direction * (ledgeDetectionWidth * 0.5f) + Vector2.up * (ledgeDetectionHeight + 0.2f);
        Vector2 rayEnd = rayStart + Vector2.down * (ledgeDetectionHeight + 0.4f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(rayStart, rayEnd);

        // hit point (pokud je)
        if (canGrab)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(ledgePosition, 0.05f);
        }
    }
}
