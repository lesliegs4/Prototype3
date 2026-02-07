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

    bool walking = false;
    Rigidbody2D rb;
    Collider2D col2d;
    float unsupportedTimer = 0f;
    readonly ContactPoint2D[] contactBuf = new ContactPoint2D[12];
    readonly RaycastHit2D[] castBuf = new RaycastHit2D[8];

    public void BeginWalk()
    {
        walking = true;
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

        // Only drive horizontal movement; let physics handle gravity/falling.
        if (gm.state == GameManager.State.Walking && walking)
        {
            bool supported = HasSupportBelow();
            Vector2 v = rb.linearVelocity;

            if (supported)
            {
                v.x = walkSpeed;
                rb.linearVelocity = v;
                unsupportedTimer = 0f;
            }
            else
            {
                // As soon as we lose support, stop horizontal motion so we drop straight down.
                v.x = 0f;
                rb.linearVelocity = v;

                unsupportedTimer += Time.fixedDeltaTime;
                if (unsupportedTimer >= unsupportedGraceTime)
                {
                    gm.GameOver();
                    walking = false;
                    rb.angularVelocity = 0f;
                }
            }
        }
        else
        {
            // Stop pushing horizontally when not walking (keep vertical velocity).
            Vector2 v = rb.linearVelocity;
            v.x = 0f;
            rb.linearVelocity = v;
            unsupportedTimer = 0f;
        }
    }

    bool HasSupportBelow()
    {
        if (col2d == null) return true; // fail-safe: don't instant-fail if collider missing

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

            // If the normal points upward, the other collider is supporting us from below.
            if (contactBuf[i].normal.y > 0.25f) return true;
        }

        // 2) If not currently in contact, do a short shape-cast down to catch near-contact frames.
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = supportLayers.value != 0,
            layerMask = supportLayers
        };

        float distance = Mathf.Max(0.01f, supportCheckDistance);
        int hitCount = col2d.Cast(Vector2.down, filter, castBuf, distance);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D other = castBuf[i].collider;
            if (other == null) continue;
            if (other.CompareTag("Ground")) continue;
            return true;
        }

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
