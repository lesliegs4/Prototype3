using UnityEngine;

public class PlankLandingDetector : MonoBehaviour
{
    [HideInInspector] public GameManager gm;
    [HideInInspector] public int platformIndex;
    private bool plankHasLanded = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (plankHasLanded) return;
        if (gm == null) return;
        if (gm.state != GameManager.State.Rotating) return;

        // We only care about the active plank touching this platform's landing zone.
        PlankController plank = other.GetComponentInParent<PlankController>();
        if (plank == null) plank = other.GetComponent<PlankController>();
        if (plank == null) return;

        // Make sure we're responding to *this* game's plank.
        if (plank.gm != gm) return;

        // Prevent false positives: only accept when the plank *tip* is actually on this platform's top.
        Collider2D platformCol = null;
        if (gm.allPlatforms != null && platformIndex >= 0 && platformIndex < gm.allPlatforms.Length)
            platformCol = gm.allPlatforms[platformIndex].GetComponent<Collider2D>();

        if (platformCol == null) return;

        Vector2 tipWorld = plank.GetPlankTipWorld();
        Bounds b = platformCol.bounds;
        float topY = b.max.y;

        Transform currentPlatform = gm.GetCurrentPlatform();
        float currentPlatformY = currentPlatform != null ? currentPlatform.position.y : topY;
        bool isDownwardPlatform = topY < currentPlatformY;

        float xMargin = 0.2f;
        if (tipWorld.x < b.min.x - xMargin || tipWorld.x > b.max.x + xMargin) return;

        Vector2 closest = platformCol.ClosestPoint(tipWorld);
        float dist = Vector2.Distance(tipWorld, closest);

        float maxContactDistance = isDownwardPlatform ? 0.35f : 0.25f;
        float topTolerance = isDownwardPlatform ? 0.6f : 0.35f;
        bool closestIsOnTopSurface = Mathf.Abs(closest.y - topY) <= topTolerance;

        if (!(dist <= maxContactDistance && closestIsOnTopSurface)) return;

        plankHasLanded = true;
        gm.OnPlankLandedOnPlatform(platformIndex);
    }

    public void Reset()
    {
        plankHasLanded = false;
    }
}
