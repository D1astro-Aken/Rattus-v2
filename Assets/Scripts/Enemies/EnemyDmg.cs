using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDmg : MonoBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private Health playerHealth;
    [SerializeField] private float playerKnockbackMultiplier = 8f;
    [SerializeField] private float playerKnockbackMultiplierRollingUrchin = 16f;
    [SerializeField] private float playerKnockUpBoost = 1f;

    private Enemy enemy; // Reference to the Enemy script

    private void Start()
    {
        // Ensure the Enemy script is attached to the same GameObject
        enemy = GetComponent<Enemy>();

        if (enemy == null)
        {
            Debug.LogError("Enemy script not found on the same GameObject! Please attach the Enemy script.");
        }

        // Ensure the playerHealth is assigned in the Inspector
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth is not assigned. Please assign it in the Inspector.");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (enemy == null || enemy.IsDead()) return;

        var h = playerHealth != null ? playerHealth : collision.gameObject.GetComponent<Health>();
        if (h != null) h.TakeDamage(damage);

        var playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        float mult = playerKnockbackMultiplier;
        var urchin = GetComponent<Urchin>();
        if (urchin != null && urchin.IsRollingState)
            mult = playerKnockbackMultiplierRollingUrchin;

        Vector2 dir = (collision.transform.position - transform.position).normalized;
        Vector2 knock = new Vector2(dir.x, Mathf.Max(dir.y, 0f) + playerKnockUpBoost) * mult;
        playerRb.velocity = Vector2.zero;
        playerRb.AddForce(knock, ForceMode2D.Impulse);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (enemy == null || enemy.IsDead()) return;

        var h = playerHealth != null ? playerHealth : other.GetComponent<Health>();
        if (h != null) h.TakeDamage(damage);

        var playerRb = other.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        float mult = playerKnockbackMultiplier;
        var urchin = GetComponent<Urchin>();
        if (urchin != null && urchin.IsRollingState)
            mult = playerKnockbackMultiplierRollingUrchin;

        Vector2 dir = (other.transform.position - transform.position).normalized;
        Vector2 knock = new Vector2(dir.x, Mathf.Max(dir.y, 0f) + playerKnockUpBoost) * mult;
        playerRb.velocity = Vector2.zero;
        playerRb.AddForce(knock, ForceMode2D.Impulse);
    }
}
