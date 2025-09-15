using UnityEngine;

public class FollowingEnemy : MonsterPatrol
{
    [Header("Follow Settings")]
    public Transform playerTransform;
    public float followDistance = 4f;

    protected override void Update()
    {
        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < followDistance)
        {
            FollowPlayer();
        }
        else
        {
            base.Update(); // použije Patrol() z parent class
        }
    }

    private void FollowPlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);

        // otoèení smìrem k hráèi
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        if (anim != null)
            anim.SetBool("isFollowing", true);
    }
}
