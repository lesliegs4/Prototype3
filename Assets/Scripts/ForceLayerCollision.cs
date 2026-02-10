using UnityEngine;

public class ForceLayerCollision : MonoBehaviour
{
    void Awake()
    {
        int plankLayer = LayerMask.NameToLayer("Plank");
        int platformLayer = LayerMask.NameToLayer("Platform");
        int defaultLayer = LayerMask.NameToLayer("Default");
        
        Debug.Log($"Plank layer: {plankLayer}, Platform layer: {platformLayer}, Default layer: {defaultLayer}");
        
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
        
        Physics2D.IgnoreLayerCollision(plankLayer, platformLayer, false);
        Physics2D.IgnoreLayerCollision(plankLayer, defaultLayer, false);
        
        bool plankPlatformIgnored = Physics2D.GetIgnoreLayerCollision(plankLayer, platformLayer);
        bool plankDefaultIgnored = Physics2D.GetIgnoreLayerCollision(plankLayer, defaultLayer);
        
        if (plankPlatformIgnored)
        {
            Debug.LogError("❌ Plank × Platform collision still DISABLED!");
        }
        else
        {
            Debug.Log("✅ Plank × Platform collision is ENABLED!");
        }
        
        if (plankDefaultIgnored)
        {
            Debug.LogError("❌ Plank × Default (Player) collision still DISABLED!");
        }
        else
        {
            Debug.Log("✅ Plank × Default (Player) collision is ENABLED!");
        }
    }
}
