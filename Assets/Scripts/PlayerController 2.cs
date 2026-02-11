// using UnityEngine;

// public class PlayerController: MonoBehaviour
// {
//     public GameManager gm;
//     public float walkSpeed = 3f;

//     [Header("Audio")]
//     [Tooltip("Looped run/footstep clip played while the player is walking and unfrozen.")]
//     public AudioClip runLoopClip;
//     [Tooltip("Optional: assign an AudioSource to use for the run loop. If empty, one will be added to the player.")]
//     public AudioSource runLoopSource;
//     [Range(0f, 1f)]
//     public float runLoopVolume = 1f;
    
//     [Header("Fail Conditions")]
//     [Tooltip("If true, triggers Game Over if the player is ungrounded for longer than the delay.")]
//     public bool enableFreefallGameOver = true;
//     [Tooltip("Seconds of continuous falling/ungrounded time before Game Over.")]
//     public float freefallGameOverDelay = 2.5f;
//     [Tooltip("Layers considered 'ground' for freefall detection (Platform + Plank recommended). If left empty, defaults to Platform/Plank layers when available.")]
//     public LayerMask groundMask;
//     public float groundCheckDistance = 0.12f;

//     [Header("Ground (Floor) Fail Condition")]
//     [Tooltip("If true, colliding with Ground triggers immediate Game Over.")]
//     public bool enableGroundHitGameOver = true;
//     [Tooltip("Layers that trigger immediate Game Over on contact (Ground recommended). If empty, defaults to the 'Ground' layer when available.")]
//     public LayerMask groundKillMask;

//     private bool walking = false;
//     private Rigidbody2D rb;
//     private Collider2D col2d;

//     private RigidbodyConstraints2D prevConstraints;
//     private float freefallTimer = 0f;
//     private bool isFrozen = false;

//     Animator anim;

//     void Start() {
//         anim = GetComponent<Animator>();
//     }

//     void Update()
//     {
//         if (gm == null) return;

//         bool shouldRun = !isFrozen && walking && gm.state == GameManager.State.Walking;

//         if (anim != null)
//             anim.SetBool("isRunning", shouldRun);

//         UpdateRunLoopAudio(shouldRun);
//     }


//     void Awake()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         col2d = GetComponent<Collider2D>();

//         if (rb != null)
//         {
//             rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
//             rb.interpolation = RigidbodyInterpolation2D.Interpolate;
//             rb.freezeRotation = true;
//         }

//         // If the mask wasn't set in the inspector, try to build a reasonable default.
//         if (groundMask.value == 0)
//         {
//             int mask = 0;
//             int platformLayer = LayerMask.NameToLayer("Platform");
//             int plankLayer = LayerMask.NameToLayer("Plank");
//             if (platformLayer >= 0) mask |= 1 << platformLayer;
//             if (plankLayer >= 0) mask |= 1 << plankLayer;
//             groundMask = (mask != 0) ? mask : Physics2D.DefaultRaycastLayers;
//         }

//         // Default kill mask to the Ground layer if present.
//         if (groundKillMask.value == 0)
//         {
//             int groundLayer = LayerMask.NameToLayer("Ground");
//             if (groundLayer >= 0)
//                 groundKillMask = 1 << groundLayer;
//         }

//         EnsureRunLoopSource();
//     }

//     public void BeginWalk()
//     {
//         walking = true;
//     }

//     public void StopWalking()
//     {
//         walking = false;
//         if (rb != null)
//         {
//             Vector2 v = rb.linearVelocity;
//             v.x = 0f;
//             rb.linearVelocity = v;
//         }
//     }

//     // Seats player on top surface WITHOUT changing X (prevents "jumpy" reposition)
//     public void SnapToPlatformTopOnly(Transform platform)
//     {
//         if (platform == null || col2d == null || rb == null) return;

//         Collider2D platCol = platform.GetComponent<Collider2D>();
//         if (platCol == null) return;

//         Bounds pb = platCol.bounds;
//         Bounds mb = col2d.bounds;

//         float newY = pb.max.y + mb.extents.y;
//         transform.position = new Vector3(transform.position.x, newY, transform.position.z);

//         rb.linearVelocity = Vector2.zero;
//         rb.angularVelocity = 0f;
//     }

//     public void FreezeInPlace()
//     {
//         walking = false;
//         isFrozen = true;

//         if(anim != null)
//         {
//             anim.SetBool("isRunning", false);
//         }
//         StopRunLoopAudio();

