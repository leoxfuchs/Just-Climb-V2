using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float climbSpeed = 3f;
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.1f;
    
    private Rigidbody rb;
    private ClimbingSystem climbingSystem;
    private Vector3 movement;
    private Camera playerCamera;
    private bool isGrounded;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        climbingSystem = GetComponent<ClimbingSystem>();
        playerCamera = Camera.main;
    }
    
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Check if grounded
        CheckGrounded();
        
        // Handle jumping - only when grounded
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (climbingSystem.IsClimbing())
            {
                Debug.Log("Can't jump while climbing!");
            }
            else if (isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                Debug.Log($"JUMP! Force: {jumpForce}, Grounded: {isGrounded}");
            }
            else
            {
                Debug.Log("Can't jump - not grounded!");
            }
        }
        
        if (climbingSystem != null && climbingSystem.IsClimbing())
        {
            // Climbing: W moves up, S moves backward, A/D move sideways
            // Always use the current transform rotation for direction
            Vector3 playerForward = transform.forward;
            Vector3 playerRight = transform.right;
            playerForward.y = 0;
            playerRight.y = 0;
            
            if (playerForward.magnitude > 0.01f)
                playerForward.Normalize();
            if (playerRight.magnitude > 0.01f)
                playerRight.Normalize();
            
            // W/S for up/down when positive/negative vertical, plus backward movement for S
            float upwardMovement = Mathf.Max(0, vertical); // Only W contributes to upward
            float backwardMovement = Mathf.Min(0, vertical); // S contributes to backward
            
            movement = new Vector3(0, upwardMovement, 0) * climbSpeed;
            movement += playerForward * backwardMovement * climbSpeed;
            movement += playerRight * horizontal * climbSpeed;
        }
        else
        {
            // Ground movement ALWAYS relative to current player rotation
            // This ensures WASD moves in the direction the player/camera is facing
            Vector3 playerForward = transform.forward;
            Vector3 playerRight = transform.right;
            
            // Remove Y component to keep movement on the ground plane
            playerForward.y = 0;
            playerRight.y = 0;
            
            // Normalize only if vectors have magnitude
            if (playerForward.magnitude > 0.01f)
                playerForward.Normalize();
            if (playerRight.magnitude > 0.01f)
                playerRight.Normalize();
            
            // Calculate movement based on current facing direction
            movement = (playerForward * vertical + playerRight * horizontal) * moveSpeed;
        }
    }
    
    void FixedUpdate()
    {
        if (climbingSystem == null || !climbingSystem.IsClimbing())
        {
            // PREVENT WALKING ON STEEP SLOPES - FORCE CLIMBING
            Vector3 desiredMovement = movement * Time.fixedDeltaTime;
            Vector3 desiredPosition = rb.position + desiredMovement;
            
            // Raycast down from desired position to check slope
            RaycastHit hit;
            if (Physics.Raycast(desiredPosition + Vector3.up * 0.1f, Vector3.down, out hit, 1f))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                
                // If slope is too steep (more than 30 degrees), BLOCK movement entirely
                if (slopeAngle > 30f)
                {
                    movement = Vector3.zero; // CANNOT WALK - MUST CLIMB
                    Debug.Log($"Blocked movement - slope too steep ({slopeAngle:F1}°). Use hands to climb!");
                    return; // Exit early, no movement allowed
                }
            }
            
            rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
        }
        else
        {
            // Let climbing system handle movement when climbing
            Vector3 climbMovement = climbingSystem.GetClimbingMovement(movement);
            rb.MovePosition(rb.position + climbMovement * Time.fixedDeltaTime);
        }
    }
    
    void CheckGrounded()
    {
        // Raycast down from player center to check if grounded
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, out hit, 2f); // Longer raycast
        
        if (isGrounded)
        {
            Debug.Log($"Grounded on: {hit.collider.name}, distance: {hit.distance}");
            // Check if the surface is too steep to walk on
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > 60f) // More lenient for jumping
            {
                // Too steep - player can't walk, must climb
                isGrounded = false;
                Debug.Log($"Surface too steep ({slopeAngle:F1}°) - must climb!");
            }
        }
        else
        {
            Debug.Log("NOT GROUNDED");
        }
    }
    
    public void SetMovementEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            movement = Vector3.zero;
        }
    }
}