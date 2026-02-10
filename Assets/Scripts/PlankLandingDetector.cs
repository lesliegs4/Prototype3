using UnityEngine;

public class PlankLandingDetector : MonoBehaviour
{
    [HideInInspector] public GameManager gm;
    [HideInInspector] public int platformIndex;
    private bool plankHasLanded = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        return;
    }

    public void Reset()
    {
        plankHasLanded = false;
    }
}
