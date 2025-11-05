using UnityEngine;
using System.Collections;

public class Projectiles : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1;
    
    [Header("Launch Delay Settings")]
    public float launchDelay = 0.5f; // Doba čekání před vypuštěním

    [Header("Destruction Settings")]
    [SerializeField] private LayerMask destructibleLayers = -1; // Defaultně všechny layers
    
    [Header("Animation Settings")]
    public float destroyAnimationDuration = 0.5f; // Duration of destroy animation before actual destruction

    private Vector2 direction;
    private bool isLaunched = false;
    private Vector3 spawnPosition;
    private Vector3 lockedPosition; // Position to lock at during destruction
    private bool delayStarted = false;
    private Animator animator;
    private bool isDestroying = false; // Prevent multiple destruction calls
    private bool isPositionLocked = false; // New state for locking position during animations

      [Header("Sounds")]
        [SerializeField] private AudioClip AudioClip1;
        [SerializeField] private AudioClip AudioClip2;
        [SerializeField] private AudioClip[] AudioClips1;
// @SFX:ProjectileInit
private void Start()
    {
        spawnPosition = transform.position;
        animator = GetComponent<Animator>();
        
        // Play spawn animation
        if (animator != null)
        {
            animator.SetTrigger("SpawnRune");
            Debug.Log("[Projectiles] SpawnRune animation triggered");
        }
        
        Destroy(gameObject, lifeTime + launchDelay + destroyAnimationDuration + 1f); // Include animation duration
    }

// @SFX:ProjectileSetDir
public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        Debug.Log($"[Projectiles] SetDirection called with: {dir}, normalized: {direction}");
        
        // Spustí delay pouze pokud ještě nebyl spuštěn
        if (!delayStarted)
        {
            StartCoroutine(LaunchAfterDelay());
            delayStarted = true;
        }
    }
    
// @SFX:ProjectileSetDelay
public void SetLaunchDelay(float delay)
    {
        launchDelay = delay;
    }

// @SFX:ProjectileLaunchDelayed
private IEnumerator LaunchAfterDelay()
    {
        yield return new WaitForSeconds(launchDelay);
        isLaunched = true;
        
        // Play idle animation when projectile starts moving
        if (animator != null)
        {
            animator.SetTrigger("IdleRune");
            Debug.Log("[Projectiles] IdleRune animation triggered - projectile launched");
        }
        
        Debug.Log("Projektil byl vypuštěn po delay!");
    }

// @SFX:ProjectileUpdate
private void Update()
    {
        if (isPositionLocked)
        {
            // Keep projectile locked at the collision position during animations
            transform.position = lockedPosition;
            return;
        }
        
        if (!isLaunched)
        {
            // Projektil zůstává na spawn pozici
            transform.position = spawnPosition;
            return;
        }

        // Normální pohyb projektilu - use world space movement
        Debug.Log($"[Projectiles] Moving with direction: {direction}, speed: {speed}");
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Note: Using transform.position instead of transform.Translate to move in world space
    }

// @SFX:ProjectileHit
private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDestroying) return; // Prevent multiple collision handling
        
        Debug.Log($"Projektil narazil do: {collision.name}, Tag: {collision.tag}, Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");

        if (collision.CompareTag("Player"))
        {
            Debug.Log("Projektil zasáhl hráče!");

            // Lock position at current location for hit animation
            lockedPosition = transform.position;
            isPositionLocked = true;

        if (SoundManager.instance != null)
         {
             if (AudioClips1 != null && AudioClips1.Length > 0)
                 SoundManager.instance.PlayOneOf(AudioClips1);
             else if (AudioClip1 != null)
                 SoundManager.instance.PlaySound(AudioClip1);
         }
            
            
            
            
            // Play hit animation
            if (animator != null)
            {
                animator.SetTrigger("HitRune");
                Debug.Log("[Projectiles] HitRune animation triggered - hit player");
            }
            
            // Způsob damage hráči
            Health playerHealth = collision.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            StartCoroutine(DestroyAfterAnimation());
            return;
        }

        // Zkontroluj specifické layers Ground a Wall
        string layerName = LayerMask.LayerToName(collision.gameObject.layer);
        if (layerName == "Ground" || layerName == "Wall")
        {
            Debug.Log($"Projektil se ničí o {layerName} layer!");
            
            // Lock position at current location for destroy animation
            lockedPosition = transform.position;
            isPositionLocked = true;
            
            // Play destroy animation
            if (animator != null)
            {
                animator.SetTrigger("DestroyRune");
                Debug.Log($"[Projectiles] DestroyRune animation triggered - hit {layerName}");
            }
            
            StartCoroutine(DestroyAfterAnimation());
            return;
        }

        // Zkontroluj jestli objekt je na některém z destructible layers
        int objectLayer = collision.gameObject.layer;
        
        Debug.Log($"Projektil narazil do layer: {LayerMask.LayerToName(objectLayer)} (číslo: {objectLayer})");
        Debug.Log($"Destructible layers mask: {destructibleLayers.value}");
        
        // Zkontroluj jestli layer objektu je v destructible layers
        if (((1 << objectLayer) & destructibleLayers) != 0)
        {
            Debug.Log($"Projektil se ničí o layer: {LayerMask.LayerToName(objectLayer)}!");
            
            // Lock position at current location for destroy animation
            lockedPosition = transform.position;
            isPositionLocked = true;
            
            // Play destroy animation
            if (animator != null)
            {
                animator.SetTrigger("DestroyRune");
                Debug.Log($"[Projectiles] DestroyRune animation triggered - hit {LayerMask.LayerToName(objectLayer)}");
            }
            
            StartCoroutine(DestroyAfterAnimation());
        }
        else
        {
            Debug.Log($"Layer {LayerMask.LayerToName(objectLayer)} není v destructible layers.");
        }
    }
    
// @SFX:ProjectileDestroy
private IEnumerator DestroyAfterAnimation()
    {
        isDestroying = true;
        
        // Position is already locked at collision point, no need to change isLaunched
        
        // Wait for animation to complete
        yield return new WaitForSeconds(destroyAnimationDuration);
        
        // Destroy the projectile
        Destroy(gameObject);
    }
}