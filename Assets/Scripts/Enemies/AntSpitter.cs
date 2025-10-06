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
    
    [Header("Burst Settings")]
    public bool useBurstFire = true;
    public int burstCount = 3;
    public float burstDelay = 0.2f;

    private float lastSpitTime = -Mathf.Infinity;
    private bool isSpitting = false;
    private Rigidbody2D playerRb;

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
            Projectile projComponent = projectilePrefab.GetComponent<Projectile>();
            if (projComponent != null)
            {
                projComponent.speed = projectileSpeed;
            }
        }
    }

    protected override void Update()
    {
        if (isSpitting) return; // Během střelby neprovádíme patrol

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
            }

            // Nastav animaci střelby
            if (anim != null)
                anim.SetBool("isSpitting", true);
        }
        else
        {
            base.Update(); // Patrol
            if (anim != null)
                anim.SetBool("isSpitting", false);
        }
    }

    private bool IsPlayerInRange()
    {
        return Vector2.Distance(transform.position, playerTransform.position) <= spittingDistance;
    }

    private bool CanSpit()
    {
        return Time.time >= lastSpitTime + spitCooldown;
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
        isSpitting = true;

        for (int i = 0; i < burstCount; i++)
        {
            if (projectilePrefab != null && spitPoint != null)
            {
                Vector2 targetDirection = CalculateTargetDirection();
                SpawnProjectile(targetDirection);
            }

            if (i < burstCount - 1) // Nečekej po posledním výstřelu
            {
                yield return new WaitForSeconds(burstDelay);
            }
        }

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
        GameObject proj = Instantiate(projectilePrefab, spitPoint.position, spitPoint.rotation);

        // Nastav směr projektilu
        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.SetDirection(direction);
        }

        // Alternativně, pokud projektil používá Rigidbody2D
        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.velocity = direction * projectileSpeed;
        }
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
}