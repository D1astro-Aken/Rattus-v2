using UnityEngine;
using System.Collections;

public class AntSpitter : MonsterPatrol
{
    [Header("Spitting Settings")]
    public Transform playerTransform;
    public float spittingDistance = 6f;
    public float spitCooldown = 2.5f;
    public GameObject projectilePrefab;
    public Transform spitPoint;
    
    [Header("Targeting Settings")]
    public float projectileSpeed = 8f;
    public bool usePredictiveAiming = true;
    public float predictionMultiplier = 1f;
    
    [Header("Run Away Settings")]
    public float runAwayDuration = 2f;
    public float runAwaySpeed = 4f;
    public float runAwayDistance = 5f;
    
    [Header("Burst Settings")]
    public bool useBurstFire = true;
    public int burstCount = 3;
    public float burstDelay = 0.2f;
    
    [Header("Projectile Launch Settings")]
    public float projectileLaunchDelay = 0.5f; // Doba čekání před vypuštěním projektilu

    private float lastSpitTime = -Mathf.Infinity;
    private bool isSpitting = false;
    private Rigidbody2D playerRb;
    
    // Shooting timer mechanics
    private bool isSpittingLocked = false;
    private float spittingLockDuration = 1f; // Duration to lock movement during spitting
    private float spittingLockStartTime;
    
    // Run away mechanic variables
    private bool isRunningAway = false;
    private float runAwayStartTime;
    private Vector2 runAwayDirection;

