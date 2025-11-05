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
    
    [Header("Multiple Spitpoints")]
    public bool useMultipleSpitPoints = false;
    public Transform[] spitPoints;
    
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
    private bool canShoot = true; // NEW: Explicit canShoot control
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

// @SFX:SpitterInit
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

// @SFX:SpitterUpdate
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
        
        // Handle active shooting (BurstSpit coroutine) - NEVER interrupt it!
        if (isSpitting)
        {
            Debug.Log("[AntSpitter] Currently shooting - no other logic allowed");
            UpdateAnimationStates();
            return; // Don't do anything else during active shooting
        }

        // Only check player range if we're NOT in any shooting state
        if (playerTransform != null && IsPlayerInRange())
        {
            FacePlayer();

            if (CanSpit())
            {
                // DISABLE shooting capability immediately after shooting starts
                canShoot = false;
                Debug.Log("[AntSpitter] canShoot set to FALSE - shooting started");
                
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
            // Player is out of range - only patrol if we're not in any shooting state
            base.Update(); // Patrol
            UpdateAnimationStates();
        }
    }

// @SFX:TargetCheck
private bool IsPlayerInRange()
    {
        return Vector2.Distance(transform.position, playerTransform.position) <= spittingDistance;
    }

// @SFX:SpitReady
private bool CanSpit()
    {
        // Can only spit if:
        // 1. Explicitly allowed to shoot (canShoot)
        // 2. Cooldown has passed
        // 3. Not currently shooting (isSpitting)
        // 4. Not in spitting lock period (isSpittingLocked)
        // 5. Not running away (isRunningAway)
        bool canSpit = canShoot && 
                      Time.time >= lastSpitTime + spitCooldown && 
                      !isSpitting && 
                      !isSpittingLocked && 
                      !isRunningAway;
        return canSpit;
    }

// @SFX:Aim
private void FacePlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }

// @SFX:SpitStart
private void Spit()
    {
        if (projectilePrefab != null && spitPoint != null)
        {
            Vector2 targetDirection = CalculateTargetDirection();
            SpawnProjectile(targetDirection);
        }
    }

// @SFX:SpitBurst
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

// @SFX:AimPredict
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
                float timeToTarget = Vector2.Distance(GetActiveSpitPoint().position, targetPosition) / projectileSpeed;
                targetPosition += playerRb.velocity * timeToTarget * predictionMultiplier;
            }
        }
        
        Vector2 direction = (targetPosition - (Vector2)GetActiveSpitPoint().position).normalized;
        Debug.Log($"[AntSpitter] CalculateTargetDirection - SpitPoint: {GetActiveSpitPoint().position}, TargetPos: {targetPosition}, Direction: {direction}");
        return direction;
    }

// @SFX:SpitFire
private void SpawnProjectile(Vector2 direction)
    {
        // Check if projectile prefab is valid
        if (projectilePrefab == null)
        {
            Debug.LogError("[AntSpitter] ProjectilePrefab is null!");
            return;
        }
        
        // Use multiple spawn points if enabled and available
        if (useMultipleSpitPoints && spitPoints != null && spitPoints.Length > 0)
        {
            SpawnProjectilesAtMultiplePoints(direction);
        }
        else
        {
            // Fallback to single spawn point
            SpawnProjectileAtSinglePoint(direction, spitPoint);
        }
    }
    
// @SFX:SpitFireMulti
private void SpawnProjectilesAtMultiplePoints(Vector2 direction)
    {
        for (int i = 0; i < spitPoints.Length; i++)
        {
            if (spitPoints[i] != null)
            {
                SpawnProjectileAtSinglePoint(direction, spitPoints[i]);
            }
            else
            {
                Debug.LogWarning($"[AntSpitter] SpitPoint at index {i} is null!");
            }
        }
    }
    