//         if (rb == null) return;

//         prevConstraints = rb.constraints;

//         rb.linearVelocity = Vector2.zero;
//         rb.angularVelocity = 0f;

//         rb.constraints = RigidbodyConstraints2D.FreezePositionX |
//                          RigidbodyConstraints2D.FreezePositionY |
//                          RigidbodyConstraints2D.FreezeRotation;

//         rb.Sleep();
//     }

//     public void Unfreeze()
//     {
//         isFrozen = false;

//         if (rb == null) return;

//         rb.constraints = (prevConstraints == 0) ? RigidbodyConstraints2D.FreezeRotation : prevConstraints;
//         rb.WakeUp();

//         if (anim != null)
//         {
//             anim.SetBool("isRunning", walking && gm != null && gm.state == GameManager.State.Walking);
//         }

//         // If we got unfrozen into a walking state, the Update loop will start the sound;
//         // but calling once here makes it feel instant.
//         if (gm != null)
//         {
//             bool shouldRun = walking && gm.state == GameManager.State.Walking;
//             UpdateRunLoopAudio(shouldRun);
//         }
//     }

//     void EnsureRunLoopSource()
//     {
//         if (runLoopSource == null)
//         {
//             runLoopSource = GetComponent<AudioSource>();
//             if (runLoopSource == null)
//                 runLoopSource = gameObject.AddComponent<AudioSource>();
//         }

//         runLoopSource.playOnAwake = false;
//         runLoopSource.loop = true;
//         runLoopSource.volume = runLoopVolume;
//         // 2D sound (no spatial falloff) is typical for this style of game.
//         runLoopSource.spatialBlend = 0f;
//     }

//     void UpdateRunLoopAudio(bool shouldRun)
//     {
//         if (runLoopClip == null) { StopRunLoopAudio(); return; }

//         EnsureRunLoopSource();

//         // Keep volume in sync with inspector changes.
//         runLoopSource.volume = runLoopVolume;

//         if (shouldRun)
//         {
//             if (runLoopSource.clip != runLoopClip)
//                 runLoopSource.clip = runLoopClip;

//             if (!runLoopSource.isPlaying)
//                 runLoopSource.Play();
//         }
//         else
//         {
//             StopRunLoopAudio();
//         }
//     }

//     void StopRunLoopAudio()
//     {
//         if (runLoopSource != null && runLoopSource.isPlaying)
//             runLoopSource.Stop();
//     }

//     void OnDisable()
//     {
//         StopRunLoopAudio();
//     }

//     void FixedUpdate()
//     {
//         if (rb == null || gm == null) return;

//         // Remove the 'GameOver' check here so the player keeps walking during the fall
//         if (gm.state == GameManager.State.Win)
//             return;

//         // Freefall -> GameOver (covers cases where plank-check fails too early, or player slips off).
//         if (enableFreefallGameOver)
//         {
//             if (gm.state != GameManager.State.Walking || !walking)
//             {
//                 freefallTimer = 0f;
//             }
//             else
//             {
//                 bool grounded = IsGrounded();
//                 if (grounded)
//                 {
//                     freefallTimer = 0f;
//                 }
//                 else
//                 {
//                     // Count time while ungrounded. We don't require high downward speed because
//                     // stepping off an edge starts with small/zero Y velocity.
//                     freefallTimer += Time.fixedDeltaTime;
//                     if (freefallTimer >= Mathf.Max(0.1f, freefallGameOverDelay))
//                     {
//                         gm.GameOver();
//                     }
//                 }
//             }
//         }

//         // If the plank failed, only trigger GameOver once we actually fall far enough.
//         if (gm.IsFailurePending() && transform.position.y < gm.GetFailureYThreshold())
//         {
//             gm.GameOver();
//         }

//         // Keep walking if the state is Walking OR if we just hit GameOver
//         if ((gm.state == GameManager.State.Walking || gm.state == GameManager.State.GameOver) && walking)
//         {
//             Vector2 v = rb.linearVelocity;
//             v.x = walkSpeed;
//             rb.linearVelocity = v;
//         }
//         else
//         {
//             Vector2 v = rb.linearVelocity;
//             v.x = 0f;
//             rb.linearVelocity = v;
//         }
//     }

//     void OnCollisionEnter2D(Collision2D collision)
//     {
//         if (!enableGroundHitGameOver || gm == null) return;
//         if (collision == null || collision.gameObject == null) return;

