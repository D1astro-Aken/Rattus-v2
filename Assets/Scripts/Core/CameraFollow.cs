using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] public float FlowSpeed = 2.0f;
    [SerializeField] public Transform target;
    [SerializeField] public float yOffset = 1f;
    [SerializeField] public float yOffsetChangeSpeed = 3f; // Speed of changing yOffset
    [SerializeField] public float yOffsetReturnSpeed = 2f; // Speed of returning yOffset
    [SerializeField] public float yOffsetMin = 0f; // Minimum value for yOffset
    [SerializeField] public float yOffsetMax = 3f; // Maximum value for yOffset

    private float initialYOffset; // To store the original yOffset

    void Start()
    {
        initialYOffset = yOffset; // Save the original yOffset
        
    }

    void Update()
    {
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
    }
}
