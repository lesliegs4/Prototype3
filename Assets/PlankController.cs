using System.Collections;
using UnityEngine;

public class PlankController : MonoBehaviour
{
    public GameManager gm;
    public Transform plankVisual; // the child "Plank"
    public float growSpeed = 3.0f;
    public float rotateSpeed = 250f;

    bool wasHolding = false;
    BoxCollider2D plankCol;

    void Update()
    {
        if (gm.state == GameManager.State.Building)
        {
            bool holding = Input.GetKey(KeyCode.Space);

            if (holding)
            {
                // grow in Y
                Vector3 s = plankVisual.localScale;
                s.y += growSpeed * Time.deltaTime;
                plankVisual.localScale = s;
                UpdatePlankVisualPivot();
            }

            // detect release (was holding last frame, not holding now)
            if (wasHolding && !holding)
            {
                StartCoroutine(RotateDown());
            }

            wasHolding = holding;
        }
    }

    void Awake()
    {
        if (plankVisual != null)
        {
            plankCol = plankVisual.GetComponent<BoxCollider2D>();
            if (plankCol == null) plankCol = plankVisual.GetComponentInChildren<BoxCollider2D>();
        }
    }

    IEnumerator RotateDown()
    {
        gm.state = GameManager.State.Rotating;

        const float targetZ = -90f;
        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, targetZ)) > 0.1f)
        {
            float currentZ = transform.eulerAngles.z;
            float nextZ = Mathf.MoveTowardsAngle(currentZ, targetZ, rotateSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, nextZ);

            yield return null;
        }

        // snap clean
        transform.rotation = Quaternion.Euler(0, 0, -90f);

        // Ensure the plank lands flush: left edge at platform right, top at platform top
        if (gm != null && gm.currentPlatform != null)
        {
            AlignHorizontalToPlatformEdgeTop(gm.currentPlatform);
        }

        gm.state = GameManager.State.Walking;
        gm.player.BeginWalk();
    }

    public void ResetAtPlatformEdge(Transform platform)
    {
        // Reset rotation
        transform.rotation = Quaternion.identity;

        // Reset plank length
        plankVisual.localScale = new Vector3(plankVisual.localScale.x, 0.1f, 1f);
        UpdatePlankVisualPivot();

        // Place pivot at right edge of platform top
        float platformRight = platform.position.x + (platform.localScale.x * 0.5f);
        float platformTop = platform.position.y + (platform.localScale.y * 0.5f);

        transform.position = new Vector3(platformRight, platformTop, 0f);

        wasHolding = false;
    }

    void UpdatePlankVisualPivot()
    {
        // Keep the "base" of the plank fixed at the parent origin so scaling only grows "up".
        // Works even if the collider has an offset.
        if (plankCol == null) return;

        float bottomLocalY = plankCol.offset.y - (plankCol.size.y * 0.5f);
        float scaledBottomY = bottomLocalY * plankVisual.localScale.y;
        Vector3 lp = plankVisual.localPosition;
        lp.y = -scaledBottomY;
        plankVisual.localPosition = lp;
    }

    void AlignHorizontalToPlatformEdgeTop(Transform platform)
    {
        if (plankCol == null) return;

        float platformRight = platform.position.x + (platform.localScale.x * 0.5f);
        float platformTop = platform.position.y + (platform.localScale.y * 0.5f);

        // After rotation, align using world-space bounds so the ball rolls on the top face.
        Bounds b = plankCol.bounds;
        Vector3 p = transform.position;
        p.x += (platformRight - b.min.x);
        p.y += (platformTop - b.max.y);
        transform.position = p;
    }
}
