using UnityEngine;
using System.Collections;

public class ShooterEnemy : MonsterPatrol
{
    [Header("Shooter Settings")]
    public Transform playerTransform;
    public float shootingDistance = 6f;
    public float shootCooldown = 2f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    [Header("Targeting Settings")]
    public float projectileSpeed = 8f;
    public bool usePredictiveAiming = true;
    public float predictionMultiplier = 1f;
    
    [Header("Burst Settings")]
    public bool useBurstFire = false;
    public int burstCount = 2;
    public float burstDelay = 0.3f;
    
    [Header("Projectile Launch Settings")]
    public float projectileLaunchDelay = 0.5f; // Doba čekání před vypuštěním projektilu
    
    [Header("Run Away Settings")]
    public float runAwayDuration = 2f;
    public float runAwaySpeed = 4f;
    public float runAwayDistance = 8f;

    private float lastShootTime = -Mathf.Infinity;
    private bool isShooting = false;
    private Rigidbody2D playerRb;
    
    // Shooting timer mechanics
    private bool isShootingLocked = false;
    private float shootingLockDuration = 1f; // Duration to lock movement during shooting
    private float shootingLockStartTime;
    
    // Run away state tracking
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
        // Check if enemy is dead and stop all actions
        Enemy enemyComponent = GetComponent<Enemy>();
        if (enemyComponent != null && enemyComponent.IsDead()) return;
        
        // Handle shooting lock timer
        if (isShootingLocked)
        {
            if (Time.time - shootingLockStartTime >= shootingLockDuration)
            {
                isShootingLocked = false;
                isShooting = false;
            }
            else
            {
                // During shooting lock, only update animations
                UpdateAnimationStates();
                return;
            }
        }
        
        // Handle run away state
        if (isRunningAway)
        {
            HandleRunAway();
            return;
        }
        
        if (isShooting) return; // Během střelby neprovádíme patrol

