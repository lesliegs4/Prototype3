using System.Collections;
using UnityEngine;

public class PlankController : MonoBehaviour
{
    public GameManager gm;
    public Transform plankVisual;
    public float growSpeed = 3.0f;
    public float rotateSpeed = 180f;

    private bool landedSuccessfully = false;
    private bool wasHolding = false;
    private bool isRotating = false;
    private bool hitNextPlatform = false;

    private BoxCollider2D plankCol;
    private Rigidbody2D plankRB;

    void Awake()
    {
        if (plankVisual != null)
        {
            plankCol = plankVisual.GetComponent<BoxCollider2D>();
            if (plankCol == null) plankCol = plankVisual.GetComponentInChildren<BoxCollider2D>();
            
            if (plankCol != null)
            {
                plankCol.isTrigger = false;
                plankCol.usedByComposite = false;
            }
            
            plankRB = plankVisual.GetComponent<Rigidbody2D>();
            if (plankRB == null)
            {
                plankRB = plankVisual.gameObject.AddComponent<Rigidbody2D>();
            }
            
            plankRB.bodyType = RigidbodyType2D.Kinematic;
            plankRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            plankRB.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void Update()
    {
        if (gm == null || plankVisual == null) return;
        if (gm.state != GameManager.State.Building) return;

        bool holding = Input.GetKey(KeyCode.Space);

        if (holding)
        {
            if (!wasHolding && AudioManager.instance != null)
                AudioManager.instance.PlayPlankGrow();
        
            Vector3 s = plankVisual.localScale;
            s.y += growSpeed * Time.deltaTime;
            plankVisual.localScale = s;
            UpdatePlankVisualPivot();
        }
        else
        {
            if (wasHolding && AudioManager.instance != null)
                AudioManager.instance.StopPlankGrow();
        }

        if (wasHolding && !holding && !isRotating)
        {
            StartCoroutine(RotateAndCheck());
        }

        wasHolding = holding;
    }

    IEnumerator RotateAndCheck()
    {
        isRotating = true;
        hitNextPlatform = false;
        gm.state = GameManager.State.Rotating;

        Debug.Log("🔄 Starting rotation...");

        float currentAngle = 0f;
        float targetAngle = -90f;
        
        int checkCount = 0;
        while (currentAngle > targetAngle)
        {
            float delta = rotateSpeed * Time.deltaTime;
            currentAngle = Mathf.Max(currentAngle - delta, targetAngle);
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
            
            checkCount++;
            if (checkCount % 10 == 0)
            {
                Debug.Log($"Checking at angle {currentAngle:F1}°");
            }
            
            if (CheckIfTouchingNextPlatformTop())
            {
                Debug.Log("🎯 Plank touched the top of next platform during rotation! Stopping here.");
                landedSuccessfully = true;
                hitNextPlatform = true;
                break;
            }
            
            yield return null;
        }

        if (!landedSuccessfully)
        {
            transform.rotation = Quaternion.Euler(0, 0, -90f);
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlayPlankLand();

        yield return new WaitForSeconds(0.1f);

        Debug.Log($"Rotation complete. Landed successfully: {landedSuccessfully}");

        if (landedSuccessfully && hitNextPlatform)
        {
            Debug.Log("✅ Plank bridged the gap!");
            MakePlankSolidForPlayer();
            gm.state = GameManager.State.Walking;
            gm.player.BeginWalk();
        }
        else
        {
            Debug.Log("❌ Plank didn't reach!");
            gm.GameOver();
            gm.player.BeginWalk();
            SetupFallPhysics();
        }

        isRotating = false;
    }

    bool CheckIfTouchingNextPlatformTop()
    {
        if (plankCol == null || plankVisual == null)
        {
            Debug.LogWarning("Plank collider or visual is null");
            return false;
        }

        Transform nextPlatform = gm.GetNextPlatform();
        if (nextPlatform == null)
        {
            Debug.LogWarning("Next platform is null");
            return false;
        }

        Collider2D platformCol = nextPlatform.GetComponent<Collider2D>();
        if (platformCol == null)
        {
            Debug.LogWarning($"Platform {nextPlatform.name} has no collider");
            return false;
        }

        Transform currentPlatform = gm.GetCurrentPlatform();
        Collider2D currentPlatformCol = currentPlatform != null ? currentPlatform.GetComponent<Collider2D>() : null;
        
        Bounds plankBounds = plankCol.bounds;
        Bounds nextPlatformBounds = platformCol.bounds;

        float currentPlatformRightX = currentPlatformCol != null ? currentPlatformCol.bounds.max.x : currentPlatform.position.x;
        
        float plankRightX = plankBounds.max.x;
        
        if (plankRightX <= currentPlatformRightX + 0.1f)
        {
            return false;
        }

        float nextPlatformTopY = nextPlatformBounds.max.y;
        float nextPlatformLeftX = nextPlatformBounds.min.x;
        float nextPlatformRightX = nextPlatformBounds.max.x;

        float plankLeftX = plankBounds.min.x;
        float plankBottomY = plankBounds.min.y;
        float plankTopY = plankBounds.max.y;

        float overlapStart = Mathf.Max(plankLeftX, nextPlatformLeftX);
        float overlapEnd = Mathf.Min(plankRightX, nextPlatformRightX);
        
        bool hasHorizontalOverlap = overlapEnd > overlapStart;
        
        if (!hasHorizontalOverlap)
        {
            return false;
        }

        float currentPlatformY = currentPlatform.position.y;
        bool isDownwardPlatform = nextPlatformTopY < currentPlatformY;
        
        float tolerance = isDownwardPlatform ? 0.5f : 0.3f;
        bool plankCrossesOrTouchesPlatformTop = plankBottomY <= nextPlatformTopY + tolerance && plankTopY >= nextPlatformTopY - tolerance;

        if (hasHorizontalOverlap && plankRightX > currentPlatformRightX + 0.5f)
        {
            float distanceToTarget = Mathf.Abs(plankBottomY - nextPlatformTopY);
            Debug.Log($"Check: plankBottom={plankBottomY:F2}, plankTop={plankTopY:F2}, platformTop={nextPlatformTopY:F2}, dist={distanceToTarget:F2}, tolerance={tolerance:F2}, isDownward={isDownwardPlatform}");
        }

        if (plankCrossesOrTouchesPlatformTop)
        {
            Debug.Log($"🎯 TOUCH DETECTED! Plank bottom: {plankBottomY:F2}, top: {plankTopY:F2}, Platform top: {nextPlatformTopY:F2}, Downward: {isDownwardPlatform}");
        }

        return plankCrossesOrTouchesPlatformTop;
    }

    void MakePlankSolidForPlayer()
    {
        if (plankVisual == null) return;
        
        plankVisual.gameObject.layer = LayerMask.NameToLayer("Plank");
        
        Debug.Log($"🔧 Plank layer set to: {LayerMask.LayerToName(plankVisual.gameObject.layer)}");
        Debug.Log($"🔧 Plank collider isTrigger: {plankCol?.isTrigger}, RB type: {plankRB?.bodyType}");
    }

    void SetupFallPhysics()
    {
        Rigidbody2D pivotRB = gameObject.GetComponent<Rigidbody2D>();
        if (pivotRB == null)
            pivotRB = gameObject.AddComponent<Rigidbody2D>();
        pivotRB.bodyType = RigidbodyType2D.Static;

        plankRB.bodyType = RigidbodyType2D.Dynamic;
        plankRB.gravityScale = 3f;

        HingeJoint2D hinge = plankVisual.gameObject.AddComponent<HingeJoint2D>();
        hinge.connectedBody = pivotRB;
        hinge.anchor = new Vector2(0, plankCol.offset.y - (plankCol.size.y * 0.5f));
        hinge.enableCollision = false;
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

    public void CleanupAfterSuccess()
    {
        if (plankVisual != null) plankVisual.gameObject.SetActive(false);
        if (plankCol != null) plankCol.enabled = false;

        wasHolding = false;
        isRotating = false;
        
        Destroy(gameObject, 0.1f); 
    }

    public void SetPlankLandedSuccessfully()
    {
        landedSuccessfully = true;
        hitNextPlatform = true;
    }

}
