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
    public bool predictPlayerMovement = true;
    public float predictionTime = 0.3f;
    
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

        // Nastav rychlost projektilu v prefabu pokud existuje
        if (projectilePrefab != null)
        {
            Projectiles projComponent = projectilePrefab.GetComponent<Projectiles>();
            if (projComponent != null)
            {
                projComponent.speed = projectileSpeed;
            }
        }
    }

    protected override void Update()
    {
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
            if (projectilePrefab != null && firePoint != null)
            {
                Vector2 targetDirection = CalculateTargetDirection();
                SpawnProjectile(targetDirection);
            }

            if (i < burstCount - 1) // Nečekej po posledním výstřelu
            {
                yield return new WaitForSeconds(burstDelay);
            }
        }

        isShooting = false;
    }

    private Vector2 CalculateTargetDirection()
    {
        Vector2 targetPosition = playerTransform.position;

        // Predikce pohybu hráče
        if (predictPlayerMovement && playerRb != null)
        {
            Vector2 playerVelocity = playerRb.velocity;
            targetPosition += playerVelocity * predictionTime;
        }

        // Vypočítej směr k cílové pozici
        Vector2 direction = (targetPosition - (Vector2)firePoint.position).normalized;
        return direction;
    }

    private void SpawnProjectile(Vector2 direction)
    {
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
        }

        // Poznámka: Rigidbody2D velocity se už nenastavuje, protože Projectiles script řídí pohyb
    }

    // Shooting lock mechanics
    private void StartShootingLock()
    {
        isShootingLocked = true;
        shootingLockStartTime = Time.time;
        isShooting = true;
        
        // Start coroutine to handle run away after shooting lock
        StartCoroutine(HandleShootingLockEnd());
    }
    
    private IEnumerator HandleShootingLockEnd()
    {
        yield return new WaitForSeconds(shootingLockDuration);
        
        // After shooting lock ends, start running away
        if (!isRunningAway)
        {
            StartRunAway();
        }
    }

    // Run away mechanics
    private void StartRunAway()
    {
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
            isRunningAway = false;
            return;
        }
        
        // Check if far enough from player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer >= runAwayDistance)
        {
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
            if (predictPlayerMovement && playerRb != null)
            {
                Vector2 predictedPos = (Vector2)playerTransform.position + playerRb.velocity * predictionTime;
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