//         int otherLayer = collision.gameObject.layer;
//         if (((1 << otherLayer) & groundKillMask.value) != 0)
//         {
//             StopWalking();
//             gm.GameOverImmediate();
//         }
//     }

//     void OnTriggerEnter2D(Collider2D other)
//     {
//         if (!enableGroundHitGameOver || gm == null) return;
//         if (other == null) return;

//         int otherLayer = other.gameObject.layer;
//         if (((1 << otherLayer) & groundKillMask.value) != 0)
//         {
//             StopWalking();
//             gm.GameOverImmediate();
//         }
//     }
    
//     // Only used at game start / restart
//     public void ResetToPlatform(Transform platform)
//     {
//         if (platform == null || col2d == null || rb == null) return;

//         walking = false;
//         freefallTimer = 0f;

//         Collider2D platCol = platform.GetComponent<Collider2D>();
//         if (platCol == null) return;

//         Bounds pb = platCol.bounds;
//         Bounds mb = col2d.bounds;

//         float radiusX = mb.extents.x;
//         float radiusY = mb.extents.y;

//         float margin = 0.25f;
//         float x = pb.min.x + radiusX + margin;
//         float y = pb.max.y + radiusY;

//         float maxX = pb.max.x - radiusX - margin;
//         x = Mathf.Min(x, maxX);

//         transform.position = new Vector3(x, y, 0f);

//         rb.linearVelocity = Vector2.zero;
//         rb.angularVelocity = 0f;
//         rb.Sleep();
//     }

//     bool IsGrounded()
//     {
//         if (col2d == null) return false;

//         Bounds b = col2d.bounds;

//         // 3-ray check: left/center/right from just above the bottom of the collider.
//         float insetX = Mathf.Min(0.05f, b.extents.x * 0.5f);
//         Vector2 originC = new Vector2(b.center.x, b.min.y + 0.02f);
//         Vector2 originL = new Vector2(b.center.x - b.extents.x + insetX, originC.y);
//         Vector2 originR = new Vector2(b.center.x + b.extents.x - insetX, originC.y);

//         return HitsGround(originC) || HitsGround(originL) || HitsGround(originR);
//     }

//     bool HitsGround(Vector2 origin)
//     {
//         RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, Mathf.Max(0.01f, groundCheckDistance), groundMask);
//         return hit.collider != null && hit.collider != col2d;
//     }
// }

