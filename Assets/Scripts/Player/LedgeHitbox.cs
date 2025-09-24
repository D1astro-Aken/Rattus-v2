using UnityEngine;

public class LedgeHitbox : MonoBehaviour
{
    [HideInInspector] public bool canGrab = false;
    [HideInInspector] public Vector2 ledgePosition;

    [Header("Upper Check")]
    public UpperCheck upperCheck;

    [SerializeField] private LayerMask groundLayer;

    [HideInInspector] public Vector3 originalLocalPosition;

    private void Awake()
    {
        // Uložíme původní lokální pozici hitboxu
        originalLocalPosition = transform.localPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            // Pokud existuje UpperCheck, musí být prostor nad volný
            if (upperCheck == null || upperCheck.isClear)
            {
                canGrab = true;
                ledgePosition = other.ClosestPoint(transform.position);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            canGrab = false;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            if (upperCheck == null || upperCheck.isClear)
            {
                ledgePosition = other.ClosestPoint(transform.position);
            }
            else
            {
                canGrab = false;
            }
        }
    }
}
