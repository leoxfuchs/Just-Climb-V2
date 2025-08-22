using UnityEngine;

public class PlayerTransparency : MonoBehaviour
{
    public float transparentAlpha = 0.2f; // Very transparent but still visible
    
    private Renderer playerRenderer;
    private Material playerMaterial;
    
    void Start()
    {
        playerRenderer = GetComponent<Renderer>();
        
        if (playerRenderer != null)
        {
            // Create a copy of the material to avoid changing the shared material
            playerMaterial = new Material(playerRenderer.material);
            playerRenderer.material = playerMaterial;
            
            // Set material to transparent rendering mode
            playerMaterial.SetFloat("_Mode", 3);
            playerMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            playerMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            playerMaterial.SetInt("_ZWrite", 0);
            playerMaterial.DisableKeyword("_ALPHATEST_ON");
            playerMaterial.EnableKeyword("_ALPHABLEND_ON");
            playerMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            playerMaterial.renderQueue = 3000;
            
            // Set the transparency
            Color color = playerMaterial.color;
            color.a = transparentAlpha;
            playerMaterial.color = color;
        }
    }
}