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
            // ADD THIS:
            if (!wasHolding)
            {
                // Just started holding - play draw sound
                if (AudioManager.instance != null)
                AudioManager.instance.PlayPlankGrow();
            }
        
            Vector3 s = plankVisual.localScale;
            s.y += growSpeed * Time.deltaTime;
            plankVisual.localScale = s;
            UpdatePlankVisualPivot();
        }
        else
        {
            // ADD THIS:
            if (wasHolding)
            {
                // Just released - stop draw sound
                if (AudioManager.instance != null)
                AudioManager.instance.StopPlankGrow();
            }
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

        if (AudioManager.instance != null)
            AudioManager.instance.PlayPlankLand();
            
        transform.rotation = Quaternion.Euler(0, 0, -90f);

        float tipX = plankCol.bounds.max.x;
        bool onPlatform = gm.IsPlankTipOnNextPlatform(tipX);

        if (onPlatform)
        {
            gm.state = GameManager.State.Walking;
            gm.player.BeginWalk();
        }
        else
        {
            // Check if it's a "Long Fail" or "Short Fail"
            Collider2D nextCol = gm.nextPlatform.GetComponent<Collider2D>();
            bool overshot = tipX > nextCol.bounds.max.x;

            if (overshot)
            {
                // Trigger the "Walking" state so the player moves toward the next platform
                gm.state = GameManager.State.Walking;
                gm.player.BeginWalk();
                
                // Tell the GameManager to pan and THEN kill the player
                gm.StartCoroutine(gm.PanAndFail());
            }
            else
            {
                // Standard Short Fail: Hinge logic and immediate Game Over
                gm.GameOver();
                gm.player.BeginWalk();
                SetupHingePhysics();
            }
        }
        rotating = false;
    }

    // Helper to keep the main coroutine clean
    void SetupHingePhysics()
    {
        Rigidbody2D pivotRB = gameObject.AddComponent<Rigidbody2D>();
        pivotRB.bodyType = RigidbodyType2D.Static;

        Rigidbody2D visualRB = plankVisual.gameObject.GetComponent<Rigidbody2D>() ?? plankVisual.gameObject.AddComponent<Rigidbody2D>();
        visualRB.bodyType = RigidbodyType2D.Dynamic;
        visualRB.gravityScale = 3f;

        HingeJoint2D hinge = plankVisual.gameObject.AddComponent<HingeJoint2D>();
        hinge.connectedBody = pivotRB;
        hinge.anchor = new Vector2(0, plankCol.offset.y - (plankCol.size.y * 0.5f));
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
        // Fully disable the visual and the collider
        if (plankVisual != null) plankVisual.gameObject.SetActive(false);
        if (plankCol != null) plankCol.enabled = false;

        // Reset internal state
        wasHolding = false;
        rotating = false;
        
        // Optional: Destroy this specific pivot since a new one is spawned anyway
        Destroy(gameObject, 0.1f); 
    }
}
