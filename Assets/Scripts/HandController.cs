using UnityEngine;
using System.Collections.Generic;

public class HandController : MonoBehaviour
{
    [Header("Hand Settings")]
    public Transform leftHand;
    public Transform rightHand;
    public float handReach = 3f;
    public float handMoveSpeed = 8f;
    public float grabDistance = 1.5f;  // Increased to 1.5 units for better auto-latching
    
    [Header("Visual Settings")]
    public Color skinColor = new Color(1f, 0.8f, 0.7f);
    public float grabPointIndicatorSize = 0.4f;
    public Color grabPointColor = new Color(1f, 0f, 0f, 0.6f); // RED with more opacity
    
    private bool isDraggingLeft = false;
    private bool isDraggingRight = false;
    private Camera mainCamera;
    
    private bool leftHandGrabbed = false;
    private bool rightHandGrabbed = false;
    private Transform leftHandLedge;
    private Transform rightHandLedge;
    private Vector3 leftHandGrabPoint;
    private Vector3 rightHandGrabPoint;
    
    // For smoother hand movement
    private Vector3 leftHandTargetPos;
    private Vector3 rightHandTargetPos;
    private Vector3 handMovementInput;
    
    // Grab point visualization
    private List<GameObject> grabPointIndicators = new List<GameObject>();
    private List<Vector3> currentGrabPoints = new List<Vector3>();
    
    void Start()
    {
        mainCamera = Camera.main;
        
        if (leftHand == null)
        {
            leftHand = CreateHand("LeftHand", new Vector3(-0.5f, 0.5f, 0.5f));
        }
        if (rightHand == null)
        {
            rightHand = CreateHand("RightHand", new Vector3(0.5f, 0.5f, 0.5f));
        }
        
        // Initialize hand positions relative to player rotation
        leftHandTargetPos = transform.rotation * new Vector3(-0.5f, 0.5f, 0.5f);
        rightHandTargetPos = transform.rotation * new Vector3(0.5f, 0.5f, 0.5f);
        
        ApplySkinColor();
        
        // Create pool of grab point indicators
        for (int i = 0; i < 30; i++)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "GrabPointIndicator_" + i;
            indicator.transform.localScale = Vector3.one * grabPointIndicatorSize;
            
            // Remove collider
            UnityEngine.Object.DestroyImmediate(indicator.GetComponent<Collider>());
            
            // Set RED transparent material
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.name = "GrabPointMaterial_" + i;
                
                // Set RED color
                mat.color = new Color(1f, 0f, 0f, 0.7f); // BRIGHT RED
                
                // Make it transparent and glowing
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Smoothness", 0.8f);
                mat.SetFloat("_Emission", 0.5f);
                mat.SetColor("_EmissionColor", new Color(1f, 0f, 0f, 1f)); // RED emission
                
                // Enable transparency
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                
                renderer.material = mat;
            }
            
