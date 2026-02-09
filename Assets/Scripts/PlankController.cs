using System.Collections;
using UnityEngine;

public class PlankController : MonoBehaviour
{
    public GameManager gm;
    public Transform plankVisual;
    public float growSpeed = 3.0f;
    public float rotateSpeed = 180f;

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
            
            // Add/get Rigidbody
            plankRB = plankVisual.GetComponent<Rigidbody2D>();
            if (plankRB == null)
            {
                plankRB = plankVisual.gameObject.AddComponent<Rigidbody2D>();
            }
            
            // Kinematic for controlled rotation
            plankRB.bodyType = RigidbodyType2D.Kinematic;
            plankRB.useFullKinematicContacts = true;
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

        // Smooth rotation
        float currentAngle = 0f;
        float targetAngle = -90f;
        
        while (currentAngle > targetAngle && !hitNextPlatform)
        {
            float delta = rotateSpeed * Time.deltaTime;
            currentAngle = Mathf.Max(currentAngle - delta, targetAngle);
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
            
            yield return null;
        }

        // Lock final rotation
        transform.rotation = Quaternion.Euler(0, 0, -90f);

        if (AudioManager.instance != null)
            AudioManager.instance.PlayPlankLand();

        // Small delay for physics
        yield return new WaitForSeconds(0.1f);

        Debug.Log($"Rotation complete. Hit platform: {hitNextPlatform}");

        if (hitNextPlatform)
        {
            Debug.Log("✅ Plank bridged the gap!");
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

    // Detect collision with platforms
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isRotating) return;

        Debug.Log($"💥 Plank collided with: {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");

        // Check if it's a platform
        if (collision.gameObject.layer == LayerMask.NameToLayer("Platform"))
        {
            Transform nextPlatform = gm.GetNextPlatform();
            
            if (collision.gameObject.transform == nextPlatform)
            {
                Debug.Log("✅ Hit the NEXT platform!");
                hitNextPlatform = true;
            }
            else
            {
                Debug.Log("⚠️ Hit a platform, but not the next one");
            }
        }
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
}
