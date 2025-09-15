using System.Collections;
using UnityEngine;

public class AmbushEnemyOneShot : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Ambush Settings")]
    public float triggerDistance = 3f;
    public float attackSpeed = 8f;
    public float attackDuration = 0.5f;
    public float fadeAlpha = 0.2f;      // pr�hlednost v klidov�m stavu
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.3f;

    private bool hasAttacked = false;
    private Vector3 originalScale;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;

        // Nastav�me pr�hlednost na fadeAlpha
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = fadeAlpha;
            spriteRenderer.color = c;
        }
    }

    private void Update()
    {
        if (hasAttacked || playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist < triggerDistance)
        {
            StartCoroutine(AmbushRoutine());
        }
    }

    private IEnumerator AmbushRoutine()
    {
        hasAttacked = true;

        // Fade-in efekt
        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            Color c = spriteRenderer.color;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(fadeAlpha, 1f, elapsed / fadeInDuration);
                spriteRenderer.color = c;
                yield return null;
            }
        }

        // Oto�en� sm�rem k hr��i
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        // �tok � rychl� pohyb sm�rem k hr��i
        float elapsedAttack = 0f;
        while (elapsedAttack < attackDuration)
        {
            rb.velocity = new Vector2(direction.x * attackSpeed, rb.velocity.y);
            elapsedAttack += Time.deltaTime;
            yield return null;
        }
        rb.velocity = Vector2.zero;

        // Fade-out p�ed despawn
        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            Color c = spriteRenderer.color;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                spriteRenderer.color = c;
                yield return null;
            }
        }

        Destroy(gameObject); // despawn nep��tele
    }
}
