using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameManager gm;
    public float walkSpeed = 3f;

    private bool walking = false;
    private Rigidbody2D rb;
    private Collider2D col2d;

    private RigidbodyConstraints2D prevConstraints;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
        }
    }

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

    // Seats player on top surface WITHOUT changing X (prevents "jumpy" reposition)
    public void SnapToPlatformTopOnly(Transform platform)
    {
        if (platform == null || col2d == null || rb == null) return;

        Collider2D platCol = platform.GetComponent<Collider2D>();
        if (platCol == null) return;

        Bounds pb = platCol.bounds;
        Bounds mb = col2d.bounds;

        float newY = pb.max.y + mb.extents.y;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void FreezeInPlace()
    {
        walking = false;
        if (rb == null) return;

        prevConstraints = rb.constraints;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.constraints = RigidbodyConstraints2D.FreezePositionX |
                         RigidbodyConstraints2D.FreezePositionY |
                         RigidbodyConstraints2D.FreezeRotation;

        rb.Sleep();
    }

    public void Unfreeze()
    {
        if (rb == null) return;

        rb.constraints = (prevConstraints == 0) ? RigidbodyConstraints2D.FreezeRotation : prevConstraints;
        rb.WakeUp();
    }

    void FixedUpdate()
    {
        if (rb == null || gm == null) return;

        // Remove the 'GameOver' check here so the player keeps walking during the fall
        if (gm.state == GameManager.State.Win)
            return;

        // Keep walking if the state is Walking OR if we just hit GameOver
        if ((gm.state == GameManager.State.Walking || gm.state == GameManager.State.GameOver) && walking)
        {
            Vector2 v = rb.linearVelocity;
            v.x = walkSpeed;
            rb.linearVelocity = v;
        }
        else
        {
            Vector2 v = rb.linearVelocity;
            v.x = 0f;
            rb.linearVelocity = v;
        }
    }
    
    // Only used at game start / restart
    public void ResetToPlatform(Transform platform)
    {
        if (platform == null || col2d == null || rb == null) return;

        walking = false;

        Collider2D platCol = platform.GetComponent<Collider2D>();
        if (platCol == null) return;

        Bounds pb = platCol.bounds;
        Bounds mb = col2d.bounds;

        float radiusX = mb.extents.x;
        float radiusY = mb.extents.y;

        float margin = 0.25f;
        float x = pb.min.x + radiusX + margin;
        float y = pb.max.y + radiusY;

        float maxX = pb.max.x - radiusX - margin;
        x = Mathf.Min(x, maxX);

        transform.position = new Vector3(x, y, 0f);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.Sleep();
    }
}