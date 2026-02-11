using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameManager gm;
    public float walkSpeed = 3f;

    [Header("Audio")]
    [Tooltip("Looped run/footstep clip played while the player is walking and unfrozen.")]
    public AudioClip runLoopClip;
    [Tooltip("Optional: assign an AudioSource to use for the run loop. If empty, one will be added to the player.")]
    public AudioSource runLoopSource;
    [Range(0f, 1f)]
    public float runLoopVolume = 1f;
    
    [Header("Fail Conditions")]
    [Tooltip("If true, triggers Game Over if the player is ungrounded for longer than the delay.")]
    public bool enableFreefallGameOver = true;
    [Tooltip("Seconds of continuous falling/ungrounded time before Game Over.")]
    public float freefallGameOverDelay = 2.5f;
    [Tooltip("Layers considered 'ground' for freefall detection (Platform + Plank recommended). If left empty, defaults to Platform/Plank layers when available.")]
    public LayerMask groundMask;
    public float groundCheckDistance = 0.12f;

    [Header("Ground (Floor) Fail Condition")]
    [Tooltip("If true, colliding with Ground triggers immediate Game Over.")]
    public bool enableGroundHitGameOver = true;
    [Tooltip("Layers that trigger immediate Game Over on contact (Ground recommended). If empty, defaults to the 'Ground' layer when available.")]
    public LayerMask groundKillMask;

    private bool walking = false;
    private Rigidbody2D rb;
    private Collider2D col2d;

    private RigidbodyConstraints2D prevConstraints;
    private float freefallTimer = 0f;
    private bool isFrozen = false;

    Animator anim;

    void Start() {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (gm == null) return;

        bool shouldRun = !isFrozen && walking && gm.state == GameManager.State.Walking;

        if (anim != null)
            anim.SetBool("isRunning", shouldRun);

        UpdateRunLoopAudio(shouldRun);
    }


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

        // If the mask wasn't set in the inspector, try to build a reasonable default.
        if (groundMask.value == 0)
        {
            int mask = 0;
            int platformLayer = LayerMask.NameToLayer("Platform");
            int plankLayer = LayerMask.NameToLayer("Plank");
            if (platformLayer >= 0) mask |= 1 << platformLayer;
            if (plankLayer >= 0) mask |= 1 << plankLayer;
            groundMask = (mask != 0) ? mask : Physics2D.DefaultRaycastLayers;
        }

        // Default kill mask to the Ground layer if present.
        if (groundKillMask.value == 0)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
                groundKillMask = 1 << groundLayer;
        }

        EnsureRunLoopSource();
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
        isFrozen = true;

        if(anim != null)
        {
            anim.SetBool("isRunning", false);
        }
        StopRunLoopAudio();

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
        isFrozen = false;

        if (rb == null) return;

        rb.constraints = (prevConstraints == 0) ? RigidbodyConstraints2D.FreezeRotation : prevConstraints;
        rb.WakeUp();

        if (anim != null)
        {
            anim.SetBool("isRunning", walking && gm != null && gm.state == GameManager.State.Walking);
        }

        // If we got unfrozen into a walking state, the Update loop will start the sound;
        // but calling once here makes it feel instant.
        if (gm != null)
        {
            bool shouldRun = walking && gm.state == GameManager.State.Walking;
            UpdateRunLoopAudio(shouldRun);
        }
    }

    void EnsureRunLoopSource()
    {
        if (runLoopSource == null)
        {
            runLoopSource = GetComponent<AudioSource>();
            if (runLoopSource == null)
                runLoopSource = gameObject.AddComponent<AudioSource>();
        }

        runLoopSource.playOnAwake = false;
        runLoopSource.loop = true;
        runLoopSource.volume = runLoopVolume;
        // 2D sound (no spatial falloff) is typical for this style of game.
        runLoopSource.spatialBlend = 0f;
    }

    void UpdateRunLoopAudio(bool shouldRun)
    {
        if (runLoopClip == null) { StopRunLoopAudio(); return; }

        EnsureRunLoopSource();

        // Keep volume in sync with inspector changes.
        runLoopSource.volume = runLoopVolume;

        if (shouldRun)
        {
            if (runLoopSource.clip != runLoopClip)
                runLoopSource.clip = runLoopClip;

            if (!runLoopSource.isPlaying)
                runLoopSource.Play();
        }
        else
        {
            StopRunLoopAudio();
        }
    }

    void StopRunLoopAudio()
    {
        if (runLoopSource != null && runLoopSource.isPlaying)
            runLoopSource.Stop();
    }

    void OnDisable()
    {
        StopRunLoopAudio();
    }

    void FixedUpdate()
    {
        if (rb == null || gm == null) return;

        // Remove the 'GameOver' check here so the player keeps walking during the fall
        if (gm.state == GameManager.State.Win)
            return;

        // Freefall -> GameOver (covers cases where plank-check fails too early, or player slips off).
        if (enableFreefallGameOver)
        {
            // IMPORTANT: This must run even in Building state.
            // If we only check during Walking, a player can slip/fall while Building (or while the camera
            // isn't following) and never trigger Game Over.
            if (isFrozen || gm.state == GameManager.State.GameOver)
            {
                freefallTimer = 0f;
            }
            else
            {
                bool grounded = IsGrounded();
                if (grounded)
                {
                    freefallTimer = 0f;
                }
                else
                {
                    // Count time while ungrounded. We don't require high downward speed because
                    // stepping off an edge starts with small/zero Y velocity.
                    freefallTimer += Time.fixedDeltaTime;
                    if (freefallTimer >= Mathf.Max(0.1f, freefallGameOverDelay))
                    {
                        gm.GameOver();
                    }
                }
            }
        }

        // If the plank failed, only trigger GameOver once we actually fall far enough.
        if (gm.IsFailurePending() && transform.position.y < gm.GetFailureYThreshold())
        {
            gm.GameOver();
        }

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

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enableGroundHitGameOver || gm == null) return;
        if (collision == null || collision.gameObject == null) return;

        int otherLayer = collision.gameObject.layer;
        if (((1 << otherLayer) & groundKillMask.value) != 0)
        {
            StopWalking();
            gm.GameOverImmediate();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enableGroundHitGameOver || gm == null) return;
        if (other == null) return;

        int otherLayer = other.gameObject.layer;
        if (((1 << otherLayer) & groundKillMask.value) != 0)
        {
            StopWalking();
            gm.GameOverImmediate();
        }
    }
    
    // Only used at game start / restart
    public void ResetToPlatform(Transform platform)
    {
        if (platform == null || col2d == null || rb == null) return;

        walking = false;
        freefallTimer = 0f;

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

    bool IsGrounded()
    {
        if (col2d == null) return false;

        Bounds b = col2d.bounds;

        // 3-ray check: left/center/right from just above the bottom of the collider.
        float insetX = Mathf.Min(0.05f, b.extents.x * 0.5f);
        Vector2 originC = new Vector2(b.center.x, b.min.y + 0.02f);
        Vector2 originL = new Vector2(b.center.x - b.extents.x + insetX, originC.y);
        Vector2 originR = new Vector2(b.center.x + b.extents.x - insetX, originC.y);

        return HitsGround(originC) || HitsGround(originL) || HitsGround(originR);
    }

    bool HitsGround(Vector2 origin)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, Mathf.Max(0.01f, groundCheckDistance), groundMask);
        return hit.collider != null && hit.collider != col2d;
    }
}