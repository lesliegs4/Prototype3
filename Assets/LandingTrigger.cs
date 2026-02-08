using UnityEngine;

public class LandingTrigger : MonoBehaviour
{
    public GameManager gm;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (gm == null) return;
        if (gm.state != GameManager.State.Walking) return;

        if (other.CompareTag("Player"))
        {
            gm.OnPlayerLandedSuccessfully();
        }
    }
}