using System.Collections.Generic;
using System.Text;
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

    [Header("Debug - Jam / Contacts")]
    public bool debugEnabled = true;

    [Tooltip("Draw ground rays + contact normals/points in Scene view.")]
    public bool debugDraw = true;

    [Tooltip("Log when we detect a jam (player wants to move but isn't).")]
    public bool debugJamLogs = true;

    [Tooltip("Log ground ray hits occasionally (spammy).")]
    public bool debugGroundLogs = false;

    [Tooltip("How slow vx must be to consider jammed while trying to walk.")]
    public float jamVelocityThreshold = 0.05f;

    [Tooltip("How long the jam condition must persist before logging.")]
    public float jamMinDuration = 0.12f;

    [Tooltip("Minimum seconds between jam logs.")]
    public float jamLogCooldown = 0.35f;

    [Tooltip("If true, logs every collision enter/stay/exit with details.")]
    public bool debugCollisionLogs = false;

    // Internal
    private bool walking = false;
    private Rigidbody2D rb;
    private Collider2D col2d;

    private RigidbodyConstraints2D prevConstraints;
    private float freefallTimer = 0f;
    private bool isFrozen = false;

    private Animator anim;

    // Jam tracking
    private float jamTimer = 0f;
    private float nextJamLogTime = 0f;

    // Track current overlaps/touches
    private readonly HashSet<Collider2D> touching = new HashSet<Collider2D>();

    // Store last collision contacts for gizmos
    private struct ContactViz
    {
        public Vector2 point;
        public Vector2 normal;
        public float time;
    }
    private readonly List<ContactViz> lastContacts = new List<ContactViz>(32);
    private const float ContactVizLifetime = 0.5f;

    void Start()
    {
        anim = GetComponent<Animator>();
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

    void Update()
    {
        if (gm == null) return;

        bool shouldRun = !isFrozen && walking && gm.state == GameManager.State.Walking;

        if (anim != null)
            anim.SetBool("isRunning", shouldRun);

        UpdateRunLoopAudio(shouldRun);
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

        if (anim != null)
            anim.SetBool("isRunning", false);

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
            anim.SetBool("isRunning", walking && gm != null && gm.state == GameManager.State.Walking);

        if (gm != null)
        {
            bool shouldRun = walking && gm.state == GameManager.State.Walking;
            UpdateRunLoopAudio(shouldRun);
        }
    }

    void FixedUpdate()
    {
        if (rb == null || gm == null) return;

        if (gm.state == GameManager.State.Win)
            return;

        // Freefall -> GameOver
        if (enableFreefallGameOver)
        {
            if (gm.state != GameManager.State.Walking || !walking)
            {
                freefallTimer = 0f;
            }
            else
            {
                bool grounded = IsGrounded(out _);
                if (grounded) freefallTimer = 0f;
                else
                {
                    freefallTimer += Time.fixedDeltaTime;
                    if (freefallTimer >= Mathf.Max(0.1f, freefallGameOverDelay))
                        gm.GameOver();
                }
            }
        }

        if (gm.IsFailurePending() && transform.position.y < gm.GetFailureYThreshold())
            gm.GameOver();

        bool shouldWalk = (gm.state == GameManager.State.Walking || gm.state == GameManager.State.GameOver) && walking;

        // Apply walking velocity
        if (shouldWalk)
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

        // Jam debugging (after velocity is applied)
        if (debugEnabled)
            UpdateJamDebug(shouldWalk);
    }

    // Debug jam & contacts
    void UpdateJamDebug(bool shouldWalk)
    {
        if (!debugJamLogs) return;

        if (!shouldWalk || isFrozen || walkSpeed <= 0.001f)
        {
            jamTimer = 0f;
            return;
        }

        float vx = rb.linearVelocity.x;

        bool jam = vx < jamVelocityThreshold;

        if (jam)
        {
            jamTimer += Time.fixedDeltaTime;

            if (jamTimer >= jamMinDuration && Time.time >= nextJamLogTime)
            {
                nextJamLogTime = Time.time + jamLogCooldown;

                var sb = new StringBuilder();
                sb.Append($"JAM vx={vx:F3} expected={walkSpeed:F2} y={transform.position.y:F2} ");

                int count = 0;
                foreach (var c in touching)
                {
                    if (c == null) continue;
                    if (count == 0) sb.Append("touching=[");
                    if (count > 0) sb.Append(", ");
                    sb.Append($"{c.name}(L:{LayerMask.LayerToName(c.gameObject.layer)} T:{c.tag})");
                    count++;
                    if (count >= 6) { sb.Append("..."); break; }
                }
                if (count > 0) sb.Append("]");
                else sb.Append("touching=[none]");

                // log grounded ray info
                bool grounded = IsGrounded(out RaycastHit2D gHit);
                if (grounded && gHit.collider != null)
                    sb.Append($" groundedOn={gHit.collider.name}(L:{LayerMask.LayerToName(gHit.collider.gameObject.layer)}) dist={gHit.distance:F3}");
                else
                    sb.Append(" grounded=false");

                Debug.Log(sb.ToString(), this);
            }
        }
        else
        {
            jamTimer = 0f;
        }
    }

    // Collisions / triggers tracking
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.collider != null)
            touching.Add(collision.collider);

        if (debugEnabled && debugCollisionLogs && collision != null && collision.collider != null)
        {
            Debug.Log($"CollisionEnter with {collision.collider.name} (L:{LayerMask.LayerToName(collision.collider.gameObject.layer)} T:{collision.collider.tag})", this);
        }

        CaptureContacts(collision);

        // Ground kill
        if (!enableGroundHitGameOver || gm == null) return;
        if (collision == null || collision.gameObject == null) return;

        int otherLayer = collision.gameObject.layer;
        if (((1 << otherLayer) & groundKillMask.value) != 0)
        {
            StopWalking();
            gm.GameOverImmediate();
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision != null && collision.collider != null)
            touching.Add(collision.collider);

        if (debugEnabled && debugCollisionLogs && collision != null && collision.collider != null)
        {
            Debug.Log($"CollisionStay with {collision.collider.name}", this);
        }

        CaptureContacts(collision);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision != null && collision.collider != null)
            touching.Remove(collision.collider);

        if (debugEnabled && debugCollisionLogs && collision != null && collision.collider != null)
        {
            Debug.Log($"CollisionExit with {collision.collider.name}", this);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null)
            touching.Add(other);

        if (debugEnabled && debugCollisionLogs && other != null)
        {
            Debug.Log($"TriggerEnter with {other.name} (L:{LayerMask.LayerToName(other.gameObject.layer)} T:{other.tag})", this);
        }

        // Ground kill
        if (!enableGroundHitGameOver || gm == null) return;
        if (other == null) return;

        int otherLayer = other.gameObject.layer;
        if (((1 << otherLayer) & groundKillMask.value) != 0)
        {
            StopWalking();
            gm.GameOverImmediate();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other != null)
            touching.Remove(other);

        if (debugEnabled && debugCollisionLogs && other != null)
        {
            Debug.Log($"TriggerExit with {other.name}", this);
        }
    }

    void CaptureContacts(Collision2D collision)
    {
        if (!debugEnabled || !debugDraw) return;
        if (collision == null) return;

        // store contact points/normals for gizmo drawing
        int n = collision.contactCount;
        for (int i = 0; i < n; i++)
        {
            var c = collision.GetContact(i);
            lastContacts.Add(new ContactViz
            {
                point = c.point,
                normal = c.normal,
                time = Time.time
            });
        }

        //trim old
        float cutoff = Time.time - ContactVizLifetime;
        for (int i = lastContacts.Count - 1; i >= 0; i--)
        {
            if (lastContacts[i].time < cutoff)
                lastContacts.RemoveAt(i);
        }
    }

    bool IsGrounded(out RaycastHit2D bestHit)
    {
        bestHit = default;
        if (col2d == null) return false;

        Bounds b = col2d.bounds;

        float insetX = Mathf.Min(0.05f, b.extents.x * 0.5f);
        Vector2 originC = new Vector2(b.center.x, b.min.y + 0.02f);
        Vector2 originL = new Vector2(b.center.x - b.extents.x + insetX, originC.y);
        Vector2 originR = new Vector2(b.center.x + b.extents.x - insetX, originC.y);

        bool hitC = HitsGround(originC, out var hc);
        bool hitL = HitsGround(originL, out var hl);
        bool hitR = HitsGround(originR, out var hr);

        // choose nearest
        bool any = false;
        float best = float.MaxValue;

        if (hitC && hc.distance < best) { best = hc.distance; bestHit = hc; any = true; }
        if (hitL && hl.distance < best) { best = hl.distance; bestHit = hl; any = true; }
        if (hitR && hr.distance < best) { best = hr.distance; bestHit = hr; any = true; }

        if (debugEnabled && debugDraw)
        {
            DrawGroundRay(originC, hc);
            DrawGroundRay(originL, hl);
            DrawGroundRay(originR, hr);
        }

        if (debugEnabled && debugGroundLogs && any)
        {
            Debug.Log($"Grounded hit {bestHit.collider.name} dist={bestHit.distance:F3} at {bestHit.point}", this);
        }

        return any;
    }

    bool IsGrounded()
    {
        return IsGrounded(out _);
    }

    bool HitsGround(Vector2 origin, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(origin, Vector2.down, Mathf.Max(0.01f, groundCheckDistance), groundMask);
        return hit.collider != null && hit.collider != col2d;
    }

    void DrawGroundRay(Vector2 origin, RaycastHit2D hit)
    {
        if (!debugDraw) return;

        Vector2 end = origin + Vector2.down * Mathf.Max(0.01f, groundCheckDistance);
        Debug.DrawLine(origin, end, Color.yellow);

        if (hit.collider != null)
        {
            Debug.DrawLine(origin, hit.point, Color.green);
            Debug.DrawRay(hit.point, hit.normal * 0.25f, Color.cyan);
        }
    }

    // ---------------------------
    // Audio
    // ---------------------------
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
        runLoopSource.spatialBlend = 0f;
    }

    void UpdateRunLoopAudio(bool shouldRun)
    {
        if (runLoopClip == null) { StopRunLoopAudio(); return; }

        EnsureRunLoopSource();
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

    // Gizmos: show contact points/normals
    void OnDrawGizmos()
    {
        if (!debugEnabled || !debugDraw) return;

        Gizmos.color = Color.magenta;
        for (int i = 0; i < lastContacts.Count; i++)
        {
            var c = lastContacts[i];
            Gizmos.DrawSphere(c.point, 0.03f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(c.point, c.point + c.normal * 0.25f);
            Gizmos.color = Color.magenta;
        }
    }
}
