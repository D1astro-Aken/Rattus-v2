using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax multipliers")]
    [SerializeField] private float parallaxMultiplierX = 0.5f;
    [SerializeField] private float parallaxMultiplierY = 0.2f;

    private Vector3 lastCameraPosition;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        lastCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        transform.position += new Vector3(
            deltaMovement.x * parallaxMultiplierX,
            deltaMovement.y * parallaxMultiplierY,
            0
        );

        lastCameraPosition = cameraTransform.position;
    }
}
