using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float knockbackDuration = 0.5f; // Duration of knockback
    
    private int currentHealth;

    private bool dead = false;
    private bool isKnockedBack = false; // Tracks if the enemy is being knocked back
    private Animator anim;
    private Rigidbody2D rb2d;
    private Collider2D[] colliders;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        rb2d = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();
    }

    public void TakeDamage(int damage, Vector2 knockbackDirection)
    {
        if (dead || isKnockedBack) return;

        anim.SetTrigger("hurt");
        currentHealth -= damage;

        // Apply knockback
        StartCoroutine(ApplyKnockback(knockbackDirection));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator ApplyKnockback(Vector2 knockbackDirection)
    {
        isKnockedBack = true;
        rb2d.velocity = Vector2.zero; // Reset current velocity
        rb2d.AddForce(knockbackDirection * 3f, ForceMode2D.Impulse); // Apply knockback force (adjust magnitude)

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false; // Allow movement again
    }

    private void Die()
    {
        if (dead) return;

        anim.SetTrigger("die");
        dead = true;

        rb2d.velocity = Vector2.zero;
        rb2d.bodyType = RigidbodyType2D.Static;

        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        Destroy(gameObject, 1f);
    }

    public bool IsDead()
    {
        return dead;
    }
}
