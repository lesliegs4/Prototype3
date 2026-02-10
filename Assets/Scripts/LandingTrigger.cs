using UnityEngine;

public class LandingTrigger : MonoBehaviour
{
    [HideInInspector] public GameManager gm;
    [HideInInspector] public int platformIndex;

    void Start()
    {
        if (gm == null)
        {
            Debug.LogError("❌ LandingTrigger on " + transform.parent.name + " - GM is NULL!");
        }
        else
        {
            Debug.Log("✅ LandingTrigger on " + transform.parent.name + " (index " + platformIndex + ") ready");
        }

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            Debug.LogError("❌ NO BoxCollider2D on " + gameObject.name);
        }
        else if (!col.isTrigger)
        {
            Debug.LogError("❌ BoxCollider2D on " + gameObject.name + " is NOT a trigger!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("🔔 TRIGGER HIT! Object: " + other.name + " | Tag: " + other.tag + " | Platform: " + platformIndex);

        if (gm == null)
        {
            Debug.LogError("❌ GM is NULL!");
            return;
        }

        Debug.Log("📊 State: " + gm.state);

        if (gm.state != GameManager.State.Walking)
        {
            Debug.LogWarning("⚠️ Not in Walking state, ignoring");
            return;
        }

        if (other.CompareTag("Player"))
        {
            Debug.Log("✅✅✅ PLAYER LANDED ON PLATFORM " + platformIndex + " ✅✅✅");
            
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
            }
            
            gm.OnPlayerLandedOnPlatform(platformIndex);
        }
        else
        {
            Debug.LogWarning("⚠️ Wrong tag! Expected 'Player', got '" + other.tag + "'");
        }
    }

    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = col.isTrigger ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
        }
    }
}