    protected override void Start()
    {
        base.Start();

        // Automaticky najdi hráče pokud není přiřazen
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerRb = player.GetComponent<Rigidbody2D>();
            }
        }
        else
        {
            playerRb = playerTransform.GetComponent<Rigidbody2D>();
        }

        // Note: We set projectile speed on each spawned instance, not on the prefab
    }

    protected override void Update()
    {
        // Handle run away mechanic FIRST - it has ABSOLUTE highest priority
        // NOTHING can interrupt run away once it starts
        if (isRunningAway)
        {
            HandleRunAway();
            return; // ABSOLUTELY no other logic during run away
        }

        // Handle spitting lock timer - NEVER check player range during this time
        if (isSpittingLocked)
        {
            if (Time.time - spittingLockStartTime >= spittingLockDuration)
            {
                isSpittingLocked = false;
                isSpitting = false;
                
                // ALWAYS start run away after spitting lock ends, regardless of player position
                Debug.Log("[AntSpitter] Spitting lock ended - starting run away (forced)");
                StartRunAway();
                return; // Return immediately to start run away
            }
            else
            {
                // During spitting lock, only update animations - NO range checks!
                UpdateAnimationStates();
                return;
            }
        }
        
        if (isSpitting) return; // Během střelby neprovádíme patrol

        // Only check player range if we're NOT in any shooting state
        if (playerTransform != null && IsPlayerInRange())
        {
            FacePlayer();

            if (CanSpit())
            {
                if (useBurstFire)
                {
                    StartCoroutine(BurstSpit());
                }
                else
                {
                    Spit();
                }
                lastSpitTime = Time.time;
                
                // Start spitting lock
                StartSpittingLock();
            }

            // Animation states
            UpdateAnimationStates();
        }
        else
        {
            // Player is out of range - only reset if we're not in any shooting sequence
            if (!isSpitting && !isSpittingLocked)
            {
                base.Update(); // Patrol
                UpdateAnimationStates();
            }
            else
            {
                // If we're shooting or locked, still update animations but don't patrol
                // This ensures animation states are properly managed even during shooting sequences
                UpdateAnimationStates();
            }
        }
    }

    private bool IsPlayerInRange()
    {
        return Vector2.Distance(transform.position, playerTransform.position) <= spittingDistance;
    }

    private bool CanSpit()
    {
        bool canSpit = Time.time >= lastSpitTime + spitCooldown && !isSpitting && !isSpittingLocked;
        return canSpit;
    }

    private void FacePlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }

    private void Spit()
    {
        if (projectilePrefab != null && spitPoint != null)
        {
            Vector2 targetDirection = CalculateTargetDirection();
            SpawnProjectile(targetDirection);
        }
    }

    private IEnumerator BurstSpit()
    {
        Debug.Log($"[AntSpitter] BurstSpit started - isSpitting: {isSpitting}");
        isSpitting = true;

        for (int i = 0; i < burstCount; i++)
        {
            Debug.Log($"[AntSpitter] Burst shot {i + 1}/{burstCount}");
            if (projectilePrefab != null && spitPoint != null)
            {
                Vector2 targetDirection = CalculateTargetDirection();
                SpawnProjectile(targetDirection);
            }
            else
            {
                Debug.LogWarning($"[AntSpitter] Cannot spawn projectile - prefab: {projectilePrefab != null}, spitPoint: {spitPoint != null}");
            }

            if (i < burstCount - 1) // Nečekej po posledním výstřelu
            {
                yield return new WaitForSeconds(burstDelay);
            }
        }

        Debug.Log("[AntSpitter] BurstSpit completed");
        isSpitting = false;
    }

    private Vector2 CalculateTargetDirection()
    {
        if (playerTransform == null) return Vector2.right;
        
        Vector2 targetPosition = playerTransform.position;
        
        // Add movement prediction if enabled
        if (usePredictiveAiming)
        {
            Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                float timeToTarget = Vector2.Distance(spitPoint.position, targetPosition) / projectileSpeed;
                targetPosition += playerRb.velocity * timeToTarget * predictionMultiplier;
            }
        }
        
        Vector2 direction = (targetPosition - (Vector2)spitPoint.position).normalized;
        return direction;
    }

    private void SpawnProjectile(Vector2 direction)
    {
        // Calculate rotation angle towards target direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        GameObject proj = Instantiate(projectilePrefab, spitPoint.position, rotation);

        // Nastav směr projektilu a launch delay
        Projectiles projectile = proj.GetComponent<Projectiles>();
        if (projectile != null)
        {
            projectile.SetDirection(direction);
            projectile.SetLaunchDelay(projectileLaunchDelay);
            // Set speed on the spawned instance, not the prefab
            projectile.speed = projectileSpeed;
        }

        // Poznámka: Rigidbody2D velocity se už nenastavuje, protože Projectiles script řídí pohyb
    }

    // Debug vizualizace v editoru
    private void OnDrawGizmosSelected()
    {
        // Zobraz dosah střelby
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spittingDistance);

        // Zobraz směr k hráči
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, playerTransform.position);

            // Zobraz predikovanou pozici
            if (usePredictiveAiming && playerRb != null)
            {
                float timeToTarget = Vector2.Distance(spitPoint.position, playerTransform.position) / projectileSpeed;
                Vector2 predictedPos = (Vector2)playerTransform.position + playerRb.velocity * timeToTarget * predictionMultiplier;
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(predictedPos, 0.5f);
                Gizmos.DrawLine(transform.position, predictedPos);
            }
        }

        // Zobraz spitPoint
        if (spitPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spitPoint.position, 0.2f);
        }
    }

    // Spitting lock mechanics
    private void StartSpittingLock()
    {
        Debug.Log($"[AntSpitter] StartSpittingLock - Duration: {spittingLockDuration}s");
        isSpittingLocked = true;
        isSpitting = true; // Set isSpitting to true during lock
        spittingLockStartTime = Time.time;
        
        // No longer need the coroutine since we handle it in Update
    }
    
    // This method is no longer needed since we handle spitting lock directly in Update
    /*
    private IEnumerator HandleSpittingLockEnd()
    {
        Debug.Log($"[AntSpitter] HandleSpittingLockEnd - Waiting {spittingLockDuration}s");
        yield return new WaitForSeconds(spittingLockDuration);
        
        Debug.Log($"[AntSpitter] HandleSpittingLockEnd - Lock ended, starting run away");
        // After spitting lock ends, always start running away regardless of player position
        if (!isRunningAway && playerTransform != null)
        {
            Debug.Log("[AntSpitter] Starting run away after spitting lock");
            StartRunAway();
        }
        else
        {
            Debug.Log($"[AntSpitter] Not starting run away - isRunningAway: {isRunningAway}, playerTransform: {playerTransform != null}");
        }
    }
    */

    // Run away mechanic methods
    private void StartRunAway()
    {
        if (playerTransform == null) return;
        
        Debug.Log($"[AntSpitter] StartRunAway - Duration: {runAwayDuration}s");
        isRunningAway = true;
        runAwayStartTime = Time.time;
        
        // Calculate direction away from player
        Vector2 directionFromPlayer = (transform.position - playerTransform.position).normalized;
        runAwayDirection = directionFromPlayer;
        
        // Face away from player
        if (runAwayDirection.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }
    
    private void HandleRunAway()
    {
        Debug.Log($"[AntSpitter] HandleRunAway - Time elapsed: {Time.time - runAwayStartTime}/{runAwayDuration}");
        
        // Check if run away duration is over
        if (Time.time - runAwayStartTime >= runAwayDuration)
        {
            Debug.Log("[AntSpitter] HandleRunAway - Run away duration ended");
            isRunningAway = false;
            return;
        }
        
        // Move away from player - ALWAYS move regardless of player position
        Vector2 targetPosition = (Vector2)transform.position + runAwayDirection * runAwaySpeed * Time.deltaTime;
        transform.position = targetPosition;
        
        // Update animations during run away
        UpdateAnimationStates();
    }
    
    // Animation management
    private void UpdateAnimationStates()
    {
        if (anim == null) return;
        
        Debug.Log($"[AntSpitter] UpdateAnimationStates - isSpitting: {isSpitting}, isSpittingLocked: {isSpittingLocked}, isRunningAway: {isRunningAway}");
        
        // Set animation parameters based on current state
        if (HasAnimatorParameter("isSpitting"))
            anim.SetBool("isSpitting", isSpitting || isSpittingLocked); // Keep spitting animation during lock
            
        if (HasAnimatorParameter("isRunning"))
            anim.SetBool("isRunning", isRunningAway);
            
        // Improved walking detection
        bool isMoving = false;
        
        if (isRunningAway)
        {
            // During run away, always consider as moving
            isMoving = true;
        }
        else if (isSpitting || isSpittingLocked)
        {
            // During shooting sequence, not moving (standing still to shoot)
            isMoving = false;
        }
        else if (playerTransform != null && IsPlayerInRange())
        {
            // When chasing/facing player, consider as moving (even if just facing)
            isMoving = true;
        }
        else
        {
            // During patrol, check if we're actually moving towards patrol point
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Transform targetPoint = patrolPoints[patrolDestination];
                float distanceToTarget = Vector2.Distance(transform.position, targetPoint.position);
                isMoving = distanceToTarget > 0.2f; // Moving if not at patrol point
            }
        }
            
        if (HasAnimatorParameter("isWalking"))
            anim.SetBool("isWalking", !isSpitting && !isSpittingLocked && !isRunningAway && isMoving);
            
        if (HasAnimatorParameter("isIdle"))
            anim.SetBool("isIdle", !isSpitting && !isSpittingLocked && !isRunningAway && !isMoving);
    }

    // Pomocná metoda pro kontrolu existence animator parametru
    private bool HasAnimatorParameter(string paramName)
    {
        if (anim == null) return false;
        
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}