using UnityEngine;

public class MonsterPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;

    protected int patrolDestination;
    protected Vector3 originalScale;
    protected Rigidbody2D rb;
    protected Animator anim;

    protected virtual void Start()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        Patrol();
    }

    protected void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[patrolDestination];
        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);

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
