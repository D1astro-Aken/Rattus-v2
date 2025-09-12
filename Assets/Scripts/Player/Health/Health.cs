using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float startingHealth;
    public float currentHealth { get; private set; }
    private Animator anim;
    private bool dead;

    [Header("iFrames")]
    [SerializeField] private float iFramesDuration;
    [SerializeField] private int numberOfFlashes;
    private SpriteRenderer spriteRend;

    [Header("Respawn")]
    [SerializeField] private Vector3 respawnPoint;
    [SerializeField] private float respawnDelay = 2f;

    private void Awake()
    {
        currentHealth = startingHealth;
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();
        respawnPoint = transform.position; // Set initial respawn point to starting position
    }

    public void TakeDamage(float _damage)
    {
        if (dead) return;

        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (currentHealth > 0)
        {
            anim.SetTrigger("hurt");
            StartCoroutine(Invulnerability());
        }
        else
        {
            if (!dead)
            {
                GetComponent<PlayerMovement>().enabled = false;
                anim.SetBool("grounded", true);
                anim.SetTrigger("die");
                dead = true;

                // Start respawn after a delay
                StartCoroutine(Respawn());
            }
        }
    }

    public void AddHealth(float _value)
    {
        currentHealth = Mathf.Clamp(currentHealth + _value, 0, startingHealth);
    }

    private IEnumerator Invulnerability()
    {
        Physics2D.IgnoreLayerCollision(3, 9, true); // Disable collisions
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRend.color = new Color(1, 1, 1, 0.5f); // Half transparent
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
            spriteRend.color = Color.white; // Back to full visibility
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
        }
        Physics2D.IgnoreLayerCollision(3, 9, false); // Re-enable collisions
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Reset player state
        currentHealth = startingHealth;
        dead = false;

        // Reset position
        transform.position = respawnPoint;

        // Enable movement again
        GetComponent<PlayerMovement>().enabled = true;

        // Reset animations
        anim.ResetTrigger("die");
        anim.SetBool("grounded", true);
    }

    // Call this method to set a new respawn point (e.g., from a checkpoint)
    public void SetRespawnPoint(Vector3 newPoint)
    {
        respawnPoint = newPoint;
    }
}
