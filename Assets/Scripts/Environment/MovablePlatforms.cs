using UnityEngine;
using System.Collections.Generic;

public class MovablePlatforms : MonoBehaviour
{
    [Header("Movement Configuration")]
    [Tooltip("Points relative to the platform's starting position.")]
    [SerializeField] private List<Vector3> waypoints = new List<Vector3>();
    [SerializeField] private float speed = 2.0f;
    [SerializeField] private bool loop = false;
    [SerializeField] private bool pingPong = false; // Move back and forth

    private bool isActivated = false;
    private int currentWaypointIndex = 0;
    private Vector3 startPosition;
    private List<Vector3> globalWaypoints;
    private bool movingForward = true;
    
    public Vector3 CurrentVelocity { get; private set; }

    private void Start()
    {
        // Capture the initial position as the starting point (index 0)
        startPosition = transform.position;
        InitializeGlobalWaypoints();
    }

    private void InitializeGlobalWaypoints()
    {
        globalWaypoints = new List<Vector3>();
        // The first waypoint is always the start position
        globalWaypoints.Add(startPosition);
        
        // Add the configured waypoints (converting from local offset to global position)
        foreach (var point in waypoints)
        {
            globalWaypoints.Add(startPosition + point);
        }
    }

    private void Update()
    {
        // Debug activation
        if (Input.GetKeyDown(KeyCode.H))
        {
            isActivated = !isActivated;
        }

        if (isActivated && globalWaypoints != null && globalWaypoints.Count > 1)
        {
            MovePlatform();
        }
    }

    private void MovePlatform()
    {
        // Determine the target waypoint index
        int targetIndex;
        if (movingForward)
        {
            targetIndex = currentWaypointIndex + 1;
        }
        else
        {
            targetIndex = currentWaypointIndex - 1;
        }

        // Handle path completion / looping logic
        if (targetIndex >= globalWaypoints.Count)
        {
            if (loop)
            {
                // If looping, go back to start (index 0)
                // To make it smooth, you might want to move towards 0. 
                // For now, let's just set target to 0.
                targetIndex = 0;
            }
            else if (pingPong)
            {
                // Switch direction
                movingForward = false;
                targetIndex = currentWaypointIndex - 1;
            }
            else
            {
                // Stop if not looping or ping-ponging
                isActivated = false;
                CurrentVelocity = Vector3.zero;
                return;
            }
        }
        else if (targetIndex < 0)
        {
            // Only happens if pingPong is true and we went backwards past 0
            if (pingPong)
            {
                movingForward = true;
                targetIndex = currentWaypointIndex + 1;
            }
            else
            {
                targetIndex = 0; // Should not happen in other modes
            }
        }

        // Move towards the target
        Vector3 targetPosition = globalWaypoints[targetIndex];
        Vector3 previousPosition = transform.position;
        
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        
        // Calculate velocity
        if (Time.deltaTime > 0)
        {
            CurrentVelocity = (transform.position - previousPosition) / Time.deltaTime;
        }
        else
        {
            CurrentVelocity = Vector3.zero;
        }

        // Check if we reached the target
        if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
        {
            currentWaypointIndex = targetIndex;
        }
    }

    public void Activate()
    {
        isActivated = true;
    }

    public void Stop()
    {
        isActivated = false;
    }

    private void OnDrawGizmos()
    {
        // Use current position if in editor (not playing), or startPosition if playing
        Vector3 basePos = Application.isPlaying ? startPosition : transform.position;

        Gizmos.color = Color.yellow;
        // Draw start point
        Gizmos.DrawWireSphere(basePos, 0.2f);

        if (waypoints == null) return;

        Vector3 previousPoint = basePos;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 globalPoint = basePos + waypoints[i];
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(globalPoint, 0.2f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(previousPoint, globalPoint);
            
            previousPoint = globalPoint;
        }

        // Draw return line if looping
        if (loop && waypoints.Count > 0)
        {
            Gizmos.color = new Color(1, 1, 0, 0.5f); // Transparent yellow
            Gizmos.DrawLine(previousPoint, basePos);
        }
    }
}
