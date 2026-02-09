using UnityEngine;

public class ForceLayerCollision : MonoBehaviour
{
    void Awake() // Changed from Start to Awake
    {
        // Get layer numbers
        int plankLayer = LayerMask.NameToLayer("Plank");
        int platformLayer = LayerMask.NameToLayer("Platform");
        
        Debug.Log($"Plank layer number: {plankLayer}");
        Debug.Log($"Platform layer number: {platformLayer}");
        
        if (plankLayer == -1)
        {
            Debug.LogError("❌ Plank layer doesn't exist! Create it in Tags & Layers.");
            return;
        }
        
        if (platformLayer == -1)
        {
            Debug.LogError("❌ Platform layer doesn't exist! Create it in Tags & Layers.");
            return;
        }
        
        // FORCE collision to be ENABLED
        Physics2D.IgnoreLayerCollision(plankLayer, platformLayer, false);
        
        // Verify
        bool isIgnored = Physics2D.GetIgnoreLayerCollision(plankLayer, platformLayer);
        
        if (isIgnored)
        {
            Debug.LogError("❌ Collision still DISABLED after forcing!");
        }
        else
        {
            Debug.Log("✅✅✅ Plank × Platform collision is NOW ENABLED! ✅✅✅");
        }
    }
}
