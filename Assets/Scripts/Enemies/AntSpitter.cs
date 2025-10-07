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
    public bool predictPlayerMovement = true;
    public float predictionTime = 0.5f;
    
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
        // Handle spitting lock timer
        if (isSpittingLocked)
        {
            if (Time.time - spittingLockStartTime >= spittingLockDuration)
            {
                isSpittingLocked = false;
                isSpitting = false;
            }
            else
            {
                // During spitting lock, only update animations
                UpdateAnimationStates();
                return;
            }
        }
        
        if (isSpitting) return; // Během střelby neprovádíme patrol

        // Handle run away mechanic
        if (isRunningAway)
        {
            HandleRunAway();
            return;
        }

        if (playerTransform != null && IsPlayerInRange())
        {
            FacePlayer();

            if (CanSpit())
            {
                Debug.Log("[AntSpitter] Player in range and can spit - starting burst");
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
            else
            {
                Debug.Log($"[AntSpitter] Player in range but cannot spit - cooldown: {Time.time - lastSpitTime < spitCooldown}, isSpitting: {isSpitting}, isSpittingLocked: {isSpittingLocked}");
            }
            // Remove the automatic run away when can't spit - let the spitting lock handle it

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
        return Vector2.Distance(transform.position, playerTransform.position) <= spittingDistance;
    }

    private bool CanSpit()
    {
        bool canSpit = Time.time >= lastSpitTime + spitCooldown && !isSpitting && !isSpittingLocked;
        Debug.Log($"[AntSpitter] CanSpit check - Time: {Time.time}, LastSpit: {lastSpitTime}, Cooldown: {spitCooldown}, CanSpit: {canSpit}, isSpitting: {isSpitting}, isSpittingLocked: {isSpittingLocked}");
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
        Vector2 targetPosition = playerTransform.position;

        // Predikce pohybu hráče
        if (predictPlayerMovement && playerRb != null)
        {
            Vector2 playerVelocity = playerRb.velocity;
            targetPosition += playerVelocity * predictionTime;
        }

        // Vypočítej směr k cílové pozici
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
            if (predictPlayerMovement && playerRb != null)
            {
                Vector2 predictedPos = (Vector2)playerTransform.position + playerRb.velocity * predictionTime;
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
        isSpittingLocked = true;
        spittingLockStartTime = Time.time;
        isSpitting = true;
        
        // Start coroutine to handle run away after spitting lock
        StartCoroutine(HandleSpittingLockEnd());
    }
    
    private IEnumerator HandleSpittingLockEnd()
    {
        yield return new WaitForSeconds(spittingLockDuration);
        
        // After spitting lock ends, start running away only if player is still in range
        if (!isRunningAway && playerTransform != null && IsPlayerInRange())
        {
            StartRunAway();
        }
    }

    // Run away mechanic methods
    private void StartRunAway()
    {
        if (playerTransform == null) return;
        
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
        // Check if run away duration is over
        if (Time.time - runAwayStartTime >= runAwayDuration)
        {
            isRunningAway = false;
            // Don't return to patrol point - let the ant resume normal behavior
            // This allows the ant to potentially engage the player again
            return;
        }
        
        // Move away from player
        Vector2 targetPosition = (Vector2)transform.position + runAwayDirection * runAwaySpeed * Time.deltaTime;
        transform.position = targetPosition;
    }
    
    // Animation management
    private void UpdateAnimationStates()
    {
        if (anim == null) return;
        
        Debug.Log($"[AntSpitter] UpdateAnimationStates - isSpitting: {isSpitting}, isSpittingLocked: {isSpittingLocked}, isRunningAway: {isRunningAway}");
        
        // Set animation parameters based on current state
        if (HasAnimatorParameter("isSpitting"))
            anim.SetBool("isSpitting", isSpitting);
            
        if (HasAnimatorParameter("isRunning"))
            anim.SetBool("isRunning", isRunningAway);
            
        // Improved walking detection
        bool isMoving = false;
        
        if (isRunningAway)
        {
            // During run away, always consider as moving
            isMoving = true;
        }
        else if (playerTransform != null && IsPlayerInRange() && !isSpitting && !isSpittingLocked)
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
            anim.SetBool("isWalking", !isSpitting && !isSpittingLocked && isMoving);
            
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