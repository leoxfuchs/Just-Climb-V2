using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 0.8f, -1.8f); // Close 3rd person, slightly zoomed out
    public float smoothTime = 0.3f;
    public float rotationSpeed = 5f;
    
    private Vector3 velocity = Vector3.zero;
    private float currentRotationAngle = 0f;
    
    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
    
    void LateUpdate()
    {
        // DISABLED - Using FirstPersonCamera script instead
        // This script was forcing the camera to look at the player
        // which was overriding mouse rotation
        return;
        
        /*
        if (target == null) return;
        
        // Allow camera rotation with Q and E keys
        if (Input.GetKey(KeyCode.Q))
        {
            currentRotationAngle += rotationSpeed;
        }
        if (Input.GetKey(KeyCode.E))
        {
            currentRotationAngle -= rotationSpeed;
        }
        
        // Calculate rotated offset
        Quaternion rotation = Quaternion.Euler(0, currentRotationAngle, 0);
        Vector3 rotatedOffset = rotation * offset;
        
        Vector3 targetPosition = target.position + rotatedOffset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        
        // Always look at the target
        transform.LookAt(target.position + Vector3.up * 1.5f);
        */
    }
}