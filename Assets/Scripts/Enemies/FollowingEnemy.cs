using UnityEngine;

public class FollowingEnemy : MonsterPatrol
{
    [Header("Follow Settings")]
    public Transform playerTransform;
    public float followDistance = 4f;

    [Header("Follow Speed")]
    public float followSpeed = 3f;

    private Enemy enemyComponent;

    protected override void Start()
    {
        base.Start(); // Call MonsterPatrol.Start()

        // Get the Enemy component for health/knockback
        enemyComponent = GetComponent<Enemy>();
        if (enemyComponent == null)
            Debug.LogWarning("Enemy component not found on FollowingEnemy!");
    }

    protected override void Update()
    {
        if (enemyComponent != null && enemyComponent.IsDead()) return;

        // Stop following if knocked back
        if (enemyComponent != null && enemyComponent.IsKnockedBack()) return;

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < followDistance)
        {
            FollowPlayer();
        }
        else
        {
            if (anim != null)
                anim.SetBool("isFollowing", false); // stop follow animation

            base.Update(); // Patrol from MonsterPatrol
        }
    }

    private void FollowPlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, followSpeed * Time.deltaTime);

        // Flip to face the player
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        if (anim != null)
            anim.SetBool("isFollowing", true);
    }
}
