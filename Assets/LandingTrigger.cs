using UnityEngine;

public class LandingTrigger : MonoBehaviour
{
    public GameManager gm;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (gm.state != GameManager.State.Walking) return;
        if (other.gameObject.name.Contains("Player"))
        {
            Debug.Log("Player landed successfully!");
            gm.OnPlayerLandedSuccessfully();
        }
    }
}
