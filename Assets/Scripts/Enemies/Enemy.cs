using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float knockbackDuration = 0.5f;
    [SerializeField] public bool damageable = true;
    [SerializeField] public bool knockbackOnly = false;
    [SerializeField] public float knockbackMultiplier = 3f;
    [SerializeField] public float upKnockBoost = 1.5f;

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

        if (!damageable)
        {
            if (knockbackOnly)
            {
                var urchin2 = GetComponent<Urchin>();
                if (urchin2 != null && urchin2.IsRollingState)
                {
                    urchin2.PrepareForKnockbackDuringRoll();
                }
                Vector2 boosted = new Vector2(
                    knockbackDirection.x,
                    Mathf.Max(knockbackDirection.y, 0f) + upKnockBoost
                ) * knockbackMultiplier;
                StartCoroutine(ApplyKnockback(boosted));
            }
            return;
        }

        anim.SetTrigger("hurt");
        currentHealth -= damage;

        // Apply knockback (special-case Urchin states)
        var urchin = GetComponent<Urchin>();
        if (urchin != null)
        {
            if (urchin.IsPatrolState)
            {
                urchin.OnDamagedDuringPatrol(knockbackDirection);
                
            }
            else if (urchin.IsRollingState)
            {
                urchin.PrepareForKnockbackDuringRoll();
                Vector2 knockUp = new Vector2(knockbackDirection.x, Mathf.Max(knockbackDirection.y, 0f) + 1f);
                StartCoroutine(ApplyKnockback(knockUp * knockbackMultiplier));
            }
            else
            {
                StartCoroutine(ApplyKnockback(knockbackDirection * knockbackMultiplier));
            }
        }
        else
        {
            StartCoroutine(ApplyKnockback(knockbackDirection * knockbackMultiplier));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator ApplyKnockback(Vector2 knockbackDirection)
    {
        isKnockedBack = true;
        rb2d.velocity = Vector2.zero;
        rb2d.AddForce(knockbackDirection, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;
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

    // New getter for FollowingEnemy
    public bool IsKnockedBack()
    {
        return isKnockedBack;
    }
}
