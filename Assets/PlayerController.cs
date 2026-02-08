using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameManager gm;
    public float walkSpeed = 3f;

    [Header("Fall Detection")]
    [Tooltip("Layers considered 'support' (plank + platforms). Do NOT include Ground.")]
    public LayerMask supportLayers;
    [Tooltip("How far below the player's collider to look for support.")]
    public float supportCheckDistance = 0.15f;
    [Tooltip("Small grace time to avoid false 'fall' on seam/contact jitter.")]
    public float unsupportedGraceTime = 0.05f;
    [Tooltip("Only count support if contact happens within this bottom band (world units) of the player's collider.")]
    public float bottomSupportEpsilon = 0.03f;

    bool walking = false;
    Rigidbody2D rb;
    Collider2D col2d;
    float unsupportedTimer = 0f;
    readonly ContactPoint2D[] contactBuf = new ContactPoint2D[12];
    // Note: we intentionally avoid wide casts here to reduce "edge leeway" before falling.

    public void BeginWalk()
    {
        walking = true;
    }

    public void StopWalking()
    {
        walking = false;
        if (rb != null)
        {
            Vector2 v = rb.linearVelocity;
            v.x = 0f;
            rb.linearVelocity = v;
        }
    }

    void Update()
    {
        // Movement is handled in FixedUpdate via Rigidbody2D for stable collisions.
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void FixedUpdate()
    {
        if (rb == null || gm == null) return;

        if (gm.state == GameManager.State.GameOver || gm.state == GameManager.State.Win)
            return;

        Vector2 v = rb.linearVelocity;   // declare ONCE

        if (gm.state == GameManager.State.Walking && walking)
        {
            if (HasSupportBelow())
            {
                v.x = walkSpeed;
                rb.linearVelocity = v;
            }
            else
            {
                gm.GameOver();
                walking = false;

                v.x = 0f;
                rb.linearVelocity = v;
                rb.angularVelocity = 0f;
            }
        }
        else
        {
            v.x = 0f;
            rb.linearVelocity = v;
        }
    }


    bool HasSupportBelow()
    {
        if (col2d == null) return true; // fail-safe: don't instant-fail if collider missing

        Bounds b = col2d.bounds;
        float bottomBandY = b.min.y + Mathf.Max(0.0001f, bottomSupportEpsilon);

        // 1) Prefer true collision contact beneath us (no more "edge ray" leeway).
        int contactCount = col2d.GetContacts(contactBuf);
        for (int i = 0; i < contactCount; i++)
        {
            Collider2D other = contactBuf[i].collider;
            if (other == null) continue;
            if (other.CompareTag("Ground")) continue;

            if (supportLayers.value != 0)
            {
                int otherLayerBit = 1 << other.gameObject.layer;
                if ((supportLayers.value & otherLayerBit) == 0) continue;
            }

            // Only count support when it touches the *bottom band* of the player.
            // This prevents "side overlap" near an edge from keeping us supported too long.
            if (contactBuf[i].normal.y > 0.25f && contactBuf[i].point.y <= bottomBandY)
            {
                return true;
            }
        }

        // 2) If not currently in contact, raycast straight down from the CENTER only.
        // This is intentionally strict so you fall as soon as you're no longer actually supported.
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.001f);
        float distance = Mathf.Max(0.01f, supportCheckDistance);

        int mask = supportLayers.value != 0 ? supportLayers.value : Physics2D.DefaultRaycastLayers;
        

        return false;
    }

    bool RayHitsNonGround(Vector2 start, float distance, int mask)
    {
        // If the first thing below is the Ground, we *do not* count that as support.
        // This makes "gap" failures end quickly even if there's a big ground collider under everything.
        RaycastHit2D hit = Physics2D.Raycast(start, Vector2.down, distance, mask);
        if (hit.collider == null) return false;
        if (hit.collider.CompareTag("Ground")) return false;
        return true;
    }

    public void ResetToPlatform(Transform platform)
    {
        walking = false;
        unsupportedTimer = 0f;

        float left = platform.position.x - (platform.localScale.x * 0.5f);
        float top = platform.position.y + (platform.localScale.y * 0.5f);

        transform.position = new Vector3(left + 0.6f, top + 0.5f, 0f);

        // zero velocity
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.CompareTag("Ground"))
        {
            gm.GameOver();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // If you add a trigger zone on platforms, use this.
    }
}
