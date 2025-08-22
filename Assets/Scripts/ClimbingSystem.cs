using UnityEngine;

public class ClimbingSystem : MonoBehaviour
{
    [Header("Climbing Settings")]
    public float pullStrength = 8f;
    public float climbDamping = 5f;
    public float gravityScale = 0.3f;
    
    private HandController handController;
    private Rigidbody rb;
    private bool isClimbing = false;
    
    void Start()
    {
        handController = GetComponent<HandController>();
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody for smoother physics
        rb.mass = 1f;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
    
    void Update()
    {
        UpdateClimbingState();
        
        if (isClimbing)
        {
            handController.ConstrainPlayerToHands();
        }
    }
    
    void FixedUpdate()
    {
        if (isClimbing)
        {
            ApplyClimbingForces();
        }
    }
    
    void UpdateClimbingState()
    {
        bool wasClimbing = isClimbing;
        isClimbing = handController != null && handController.AnyHandGrabbed();
        
        if (isClimbing != wasClimbing)
        {
            if (isClimbing)
            {
                OnStartClimbing();
            }
            else
            {
                OnStopClimbing();
            }
        }
    }
    
    void OnStartClimbing()
    {
        if (rb != null)
        {
            rb.useGravity = false;
            // Don't completely zero velocity, just reduce it
            rb.linearVelocity *= 0.3f;
            rb.linearDamping = climbDamping;
        }
    }
    
    void OnStopClimbing()
    {
        if (rb != null)
        {
            rb.useGravity = true;
            rb.linearDamping = 2f;
        }
    }
    
    void ApplyClimbingForces()
    {
        if (!isClimbing || handController == null || rb == null)
            return;
        
        Vector3 handAnchor = handController.GetGrabbedHandPosition();
        Vector3 targetPos = handAnchor;
        targetPos.y -= 1.2f; // Body hangs below hands
        
        Vector3 toTarget = targetPos - transform.position;
        float distance = toTarget.magnitude;
        
        // Only apply force if we're not too close
        if (distance > 0.2f)
        {
            // Calculate pull force
            Vector3 pullDirection = toTarget.normalized;
            float pullMagnitude = Mathf.Min(distance * pullStrength, pullStrength * 2f);
            Vector3 pullForce = pullDirection * pullMagnitude;
            
            // Add gravity compensation based on how many hands are grabbed
            float gravityCompensation = gravityScale;
            if (handController.IsHandGrabbed(true) && handController.IsHandGrabbed(false))
            {
                // Both hands grabbed - full support
                gravityCompensation = 1.0f;
            }
            pullForce.y += Physics.gravity.y * gravityCompensation;
            
            // Apply force smoothly
            rb.AddForce(pullForce, ForceMode.Force);
            
            // Limit maximum velocity to prevent jankiness
            if (rb.linearVelocity.magnitude > 4f)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * 4f;
            }
        }
        else
        {
            // If very close, just dampen movement
            rb.linearVelocity *= 0.9f;
        }
    }
    
    public void OnHandGrabbed(bool isLeftHand)
    {
        // Optional: Add any special handling when a hand grabs
    }
    
    public void OnHandReleased()
    {
        // Optional: Add any special handling when a hand releases
    }
    
    public Vector3 GetClimbingMovement(Vector3 requestedMovement)
    {
        if (!isClimbing)
        {
            return requestedMovement;
        }
        
        // Reduce movement speed while climbing
        return requestedMovement * 0.5f;
    }
    
    public bool IsClimbing()
    {
        return isClimbing;
    }
}