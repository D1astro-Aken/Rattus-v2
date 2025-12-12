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
        
        // 1. Zkontroluj, zda je nad hráčem volné místo (UpperCheck)
        Vector2 aboveCheckPos = playerPos + Vector2.up * (upwardCheckDistance * 0.8f);
        Vector2 aboveBoxSize = new Vector2(ledgeDetectionWidth * 0.9f, ledgeDetectionHeight * 0.8f);
        
        Collider2D aboveCollider = Physics2D.OverlapBox(aboveCheckPos, aboveBoxSize, 0f, ledgeDetectionLayer);
        if (aboveCollider != null)
        {
            return false; // Nad hráčem je překážka, není to ledge
        }

        // 2. NOVÁ DETEKCE: Použij BoxCast směrem dopředu pro nalezení nejbližší zdi
        // Tím zajistíme, že vždy najdeme čelo zdi, nikoliv vnitřek nebo zeď za ní
        Vector2 boxOrigin = playerPos;
        // Použijeme úzký box pro "scan" směrem dopředu. Výška odpovídá detekční výšce.
        Vector2 boxSize = new Vector2(0.1f, ledgeDetectionHeight); 
        
        RaycastHit2D wallHit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.right * direction, forwardCheckDistance, ledgeDetectionLayer);
        
        if (wallHit.collider == null)
        {
            return false; // Žádná zeď v dosahu
        }

        // 3. Zkontroluj, zda je nad nalezenou zdí volno (Ledge) a najdi přesnou výšku
        // Použijeme X souřadnici nárazu (wallHit.point.x) a posuneme se kousek do zdi
        float checkX = wallHit.point.x + (direction * 0.1f); 
        
        // Raycast shora dolů pro nalezení přesné výšky ledge
        // Začneme dostatečně vysoko (upwardCheckDistance)
        Vector2 rayOrigin = new Vector2(checkX, playerPos.y + upwardCheckDistance);
        float rayDistance = upwardCheckDistance + (ledgeDetectionHeight * 2f); 
        
        RaycastHit2D ledgeHit = Physics2D.Raycast(rayOrigin, Vector2.down, rayDistance, ledgeDetectionLayer);
        
        if (ledgeHit.collider == null)
        {
            return false; // Nenalezen povrch shora
        }
        
        // (Volitelné) Zde by šla přidat kontrola, zda je ledgeHit.point.y v dosahu hráče

        // 4. Zkontroluj volný prostor nad ledge (Headroom)
        // Zkontrolujeme prostor nad nalezenou hranou
        Vector2 headroomCheckPos = new Vector2(checkX, ledgeHit.point.y + (ledgeDetectionHeight * 0.5f) + 0.1f);
        Vector2 headroomBoxSize = new Vector2(ledgeDetectionWidth * 0.9f, ledgeDetectionHeight);
        
        Collider2D headroomCollider = Physics2D.OverlapBox(headroomCheckPos, headroomBoxSize, 0f, ledgeDetectionLayer);
        if (headroomCollider != null)
        {
            return false; // Nad ledge není místo pro hráče
        }

        // 5. Ulož přesnou pozici
        // X = Pozice stěny (wallHit.point.x)
        // Y = Výška ledge (ledgeHit.point.y)
        ledgePosition = new Vector2(wallHit.point.x, ledgeHit.point.y);
        
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
        float direction = Mathf.Sign(transform.root.localScale.x);
        Vector2 playerPos = transform.position;

        // 1. Above Check
        Gizmos.color = Color.cyan;
        Vector2 aboveCheckPos = playerPos + Vector2.up * (upwardCheckDistance * 0.8f);
        Vector2 aboveBoxSize = new Vector2(ledgeDetectionWidth * 0.9f, ledgeDetectionHeight * 0.8f);
        Gizmos.DrawWireCube(aboveCheckPos, aboveBoxSize);

        // 2. Forward BoxCast (Range)
        Gizmos.color = Color.blue;
        Vector2 boxOrigin = playerPos;
        Vector2 boxSize = new Vector2(0.1f, ledgeDetectionHeight);
        
        // Draw start box
        Gizmos.DrawWireCube(boxOrigin, boxSize);
        // Draw end box (max range)
        Vector2 boxEnd = boxOrigin + Vector2.right * direction * forwardCheckDistance;
        Gizmos.DrawWireCube(boxEnd, boxSize);
        // Draw connection
        Gizmos.DrawLine(boxOrigin, boxEnd);

        // 3. Hit Visualization (Play Mode Only)
        if (Application.isPlaying && canGrab)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(ledgePosition, 0.05f);
            
            // Visualize the vertical ray that found the surface
            Gizmos.color = Color.yellow;
            Vector2 rayTop = new Vector2(ledgePosition.x, playerPos.y + upwardCheckDistance);
            Gizmos.DrawLine(rayTop, ledgePosition);
            
            // Visualize Headroom check
            Gizmos.color = Color.magenta;
            Vector2 headroomCheckPos = new Vector2(ledgePosition.x + direction * 0.1f, ledgePosition.y + (ledgeDetectionHeight * 0.5f) + 0.1f);
            Vector2 headroomBoxSize = new Vector2(ledgeDetectionWidth * 0.9f, ledgeDetectionHeight);
            Gizmos.DrawWireCube(headroomCheckPos, headroomBoxSize);
        }
    }
}