// @SFX:SpitFireSingle
private void SpawnProjectileAtSinglePoint(Vector2 direction, Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("[AntSpitter] SpawnPoint is null!");
            return;
        }
        
        // Calculate rotation angle towards target direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        GameObject proj = Instantiate(projectilePrefab, spawnPoint.position, rotation);

        // Nastav směr projektilu a launch delay
        Projectiles projectile = proj.GetComponent<Projectiles>();
        if (projectile != null)
        {
            projectile.SetDirection(direction);
            projectile.SetLaunchDelay(projectileLaunchDelay);
            // Set speed on the spawned instance, not the prefab
            projectile.speed = projectileSpeed;
        }
        else
        {
            Debug.LogError("[AntSpitter] Projectile component not found on spawned projectile!");
        }

        // Poznámka: Rigidbody2D velocity se už nenastavuje, protože Projectiles script řídí pohyb
    }

    // Debug vizualizace v editoru
// @SFX:DebugGizmos
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
                float timeToTarget = Vector2.Distance(GetActiveSpitPoint().position, playerTransform.position) / projectileSpeed;
                Vector2 predictedPos = (Vector2)playerTransform.position + playerRb.velocity * timeToTarget * predictionMultiplier;
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(predictedPos, 0.5f);
                Gizmos.DrawLine(transform.position, predictedPos);
            }
        }

        // Zobraz spitPoint(s)
        if (useMultipleSpitPoints && spitPoints != null && spitPoints.Length > 0)
        {
            // Zobraz všechny spitPoints
            for (int i = 0; i < spitPoints.Length; i++)
            {
                if (spitPoints[i] != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(spitPoints[i].position, 0.2f);
                    // Zobraz číslo spitPointu
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(spitPoints[i].position + Vector3.up * 0.3f, Vector3.one * 0.1f);
                }
            }
        }
        else if (spitPoint != null)
        {
            // Zobraz single spitPoint
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spitPoint.position, 0.2f);
        }
    }

    // Spitting lock mechanics
// @SFX:SpitLock
private void StartSpittingLock()
    {
        Debug.Log($"[AntSpitter] StartSpittingLock - Duration: {spittingLockDuration}s");
        isSpittingLocked = true;
        // DON'T set isSpitting = true here! It should only be true during actual shooting
        // isSpitting = true; // REMOVED - this was causing the ant to stay in shooting state
        spittingLockStartTime = Time.time;
        
        // No longer need the coroutine since we handle it in Update
    }
    
    // This method is no longer needed since we handle spitting lock directly in Update
/*
    // @SFX:SpitLockEnd
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
// @SFX:RunAwayStart
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
    
// @SFX:RunAwayLoop
private void HandleRunAway()
    {
        Debug.Log($"[AntSpitter] HandleRunAway - Time elapsed: {Time.time - runAwayStartTime}/{runAwayDuration}");
        
        // Check if run away duration is over
        if (Time.time - runAwayStartTime >= runAwayDuration)
        {
            Debug.Log("[AntSpitter] HandleRunAway - Run away duration ended");
            isRunningAway = false;
            
            // RE-ENABLE shooting capability after runaway completes
            canShoot = true;
            Debug.Log("[AntSpitter] canShoot set to TRUE - runaway completed");
            return;
        }
        
        // Move away from player - ALWAYS move regardless of player position
        Vector2 targetPosition = (Vector2)transform.position + runAwayDirection * runAwaySpeed * Time.deltaTime;
        transform.position = targetPosition;
        
        // Update animations during run away
        UpdateAnimationStates();
    }
    
    // Animation management
// @SFX:AnimState
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
// @SFX:AnimParamCheck
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

// @SFX:ActiveSpitPoint
private Transform GetActiveSpitPoint()
    {
        if (useMultipleSpitPoints && spitPoints != null && spitPoints.Length > 0)
        {
            // Return the first valid spit point for calculations
            for (int i = 0; i < spitPoints.Length; i++)
            {
                if (spitPoints[i] != null)
                    return spitPoints[i];
            }
        }
        
        // Fallback to single spit point
        return spitPoint;
    }
}