        if (playerTransform != null && IsPlayerInRange())
        {
            FacePlayer();

            if (CanShoot())
            {
                if (useBurstFire)
                {
                    StartCoroutine(BurstShoot());
                }
                else
                {
                    Shoot();
                }
                lastShootTime = Time.time;
                
                // Start shooting lock
                StartShootingLock();
            }

            // Animation states
            UpdateAnimationStates();
        }
        else
        {
            base.Update(); // Patrol
            UpdateAnimationStates();
        }
    }

    private bool IsPlayerInRange()
    {
        return Vector2.Distance(transform.position, playerTransform.position) <= shootingDistance;
    }

    private bool CanShoot()
    {
        return Time.time >= lastShootTime + shootCooldown;
    }

    private void FacePlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }

    private void Shoot()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            Vector2 targetDirection = CalculateTargetDirection();
            SpawnProjectile(targetDirection);
        }
    }

    private IEnumerator BurstShoot()
    {
        isShooting = true;
        
        for (int i = 0; i < burstCount; i++)
        {
            // Check if this enemy still exists
            if (this == null || gameObject == null)
            {
                yield break;
            }
            
            Vector2 targetDirection = CalculateTargetDirection();
            SpawnProjectile(targetDirection);
            
            if (i < burstCount - 1) // Don't wait after the last shot
            {
                yield return new WaitForSeconds(burstDelay);
            }
        }
        
        // Check if this enemy still exists before setting isShooting
        if (this != null && gameObject != null)
        {
            isShooting = false;
        }
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
                float timeToTarget = Vector2.Distance(firePoint.position, targetPosition) / projectileSpeed;
                targetPosition += playerRb.velocity * timeToTarget * predictionMultiplier;
            }
        }
        
        Vector2 direction = (targetPosition - (Vector2)firePoint.position).normalized;
        return direction;
    }

    private void SpawnProjectile(Vector2 direction)
    {
        // Check if projectile prefab and fire point are valid
        if (projectilePrefab == null)
        {
            Debug.LogError("[ShooterEnemy] ProjectilePrefab is null!");
            return;
        }
        
        if (firePoint == null)
        {
            Debug.LogError("[ShooterEnemy] FirePoint is null!");
            return;
        }
        
        // Calculate rotation angle towards target direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, rotation);

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
            Debug.LogError("[ShooterEnemy] Projectile component not found on spawned projectile!");
        }

        // Poznámka: Rigidbody2D velocity se už nenastavuje, protože Projectiles script řídí pohyb
    }

    // Shooting lock mechanics
    private void StartShootingLock()
    {
        if (playerTransform == null) return;
        
        Debug.Log($"[ShooterEnemy] StartShootingLock - Duration: {shootingLockDuration}s");
        isShootingLocked = true;
        isShooting = true;
        shootingLockStartTime = Time.time;
        
        // Start coroutine to handle the end of shooting lock
        StartCoroutine(HandleShootingLockEnd());
    }
    
    private IEnumerator HandleShootingLockEnd()
    {
        yield return new WaitForSeconds(shootingLockDuration);
        
        // Check if this enemy still exists
        if (this == null || gameObject == null)
        {
            yield break;
        }
        
        Debug.Log("[ShooterEnemy] HandleShootingLockEnd - Shooting lock ended");
        
        // Check if player is still in range after shooting lock
        if (playerTransform != null && IsPlayerInRange())
        {
            Debug.Log("[ShooterEnemy] Player still in range after shooting lock - starting run away");
            StartRunAway();
        }
        else
        {
            Debug.Log("[ShooterEnemy] Player not in range after shooting lock - resuming normal behavior");
            isShootingLocked = false;
            isShooting = false;
        }
    }

    // Run away mechanics
    private void StartRunAway()
    {
        Debug.Log($"[ShooterEnemy] StartRunAway - Duration: {runAwayDuration}s");
        isRunningAway = true;
        runAwayStartTime = Time.time;
        
        // Calculate direction away from player
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        runAwayDirection = -directionToPlayer; // Opposite direction
        
        // Flip sprite to face away from player
        if (runAwayDirection.x > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }
    
    private void HandleRunAway()
    {
        // Check if run away duration has expired
        if (Time.time - runAwayStartTime >= runAwayDuration)
        {
            Debug.Log("[ShooterEnemy] HandleRunAway - Run away duration ended");
            isRunningAway = false;
            return;
        }
        
        // Check if far enough from player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer >= runAwayDistance)
        {
            Debug.Log("[ShooterEnemy] HandleRunAway - Far enough from player, stopping run away");
            isRunningAway = false;
            return;
        }
        
        // Move away from player
        Vector2 movement = runAwayDirection * runAwaySpeed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);
        
        // Update animation states
        UpdateAnimationStates();
    }

    // Debug vizualizace v editoru
    private void OnDrawGizmosSelected()
    {
        // Zobraz dosah střelby
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingDistance);

        // Zobraz směr k hráči
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, playerTransform.position);

            // Zobraz predikovanou pozici
            if (usePredictiveAiming && playerRb != null)
            {
                float timeToTarget = Vector2.Distance(firePoint.position, playerTransform.position) / projectileSpeed;
                Vector2 predictedPos = (Vector2)playerTransform.position + playerRb.velocity * timeToTarget * predictionMultiplier;
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(predictedPos, 0.5f);
                Gizmos.DrawLine(transform.position, predictedPos);
            }
        }

        // Zobraz firePoint
        if (firePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }

    // Animation management
    private void UpdateAnimationStates()
    {
        if (anim == null) return;
        
        // Set animation parameters based on current state
        if (HasAnimatorParameter("isShooting"))
            anim.SetBool("isShooting", isShooting);
            
        if (HasAnimatorParameter("isRunning"))
            anim.SetBool("isRunning", isRunningAway);
            
        // Improved walking detection
        bool isMoving = false;
        
        if (isRunningAway)
        {
            // During run away, always consider as moving
            isMoving = true;
        }
        else if (playerTransform != null && IsPlayerInRange() && !isShooting && !isShootingLocked)
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
            anim.SetBool("isWalking", !isShooting && !isShootingLocked && isMoving);
            
        if (HasAnimatorParameter("isIdle"))
            anim.SetBool("isIdle", !isShooting && !isShootingLocked && !isRunningAway && !isMoving);
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
