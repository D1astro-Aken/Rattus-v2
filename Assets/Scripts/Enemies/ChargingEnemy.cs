using System.Collections;
using UnityEngine;

public class ChargingEnemy : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    private int patrolDestination = 0;
    private Vector3 originalScale;
    private Rigidbody2D rb;
    private Animator anim;

    public Transform playerTransform;
    public float triggerDistance = 4f;

    [Header("Charge Settings")]
    public bool enableCharge = true;
    public float windUpDuration = 0.5f;
    public float chargeSpeed = 8f;
    public float chargeDuration = 1f;
    public float chargeCooldown = 3f;
    public float stunDuration = 1f;

    private bool isCharging = false;
    private bool canCharge = true;
    private bool isStunned = false;

    private void Start()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!enableCharge)
        {
            Patrol();
            return;
        }

        if (isCharging || isStunned)
            return;

        if (Vector2.Distance(transform.position, playerTransform.position) < triggerDistance)
        {
            TryCharge();
        }
        else
        {
            Patrol();
        }
    }

    private void TryCharge()
    {
        if (canCharge)
        {
            StartCoroutine(ChargeSequence());
        }
    }

    private IEnumerator ChargeSequence()
    {
        isCharging = true;
        canCharge = false;

        rb.velocity = Vector2.zero;

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;

        // Face player
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        // Wind-up animation
        if (anim != null) anim.SetTrigger("WindUp");
        yield return new WaitForSeconds(windUpDuration);
        if (anim != null) anim.ResetTrigger("WindUp");

        // Charge
        if (anim != null) anim.SetTrigger("Charge");
        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            rb.velocity = new Vector2(direction.x * chargeSpeed, rb.velocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.velocity = Vector2.zero;
        if (anim != null) anim.ResetTrigger("Charge");

        // Stun
        isStunned = true;
        if (anim != null) anim.SetTrigger("Stun");
        yield return new WaitForSeconds(stunDuration);
        if (anim != null) anim.ResetTrigger("Stun");
        isStunned = false;

        // Face patrol point after stun if player not in range
        FacePatrolPoint();

        // Cooldown
        yield return new WaitForSeconds(chargeCooldown);
        canCharge = true;
        isCharging = false;
    }

    private void FacePatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        if (Vector2.Distance(transform.position, playerTransform.position) < triggerDistance) return;

        Transform target = patrolPoints[patrolDestination];
        float dir = target.position.x - transform.position.x;

        if (Mathf.Abs(dir) < 0.01f && patrolPoints.Length > 1)
        {
            int prevIndex = (patrolDestination - 1 + patrolPoints.Length) % patrolPoints.Length;
            dir = patrolPoints[prevIndex].position.x - transform.position.x;
        }

        if (dir != 0)
            transform.localScale = new Vector3(Mathf.Sign(dir) * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }

    private void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[patrolDestination];
        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, patrolSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.2f)
        {
            if (patrolDestination == 0)
            {
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
                patrolDestination = 1;
            }
            else
            {
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
                patrolDestination = 0;
            }
        }
    }
}
