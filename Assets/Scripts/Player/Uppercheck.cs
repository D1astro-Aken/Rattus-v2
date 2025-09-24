using UnityEngine;

public class UpperCheck : MonoBehaviour
{
    [Header("Layers to Block Ledge Grab")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [HideInInspector] public bool isClear = true;

    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void FixedUpdate()
    {
        CheckUpperSpace();
    }

    private void CheckUpperSpace()
    {
        // Spoèítáme velikost a pozici pro kontrolu
        Vector2 checkSize = boxCollider.bounds.size;
        Vector2 checkCenter = boxCollider.bounds.center;

        // Raycast nebo BoxCast nahoru
        RaycastHit2D hit = Physics2D.BoxCast(
            checkCenter,
            checkSize,
            0f,
            Vector2.up,
            0.05f,
            groundLayer | wallLayer
        );

        isClear = hit.collider == null;
    }

    private void OnDrawGizmos()
    {
        if (boxCollider == null) return;

        Gizmos.color = isClear ? Color.green : Color.red;
        Gizmos.DrawWireCube(boxCollider.bounds.center, boxCollider.bounds.size);
    }
}
