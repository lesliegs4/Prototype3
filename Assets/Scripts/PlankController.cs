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

        yield return RotateToZ(-90f);

        // Hard-set rotation to -90 to ensure it is perfectly flush
        transform.rotation = Quaternion.Euler(0, 0, -90f);

        float tipX = plankCol.bounds.max.x;

        if (gm.IsPlankTipOnNextPlatform(tipX))
        {
            gm.state = GameManager.State.Walking;
            gm.player.BeginWalk(); 
        }
        else
        {
            // Fail Scenario:
            gm.GameOver();
            gm.player.BeginWalk(); // Allow player to keep moving into the gap

            // Anchor the pivot and let the visual fall via a Hinge
            Rigidbody2D pivotRB = gameObject.AddComponent<Rigidbody2D>();
            pivotRB.bodyType = RigidbodyType2D.Static;

            Rigidbody2D visualRB = plankVisual.gameObject.GetComponent<Rigidbody2D>();
            if (visualRB == null) visualRB = plankVisual.gameObject.AddComponent<Rigidbody2D>();
            
            visualRB.bodyType = RigidbodyType2D.Dynamic;
            visualRB.gravityScale = 3f;

            HingeJoint2D hinge = plankVisual.gameObject.AddComponent<HingeJoint2D>();
            hinge.connectedBody = pivotRB;
            // Anchor point at the left edge of the plank visual
            hinge.anchor = new Vector2(0, plankCol.offset.y - (plankCol.size.y * 0.5f)); 
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