            indicator.SetActive(false);
            grabPointIndicators.Add(indicator);
        }
    }
    
    Transform CreateHand(string name, Vector3 localPosition)
    {
        GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hand.name = name;
        hand.transform.parent = transform;
        hand.transform.localPosition = localPosition;
        hand.transform.localScale = Vector3.one * 0.35f;
        
        Collider col = hand.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        return hand.transform;
    }
    
    void ApplySkinColor()
    {
        if (leftHand != null)
        {
            Renderer leftRenderer = leftHand.GetComponent<Renderer>();
            if (leftRenderer != null)
            {
                leftRenderer.material.color = skinColor;
            }
        }
        
        if (rightHand != null)
        {
            Renderer rightRenderer = rightHand.GetComponent<Renderer>();
            if (rightRenderer != null)
            {
                rightRenderer.material.color = skinColor;
            }
        }
    }
    
    void Update()
    {
        HandleMouseInput();
        UpdateHandMovementInput();
        UpdateHandPositions();
        CheckLedgeGrab();
        UpdateGrabPointIndicators();
    }
    
    void HandleMouseInput()
    {
        // Handle left click
        if (Input.GetMouseButtonDown(0))  // On click down
        {
            // Always start dragging left hand when left clicking
            isDraggingLeft = true;
            isDraggingRight = false;
            
            // If hand was grabbed, release it so we can move it
            if (leftHandGrabbed)
            {
                ReleaseHand(true);
                Debug.Log("Released left hand to move it");
            }
        }
        else if (Input.GetMouseButtonUp(0))  // On click release
        {
            // Try to grab when RELEASING the mouse button
            if (isDraggingLeft && !leftHandGrabbed)
            {
                CheckHandLedgeGrab(leftHand, true);
            }
            isDraggingLeft = false;
        }
        
        // Handle right click
        if (Input.GetMouseButtonDown(1))  // On click down
        {
            // Always start dragging right hand when right clicking
            isDraggingRight = true;
            isDraggingLeft = false;
            
            // If hand was grabbed, release it so we can move it
            if (rightHandGrabbed)
            {
                ReleaseHand(false);
                Debug.Log("Released right hand to move it");
            }
        }
        else if (Input.GetMouseButtonUp(1))  // On click release
        {
            // Try to grab when RELEASING the mouse button
            if (isDraggingRight && !rightHandGrabbed)
            {
                CheckHandLedgeGrab(rightHand, false);
            }
            isDraggingRight = false;
        }
    }
    
    void UpdateHandMovementInput()
    {
        if (isDraggingLeft || isDraggingRight)
        {
            // Get mouse movement for hand control
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            // Convert mouse movement to world space
            Vector3 cameraRight = mainCamera.transform.right;
            Vector3 cameraUp = mainCamera.transform.up;
            
            // Scale mouse movement for hand speed
            handMovementInput = (cameraRight * mouseX + cameraUp * mouseY) * handMoveSpeed * 2f;
            
            // If we have a grabbed hand as anchor, allow pulling up with movement
            if (isDraggingLeft && rightHandGrabbed)
            {
                // Right hand is anchor - can pull body up
                if (mouseY > 0.1f)  // Moving up
                {
                    // Pull the player body up toward the grabbed hand
                    Vector3 pullDirection = (rightHand.position - transform.position).normalized;
                    pullDirection.y = Mathf.Abs(pullDirection.y) + 0.5f;  // Emphasize upward movement
                    
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(pullDirection * mouseY * 50f, ForceMode.Force);
                    }
                }
            }
            else if (isDraggingRight && leftHandGrabbed)
            {
                // Left hand is anchor - can pull body up
                if (mouseY > 0.1f)  // Moving up
                {
                    // Pull the player body up toward the grabbed hand
                    Vector3 pullDirection = (leftHand.position - transform.position).normalized;
                    pullDirection.y = Mathf.Abs(pullDirection.y) + 0.5f;  // Emphasize upward movement
                    
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(pullDirection * mouseY * 50f, ForceMode.Force);
                    }
                }
            }
        }
        else
        {
            handMovementInput = Vector3.zero;
        }
    }
    
    void UpdateHandPositions()
    {
        // Update left hand
        if (leftHandGrabbed && leftHandLedge != null)
        {
            leftHand.position = leftHandGrabPoint;
        }
        else if (isDraggingLeft)
        {
            // Always allow hand movement when dragging, even if not grabbed
            leftHandTargetPos += handMovementInput * Time.deltaTime;
            
            // Constrain to reach
            Vector3 offset = leftHandTargetPos;
            if (rightHandGrabbed)
            {
                // Use right hand as anchor
                offset = leftHandTargetPos - (rightHand.position - transform.position);
                if (offset.magnitude > handReach * 2.2f)
                {
                    offset = offset.normalized * handReach * 2.2f;
                    leftHandTargetPos = (rightHand.position - transform.position) + offset;
                }
            }
            else
            {
                // Use body as anchor
                if (offset.magnitude > handReach)
                {
                    offset = offset.normalized * handReach;
                    leftHandTargetPos = offset;
                }
            }
            
            leftHand.position = transform.position + leftHandTargetPos;
        }
        else if (!leftHandGrabbed)
        {
            // Return to rest position relative to player rotation
            Vector3 restPosition = transform.rotation * new Vector3(-0.5f, 0.5f, 0.5f);
            leftHandTargetPos = Vector3.Lerp(leftHandTargetPos, restPosition, Time.deltaTime * 2f);
            leftHand.position = transform.position + leftHandTargetPos;
        }
        
        // Update right hand
        if (rightHandGrabbed && rightHandLedge != null)
        {
            rightHand.position = rightHandGrabPoint;
        }
        else if (isDraggingRight)
        {
            // Always allow hand movement when dragging, even if not grabbed
            rightHandTargetPos += handMovementInput * Time.deltaTime;
            
            // Constrain to reach
            Vector3 offset = rightHandTargetPos;
            if (leftHandGrabbed)
            {
                // Use left hand as anchor
                offset = rightHandTargetPos - (leftHand.position - transform.position);
                if (offset.magnitude > handReach * 2.2f)
                {
                    offset = offset.normalized * handReach * 2.2f;
                    rightHandTargetPos = (leftHand.position - transform.position) + offset;
                }
            }
            else
            {
                // Use body as anchor
                if (offset.magnitude > handReach)
                {
                    offset = offset.normalized * handReach;
                    rightHandTargetPos = offset;
                }
            }
            
            rightHand.position = transform.position + rightHandTargetPos;
        }
        else if (!rightHandGrabbed)
        {
            // Return to rest position relative to player rotation
            Vector3 restPosition = transform.rotation * new Vector3(0.5f, 0.5f, 0.5f);
            rightHandTargetPos = Vector3.Lerp(rightHandTargetPos, restPosition, Time.deltaTime * 2f);
            rightHand.position = transform.position + rightHandTargetPos;
        }
    }
    
    void CheckLedgeGrab()
    {
        // REMOVED - causing massive lag
    }
    
    void CheckHandLedgeGrab(Transform hand, bool isLeftHand)
    {
        // Use the old automatic grab point detection
        CheckForClimbableSurface(hand, isLeftHand);
    }
    
    void CheckForClimbableSurface(Transform hand, bool isLeftHand)
    {
        Vector3 bestGrabPoint = Vector3.zero;
        Transform bestSurface = null;
        float bestDistance = grabDistance;
        
        // Find all grab points using SAME system as indicators
        List<Vector3> grabPoints = FindGrabPoints(hand.position, grabDistance * 2f);
        
        // Find the CLOSEST grab point to hand
        foreach (Vector3 point in grabPoints)
        {
            float distance = Vector3.Distance(hand.position, point);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestGrabPoint = point;
                
                // Find which surface this grab point belongs to
                Collider[] nearColliders = Physics.OverlapSphere(point, 0.3f);
                foreach (Collider col in nearColliders)
                {
                    if (!col.isTrigger && col.transform != transform)
                    {
                        bestSurface = col.transform;
                        break;
                    }
                }
            }
        }
        
        // Grab the closest point if within range
        if (bestSurface != null && bestDistance <= grabDistance)
        {
            GrabLedge(hand, isLeftHand, bestSurface, bestGrabPoint);
            Debug.Log($"GRABBED at {bestGrabPoint}, distance: {bestDistance}");
        }
        else
        {
            Debug.Log($"NO GRAB - best distance: {bestDistance}, grab distance: {grabDistance}");
        }
    }
    
    bool IsClimbableSurface(RaycastHit hit)
    {
        // ONLY allow climbing on actual solid surfaces - not air
        if (hit.collider == null) return false;
        
        // Must hit a solid object (terrain or mesh)
        if (!hit.collider.isTrigger && hit.distance > 0.01f)
        {
            // Calculate surface angle
            float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
            
            // Surface is climbable if it's steep (like a wall or cliff face)
            // More than 45 degrees from horizontal = climbable
            return surfaceAngle > 45f && surfaceAngle < 135f;
        }
        
        return false;
    }
    
    List<Vector3> FindGrabPoints(Vector3 center, float radius)
    {
        List<Vector3> grabPoints = new List<Vector3>();
        
        // Find all colliders in range
        Collider[] colliders = Physics.OverlapSphere(center, radius);
        
        foreach (Collider col in colliders)
        {
            // Skip triggers and the player
            if (col.isTrigger || col.transform == transform) continue;
            
            // 1. Check for ledges (top edges)
            Vector3[] ledgePoints = FindLedgePoints(col, center, radius);
            grabPoints.AddRange(ledgePoints);
            
            // 2. Check for indents (concave features)
            Vector3[] indentPoints = FindIndentPoints(col, center, radius);
            grabPoints.AddRange(indentPoints);
            
            // 3. Check for outdents (convex features)
            Vector3[] outdentPoints = FindOutdentPoints(col, center, radius);
            grabPoints.AddRange(outdentPoints);
            
            // 4. Check for cracks (narrow gaps)
            Vector3[] crackPoints = FindCrackPoints(col, center, radius);
            grabPoints.AddRange(crackPoints);
            
            // 5. Check for corners (where surfaces meet)
            Vector3[] cornerPoints = FindCornerPoints(col, center, radius);
            grabPoints.AddRange(cornerPoints);
        }
        
        return grabPoints;
    }
    
    Vector3[] FindLedgePoints(Collider col, Vector3 searchCenter, float searchRadius)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Cast rays downward around the collider to find top edges
        Bounds bounds = col.bounds;
        float step = 0.2f;
        
        for (float x = bounds.min.x; x <= bounds.max.x; x += step)
        {
            for (float z = bounds.min.z; z <= bounds.max.z; z += step)
            {
                Vector3 testPoint = new Vector3(x, bounds.max.y + 0.1f, z);
                
                if (Vector3.Distance(testPoint, searchCenter) > searchRadius) continue;
                
                RaycastHit hit;
                // Cast down to find top surface
                if (Physics.Raycast(testPoint, Vector3.down, out hit, 0.3f))
                {
                    if (hit.collider == col)
                    {
                        // Check if there's empty space in front (making it a ledge)
                        Vector3 forwardTest = hit.point + Vector3.forward * 0.2f;
                        if (!Physics.Raycast(forwardTest + Vector3.up * 0.1f, Vector3.down, 0.3f))
                        {
                            points.Add(hit.point + Vector3.up * 0.05f);
                        }
                    }
                }
            }
        }
        
        return points.ToArray();
    }
    
    Vector3[] FindIndentPoints(Collider col, Vector3 searchCenter, float searchRadius)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Use bounds center as a fallback for non-convex colliders
        Vector3 testCenter = col.bounds.center;
        
        // Cast rays in multiple directions to detect indentations
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            
            RaycastHit hit1, hit2;
            Vector3 origin = testCenter + direction * 1f;
            
            if (Physics.Raycast(origin, -direction, out hit1, 2f) &&
                Physics.Raycast(origin + Vector3.up * 0.2f, -direction, out hit2, 2f))
            {
                // If the upper ray travels further, we found an indent
                if (hit2.distance > hit1.distance + 0.1f && hit1.collider == col)
                {
                    if (Vector3.Distance(hit1.point, searchCenter) <= searchRadius)
                    {
                        points.Add(hit1.point);
                    }
                }
            }
        }
        
        return points.ToArray();
    }
    
    Vector3[] FindOutdentPoints(Collider col, Vector3 searchCenter, float searchRadius)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Find protruding edges using raycasts
        Bounds bounds = col.bounds;
        
        // Check corners and edges of the bounds
        Vector3[] testPoints = new Vector3[]
        {
            new Vector3(bounds.min.x, bounds.center.y, bounds.center.z),
            new Vector3(bounds.max.x, bounds.center.y, bounds.center.z),
            new Vector3(bounds.center.x, bounds.center.y, bounds.min.z),
            new Vector3(bounds.center.x, bounds.center.y, bounds.max.z),
        };
        
        foreach (Vector3 testPoint in testPoints)
        {
            if (Vector3.Distance(testPoint, searchCenter) <= searchRadius)
            {
                // Use raycast to find the actual surface
                RaycastHit hit;
                Vector3 toCenter = (bounds.center - testPoint).normalized;
                
                if (Physics.Raycast(testPoint - toCenter * 0.5f, toCenter, out hit, 1f))
                {
                    if (hit.collider == col)
                    {
                        // Verify it's actually grabbable by checking surrounding space
                        int emptyCount = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            float angle = i * 90f * Mathf.Deg2Rad;
                            Vector3 checkDir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                            if (!Physics.Raycast(hit.point + checkDir * 0.3f, -checkDir, 0.3f))
                            {
                                emptyCount++;
                            }
                        }
                        
                        if (emptyCount >= 2) // At least 2 sides are free
                        {
                            points.Add(hit.point);
                        }
                    }
                }
            }
        }
        
        return points.ToArray();
    }
    
    Vector3[] FindCrackPoints(Collider col, Vector3 searchCenter, float searchRadius)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Look for narrow gaps between this collider and others using raycasts
        Collider[] nearbyColliders = Physics.OverlapSphere(col.bounds.center, 2f);
        
        foreach (Collider other in nearbyColliders)
        {
            if (other == col || other.isTrigger) continue;
            
            // Use raycasts to find actual gap
            Vector3 dirToOther = (other.bounds.center - col.bounds.center).normalized;
            RaycastHit hit1, hit2;
            
            // Cast from this collider toward the other
            if (Physics.Raycast(col.bounds.center, dirToOther, out hit1, 3f) && hit1.collider == other)
            {
                // Cast back from the hit point
                if (Physics.Raycast(hit1.point, -dirToOther, out hit2, 3f) && hit2.collider == col)
                {
                    float gap = Vector3.Distance(hit1.point, hit2.point);
                    
                    // If gap is small enough to be a crack
                    if (gap > 0.05f && gap < 0.3f)
                    {
                        Vector3 crackPoint = (hit1.point + hit2.point) * 0.5f;
                        if (Vector3.Distance(crackPoint, searchCenter) <= searchRadius)
                        {
                            points.Add(crackPoint);
                        }
                    }
                }
            }
        }
        
        return points.ToArray();
    }
    
    Vector3[] FindCornerPoints(Collider col, Vector3 searchCenter, float searchRadius)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Check the 8 corners of the bounding box
        Bounds bounds = col.bounds;
        Vector3[] corners = new Vector3[]
        {
            new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
        };
        
        foreach (Vector3 corner in corners)
        {
            if (Vector3.Distance(corner, searchCenter) <= searchRadius)
            {
                // Use raycasts to verify this is an actual corner
                int faceCount = 0;
                Vector3[] checkDirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
                Vector3 actualCorner = corner;
                
                foreach (Vector3 dir in checkDirs)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(corner + dir * 0.2f, -dir, out hit, 0.4f))
                    {
                        if (hit.collider == col)
                        {
                            faceCount++;
                            // Use the hit point as a better corner position
                            actualCorner = hit.point;
                        }
                    }
                }
                
                if (faceCount >= 2)
                {
                    points.Add(actualCorner);
                }
            }
        }
        
        return points.ToArray();
    }
    
    void UpdateGrabPointIndicators()
    {
        // Hide all indicators first
        foreach (GameObject indicator in grabPointIndicators)
        {
            indicator.SetActive(false);
        }
        
        // Show indicators when dragging a hand
        if (isDraggingLeft || isDraggingRight)
        {
            Transform activeHand = isDraggingLeft ? leftHand : rightHand;
            
            // Find grab points using the old automatic detection
            List<Vector3> grabPoints = FindGrabPoints(activeHand.position, grabDistance * 2f);
            
            int indicatorIndex = 0;
            foreach (Vector3 point in grabPoints)
            {
                if (indicatorIndex >= grabPointIndicators.Count) break;
                
                float distance = Vector3.Distance(activeHand.position, point);
                if (distance <= grabDistance * 2f)
                {
                    GameObject indicator = grabPointIndicators[indicatorIndex];
                    indicator.transform.position = point;
                    indicator.SetActive(true);
                    
                    // BIG RED circles
                    indicator.transform.localScale = Vector3.one * grabPointIndicatorSize;
                    
                    Renderer renderer = indicator.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.color = new Color(1f, 0f, 0f, 0.8f);
                        renderer.material.SetColor("_EmissionColor", new Color(1f, 0f, 0f, 1f));
                    }
                    
                    indicatorIndex++;
                }
            }
        }
    }
    
    List<Vector3> FindAllGrabPointsNearHand(Vector3 handPos, float searchRadius)
    {
        List<Vector3> allGrabPoints = new List<Vector3>();
        
        // Find all colliders in large radius
        Collider[] colliders = Physics.OverlapSphere(handPos, searchRadius);
        
        foreach (Collider col in colliders)
        {
            // Skip triggers and the player
            if (col.isTrigger || col.transform == transform) continue;
            
            // For each surface, find MULTIPLE grab points
            List<Vector3> surfaceGrabPoints = FindGrabPointsOnSurface(col, handPos, searchRadius);
            allGrabPoints.AddRange(surfaceGrabPoints);
        }
        
        return allGrabPoints;
    }
    
    List<Vector3> FindGrabPointsOnSurface(Collider surface, Vector3 handPos, float maxDistance)
    {
        List<Vector3> grabPoints = new List<Vector3>();
        
        Bounds bounds = surface.bounds;
        float step = 0.3f; // Sample points every 0.3 units
        
        // Sample points across the ENTIRE surface bounds
        for (float x = bounds.min.x; x <= bounds.max.x; x += step)
        {
            for (float y = bounds.min.y; y <= bounds.max.y; y += step)
            {
                for (float z = bounds.min.z; z <= bounds.max.z; z += step)
                {
                    Vector3 testPoint = new Vector3(x, y, z);
                    
                    // Skip if too far from hand
                    if (Vector3.Distance(testPoint, handPos) > maxDistance) continue;
                    
                    // Check if this point is ON or NEAR the surface
                    Vector3 closestPoint = surface.ClosestPoint(testPoint);
                    float distToSurface = Vector3.Distance(testPoint, closestPoint);
                    
                    if (distToSurface < 0.2f) // Point is on the surface
                    {
                        // Check if it's a grabbable location
                        if (IsGrabbablePoint(surface, closestPoint))
                        {
                            grabPoints.Add(closestPoint);
                        }
                    }
                }
            }
        }
        
        return grabPoints;
    }
    
    bool IsGrabbablePoint(Collider surface, Vector3 point)
    {
        RaycastHit hit;
        
        // Check for ledges (top surfaces) - ENTIRE top surface is grabbable
        if (Physics.Raycast(point + Vector3.up * 0.1f, Vector3.down, out hit, 0.3f))
        {
            if (hit.collider == surface)
            {
                return true; // ANY point on top surface is grabbable
            }
        }
        
        // Check for side grabs (vertical surfaces) - ENTIRE side is grabbable
        Vector3[] sideDirections = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (Vector3 dir in sideDirections)
        {
            if (Physics.Raycast(point + dir * 0.1f, -dir, out hit, 0.3f))
            {
                if (hit.collider == surface)
                {
                    // Check surface normal - should be mostly vertical for side grab
                    float angle = Vector3.Angle(hit.normal, Vector3.up);
                    if (angle > 45f && angle < 135f) // More lenient side surface
                    {
                        return true;
                    }
                }
            }
        }
        
        // Check for indent/hole grabs - BOTTOM of indents are grabbable
        Vector3[] allDirections = { 
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
            Vector3.up, Vector3.down,
            new Vector3(1,1,0).normalized, new Vector3(-1,1,0).normalized,
            new Vector3(1,-1,0).normalized, new Vector3(-1,-1,0).normalized
        };
        
        int solidSides = 0;
        bool hasBottomSurface = false;
        
        foreach (Vector3 dir in allDirections)
        {
            if (Physics.Raycast(point, dir, out hit, 0.4f))
            {
                if (hit.collider == surface)
                {
                    solidSides++;
                    // Check if this is the bottom of a hole/indent
                    if (dir == Vector3.down || dir.y < -0.5f)
                    {
                        hasBottomSurface = true;
                    }
                }
            }
        }
        
        // If surrounded by walls AND has bottom surface = indent/hole bottom
        if (solidSides >= 3 && hasBottomSurface)
        {
            return true;
        }
        
        // If point is inside a concave area (corner/indent)
        if (solidSides >= 2)
        {
            return true;
        }
        
        return false;
    }
    
    void GrabLedge(Transform hand, bool isLeftHand, Transform ledgeTransform, Vector3 grabPoint)
    {
        if (isLeftHand)
        {
            leftHandGrabbed = true;
            leftHandLedge = ledgeTransform;
            leftHandGrabPoint = grabPoint;
            leftHandTargetPos = grabPoint - transform.position;
            isDraggingLeft = false;
            Debug.Log($"LEFT HAND GRABBED at {grabPoint}");
        }
        else
        {
            rightHandGrabbed = true;
            rightHandLedge = ledgeTransform;
            rightHandGrabPoint = grabPoint;
            rightHandTargetPos = grabPoint - transform.position;
            isDraggingRight = false;
            Debug.Log($"RIGHT HAND GRABBED at {grabPoint}");
        }
        
        ClimbingSystem climbingSystem = GetComponent<ClimbingSystem>();
        if (climbingSystem != null)
        {
            climbingSystem.OnHandGrabbed(isLeftHand);
        }
    }
    
    void ReleaseHand(bool isLeftHand)
    {
        if (isLeftHand)
        {
            leftHandGrabbed = false;
            leftHandLedge = null;
            leftHandTargetPos = leftHand.position - transform.position;
            Debug.Log("LEFT HAND RELEASED");
        }
        else
        {
            rightHandGrabbed = false;
            rightHandLedge = null;
            rightHandTargetPos = rightHand.position - transform.position;
            Debug.Log("RIGHT HAND RELEASED");
        }
        
        ClimbingSystem climbingSystem = GetComponent<ClimbingSystem>();
        if (climbingSystem != null)
        {
            climbingSystem.OnHandReleased();
        }
    }
    
    public bool IsHandGrabbed(bool isLeftHand)
    {
        return isLeftHand ? leftHandGrabbed : rightHandGrabbed;
    }
    
    public Vector3 GetHandPosition(bool isLeftHand)
    {
        return isLeftHand ? leftHand.position : rightHand.position;
    }
    
    public bool AnyHandGrabbed()
    {
        return leftHandGrabbed || rightHandGrabbed;
    }
    
    public Vector3 GetGrabbedHandPosition()
    {
        Vector3 avgPos = Vector3.zero;
        int count = 0;
        
        if (leftHandGrabbed)
        {
            avgPos += leftHand.position;
            count++;
        }
        if (rightHandGrabbed)
        {
            avgPos += rightHand.position;
            count++;
        }
        
        if (count > 0)
            return avgPos / count;
            
        return transform.position;
    }
    
    public void ConstrainPlayerToHands()
    {
        if (!AnyHandGrabbed()) return;
        
        Vector3 handCenter = GetGrabbedHandPosition();
        Vector3 toPlayer = transform.position - handCenter;
        
        // Smoother constraint
        float maxDist = handReach * 0.9f;
        if (toPlayer.magnitude > maxDist)
        {
            Vector3 targetPos = handCenter + toPlayer.normalized * maxDist;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 8f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (leftHandGrabbed)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(leftHand.position, 0.15f);
            Gizmos.DrawLine(transform.position, leftHand.position);
        }
        
        if (rightHandGrabbed)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rightHand.position, 0.15f);
            Gizmos.DrawLine(transform.position, rightHand.position);
        }
        
        if (isDraggingLeft && !leftHandGrabbed)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(leftHand.position, grabDistance);
        }
        
        if (isDraggingRight && !rightHandGrabbed)
        {
            Gizmos.color = new Color(0, 0, 1, 0.3f);
            Gizmos.DrawWireSphere(rightHand.position, grabDistance);
        }
    }
}