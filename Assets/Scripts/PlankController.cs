using System.Collections;
using UnityEngine;

public class PlankController : MonoBehaviour
{
    public GameManager gm;
    public Transform plankVisual;     // child "Plank"
    public float growSpeed = 3.0f;
    public float rotateSpeed = 250f;

    private bool wasHolding = false;
    private bool rotating = false;

    private BoxCollider2D plankCol;

    void Awake()
    {
        if (plankVisual != null)
        {
            plankCol = plankVisual.GetComponent<BoxCollider2D>();
            if (plankCol == null) plankCol = plankVisual.GetComponentInChildren<BoxCollider2D>();
        }
    }

    void Update()
    {
        if (gm == null || plankVisual == null) return;
        if (gm.state != GameManager.State.Building) return;

        bool holding = Input.GetKey(KeyCode.Space);

        if (holding)
        {
            // grow upward (local Y)
            Vector3 s = plankVisual.localScale;
            s.y += growSpeed * Time.deltaTime;
            plankVisual.localScale = s;

            UpdatePlankVisualPivot();
        }

        if (wasHolding && !holding && !rotating)
        {
            StartCoroutine(RotateDownAndResolve());
        }

        wasHolding = holding;
    }

    IEnumerator RotateDownAndResolve()
    {
        rotating = true;
        gm.state = GameManager.State.Rotating;

        // rotate to horizontal
        yield return RotateToZ(-90f);

        // tip check using collider bounds (robust)
        if (plankCol == null) plankCol = plankVisual.GetComponent<BoxCollider2D>();
        float tipX = plankCol.bounds.max.x;

        if (gm.IsPlankTipOnNextPlatform(tipX))
        {
            gm.state = GameManager.State.Walking;
            gm.player.BeginWalk();   // assumes GameManager has player reference
        }
        else
        {
            gm.GameOver();

            // Let player physics fall naturally (stop pushing sideways)
            gm.player.StopWalking();

            // Make plank fall away smoothly
            Rigidbody2D prb = plankVisual.GetComponent<Rigidbody2D>();
            if (prb == null) prb = plankVisual.gameObject.AddComponent<Rigidbody2D>();

            prb.bodyType = RigidbodyType2D.Dynamic;
            prb.gravityScale = 2f;
            prb.angularVelocity = -250f;

            // optional: disable collider after a moment so it doesn't "hold" anything
            yield return new WaitForSeconds(0.05f);
            if (plankCol != null) plankCol.enabled = false;
        }

        rotating = false;
    }

    IEnumerator RotateToZ(float targetZ)
    {
        while (true)
        {
            float z = transform.eulerAngles.z;
            float current = NormalizeAngle(z);
            float next = Mathf.MoveTowardsAngle(current, targetZ, rotateSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, next);

            if (Mathf.Abs(Mathf.DeltaAngle(next, targetZ)) < 0.5f)
                break;

            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, 0, targetZ);
    }

    float NormalizeAngle(float z)
    {
        if (z > 180f) z -= 360f;
        return z;
    }

    void UpdatePlankVisualPivot()
    {
        if (plankCol == null) return;

        float bottomLocalY = plankCol.offset.y - (plankCol.size.y * 0.5f);
        float scaledBottomY = bottomLocalY * plankVisual.localScale.y;

        Vector3 lp = plankVisual.localPosition;
        lp.y = -scaledBottomY;
        plankVisual.localPosition = lp;
    }

    // Place plank at platform RIGHT edge using platform collider bounds (better than localScale)
    public void ResetAtPlatformEdge(Transform platform)
    {
        if (gm == null || plankVisual == null || platform == null) return;

        // remove fail rigidbody if any
        Rigidbody2D prb = plankVisual.GetComponent<Rigidbody2D>();
        if (prb != null) Destroy(prb);

        transform.rotation = Quaternion.identity;

        // reset length
        plankVisual.localScale = new Vector3(plankVisual.localScale.x, 0.1f, 1f);
        UpdatePlankVisualPivot();

        Collider2D pc = platform.GetComponent<Collider2D>();
        float platformRight = pc != null ? pc.bounds.max.x : platform.position.x + platform.localScale.x * 0.5f;
        float platformTop   = pc != null ? pc.bounds.max.y : platform.position.y + platform.localScale.y * 0.5f;

        transform.position = new Vector3(platformRight, platformTop, 0f);

        if (plankCol != null) plankCol.enabled = true;

        wasHolding = false;
        rotating = false;
    }

    public void CleanupAfterSuccess()
    {
        // disable collider so it stops affecting the player
        if (plankCol != null) plankCol.enabled = false;

        // keep it visually small / hidden
        plankVisual.localScale = new Vector3(plankVisual.localScale.x, 0.1f, 1f);
        transform.rotation = Quaternion.identity;

        wasHolding = false;
        rotating = false;
    }
}
