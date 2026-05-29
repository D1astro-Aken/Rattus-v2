using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] public float FlowSpeed = 2.0f;
    [SerializeField] private Camera followCamera;
    [SerializeField] public Transform target;
    [SerializeField] public float yOffset = 1f;
    [SerializeField] public float yOffsetChangeSpeed = 3f; // Speed of changing yOffset
    [SerializeField] public float yOffsetReturnSpeed = 2f; // Speed of returning yOffset
    [SerializeField] public float yOffsetMin = 0f; // Minimum value for yOffset
    [SerializeField] public float yOffsetMax = 3f; // Maximum value for yOffset

    private float initialYOffset; // To store the original yOffset
    private float baseOrthoSize;
    private float baseFov;

    [Header("Idle Sit Zoom")]
    [SerializeField] private bool enableIdleSitZoom = true;
    [SerializeField] private float idleSitZoomMultiplier = 0.85f;
    [SerializeField] private float idleSitZoomSpeed = 2.5f;

    void Start()
    {
        initialYOffset = yOffset; // Save the original yOffset
        if (followCamera == null)
            followCamera = GetComponent<Camera>();
        if (followCamera == null)
            followCamera = Camera.main;
        if (followCamera != null)
        {
            baseOrthoSize = followCamera.orthographicSize;
            baseFov = followCamera.fieldOfView;
        }
    }

    void Update()
    {
        if (!target)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                target = player.transform;
            }
        }

        if (!target)
        {
            return;
        }

        if (followCamera == null)
            followCamera = GetComponent<Camera>();
        if (followCamera == null)
            followCamera = Camera.main;
        if (followCamera != null)
        {
            if (followCamera.orthographic && baseOrthoSize <= 0f)
                baseOrthoSize = followCamera.orthographicSize;
            if (!followCamera.orthographic && baseFov <= 0f)
                baseFov = followCamera.fieldOfView;
        }

        // Check for up and down arrow key input to adjust yOffset
        if (Input.GetKey(KeyCode.W))
        {
            yOffset += yOffsetChangeSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            yOffset -= yOffsetChangeSpeed * Time.deltaTime;
        }
        else
        {
            // Smoothly return yOffset to its original value when no key is pressed
            yOffset = Mathf.Lerp(yOffset, initialYOffset, yOffsetReturnSpeed * Time.deltaTime);
        }

        // Clamp yOffset within the defined range
        yOffset = Mathf.Clamp(yOffset, yOffsetMin, yOffsetMax);

        // Update camera position
        Vector3 newPos = new Vector3(target.position.x, target.position.y + yOffset, -10f);
        transform.position = Vector3.Slerp(transform.position, newPos, FlowSpeed * Time.deltaTime);

        if (enableIdleSitZoom && followCamera != null)
        {
            float desiredOrtho = baseOrthoSize;
            float desiredFov = baseFov;
            IdleSit idleSit =
                target.GetComponent<IdleSit>() ??
                target.GetComponentInParent<IdleSit>() ??
                target.GetComponentInChildren<IdleSit>();
            if (idleSit != null && idleSit.IsSitting)
            {
                float m = Mathf.Clamp(idleSitZoomMultiplier, 0.1f, 2f);
                desiredOrtho = baseOrthoSize * m;
                desiredFov = baseFov * m;
            }

            if (followCamera.orthographic)
            {
                followCamera.orthographicSize = Mathf.Lerp(
                    followCamera.orthographicSize,
                    desiredOrtho,
                    idleSitZoomSpeed * Time.deltaTime
                );
            }
            else
            {
                followCamera.fieldOfView = Mathf.Lerp(
                    followCamera.fieldOfView,
                    desiredFov,
                    idleSitZoomSpeed * Time.deltaTime
                );
            }
        }
    }
}
