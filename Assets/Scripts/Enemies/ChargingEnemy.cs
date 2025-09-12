using System.Collections;
using UnityEngine;

public class ChargingEnemy : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    private int patrolIndex;
    private Vector3 originalScale;
    private Rigidbody2D rb;

    public Transform playerTransform;
    public float triggerDistance = 4f;

    [Header("Charge Settings")]
    public float chargeSpeed = 8f;
    public float chargeDuration = 1f;
    public float chargeCooldown = 3f;

    private bool isCharging = false;
    private bool canCharge = true;

    private void Start()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!isCharging && Vector2.Distance(transform.position, playerTransform.position) < triggerDistance)
        {
            TryCharge();
        }

        if (!isCharging)
        {
            Patrol();
        }
    }

    private void TryCharge()
    {
        if (canCharge)
        {
            StartCoroutine(ChargeTowardsPlayer());
        }
    }

    private IEnumerator ChargeTowardsPlayer()
    {
        isCharging = true;
        canCharge = false;

        // Urèi smìr k hráèi pouze na ose X
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;

        // Otoèení sprite
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            rb.velocity = new Vector2(direction.x * chargeSpeed, rb.velocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
        isCharging = false;

        yield return new WaitForSeconds(chargeCooldown);
        canCharge = true;
    }

    private void Patrol()
    {
        Transform target = patrolPoints[patrolIndex];
        transform.position = Vector2.MoveTowards(transform.position, target.position, patrolSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.2f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

            float direction = target.position.x - transform.position.x;
            transform.localScale = new Vector3(Mathf.Sign(direction) * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }
}