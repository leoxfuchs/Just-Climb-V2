using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    
    private Camera playerCamera;
    private float verticalRotation = 0f;
    private Transform playerBody;
    private float horizontalRotation = 0f;
    
    void Start()
    {
        playerBody = transform;
        playerCamera = Camera.main;
        
        if (playerCamera == null)
        {
            Debug.LogError("No main camera found!");
            return;
        }
        
        // Make camera a child of player for proper first-person view
        playerCamera.transform.parent = transform;
        playerCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
        playerCamera.transform.localRotation = Quaternion.identity;
        
        // Initialize rotation from current transform
        horizontalRotation = transform.eulerAngles.y;
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        if (playerCamera == null) return;
        
        // Handle ESC to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? 
                CursorLockMode.None : CursorLockMode.Locked;
        }
        
        // Only rotate when cursor is locked
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        // DON'T ROTATE CAMERA IF USING HANDS (left or right mouse button held)
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            return; // Skip camera rotation entirely when moving hands
        }
        
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Accumulate horizontal rotation
        horizontalRotation += mouseX;
        
        // Apply the accumulated horizontal rotation to the player body
        playerBody.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
        
        // Rotate the camera vertically
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}