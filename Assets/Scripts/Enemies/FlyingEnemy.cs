using UnityEngine;

public class FlyingEnemy : MonsterPatrol
{
    [Header("Flying Settings")]
    public Transform playerTransform;
    public float followDistance = 6f;

    protected override void Update()
    {
        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < followDistance)
        {
            FlyTowardsPlayer();
        }
        else
        {
            base.Update(); // fallback na patrolování
        }
    }

    private void FlyTowardsPlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);

        // otoèení podle smìru
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        if (anim != null)
            anim.SetBool("isFlying", true);
    }
}
