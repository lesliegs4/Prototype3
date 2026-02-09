using UnityEngine;

public class LandingTrigger : MonoBehaviour
{
    public GameManager gm;
    public int platformIndex; // Which platform in the sequence is this?

    void OnTriggerEnter2D(Collider2D other)
    {
        if (gm == null) return;
        if (gm.state != GameManager.State.Walking) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("✅ Player landed on platform " + platformIndex);
            gm.OnPlayerLandedOnPlatform(platformIndex);
        }
    }
}