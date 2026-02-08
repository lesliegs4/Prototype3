using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum State { Building, Rotating, Walking, Panning, GameOver, Win }
    public State state = State.Building;

    [Header("References")]
    public PlayerController player;

    [Header("Platforms")]
    public GameObject platformPrefab;
    public Transform currentPlatform;   // assign in Inspector
    public Transform nextPlatform;      // assign in Inspector

    [Header("Plank Prefab")]
    public GameObject plankPivotPrefab; // assign prefab in Inspector
    private PlankController activePlank;

    [Header("Camera")]
    public Camera cam;
    public float panDuration = 0.5f;
    public Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

    [Header("Progress")]
    public int score = 0;
    public int winScore = 10;

    [Header("UI")]
    public UIManager uiManager;

    void Start()
    {
        // Put player on starting platform once
        player.ResetToPlatform(currentPlatform);

        // Spawn the first plank at current platform edge
        SpawnNewPlankAtCurrentPlatform();

        // Make sure landing trigger on the initial next platform knows about us
        WireLandingTrigger(nextPlatform);
    }

    void SpawnNewPlankAtCurrentPlatform()
    {
        // Calculate exact edge position first
        Collider2D pc = currentPlatform.GetComponent<Collider2D>();
        float platformRight = pc != null ? pc.bounds.max.x : currentPlatform.position.x;
        float platformTop = pc != null ? pc.bounds.max.y : currentPlatform.position.y;
        Vector3 spawnPos = new Vector3(platformRight, platformTop, 0f);

        // Instantiate directly at the edge to prevent the "center screen flash"
        GameObject go = Instantiate(plankPivotPrefab, spawnPos, Quaternion.identity);
        activePlank = go.GetComponent<PlankController>();
        activePlank.gm = this;
        
        // Ensure it starts invisible/tiny
        activePlank.plankVisual.localScale = new Vector3(activePlank.plankVisual.localScale.x, 0.1f, 1f);
    }

    void WireLandingTrigger(Transform platform)
    {
        if (platform == null) return;
        LandingTrigger lt = platform.GetComponentInChildren<LandingTrigger>();
        if (lt != null) lt.gm = this;
    }

    // Robust: uses collider bounds, not localScale
    public bool IsPlankTipOnNextPlatform(float tipX)
    {
        if (nextPlatform == null) return false;

        Collider2D c = nextPlatform.GetComponent<Collider2D>();
        if (c == null) return false;

        return tipX >= c.bounds.min.x && tipX <= c.bounds.max.x;
    }

    public void OnPlayerLandedSuccessfully()
    {
        if (state == State.GameOver || state == State.Win) return;

        score++;
        if (score >= winScore)
        {
            state = State.Win;
            Debug.Log("YOU WIN!");
        
            if (uiManager != null) uiManager.ShowWinScreen();
        
            return;
        }

        // IMPORTANT:
        // Seat player on TOP of the platform they landed on (nextPlatform),
        // WITHOUT changing X (so it doesn't look jumpy), then freeze during camera pan.
        player.SnapToPlatformTopOnly(nextPlatform);
        player.FreezeInPlace();

        // Clean up old plank quickly (disable collider/visual) so it doesn't push the ball
        if (activePlank != null) activePlank.CleanupAfterSuccess();

        // Advance platforms: next becomes current, spawn a new next
        AdvancePlatforms();

        // Pan camera to new current, then start next round
        StartCoroutine(PanThenReset());
    }

    void AdvancePlatforms()
    {
        // Promote next -> current
        currentPlatform = nextPlatform;

        // Spawn new next ahead of current (keep 3 on screen by NOT destroying old platforms here)
        float gap = Random.Range(2.0f, 5.0f);
        float width = Random.Range(2.0f, 4.0f);

        // Use collider bounds for accurate placement
        Collider2D curCol = currentPlatform.GetComponent<Collider2D>();
        float curRight = (curCol != null) ? curCol.bounds.max.x : currentPlatform.position.x + currentPlatform.localScale.x * 0.5f;
        float curY = currentPlatform.position.y;

        Vector3 newPos = new Vector3(curRight + gap + width * 0.5f, curY, 0f);

        GameObject p = Instantiate(platformPrefab, newPos, Quaternion.identity);
        p.transform.localScale = new Vector3(width, 1f, 1f);
        nextPlatform = p.transform;

        WireLandingTrigger(nextPlatform);
    }

    IEnumerator PanThenReset()
    {
        state = State.Panning;

        Vector3 startPos = cam.transform.position;
        Vector3 targetPos = new Vector3(currentPlatform.position.x, cam.transform.position.y, 0f) + cameraOffset;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, panDuration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        cam.transform.position = targetPos;

        ResetForNextRound();
    }

    public IEnumerator PanAndFail()
    {
        // Wait for the player to reach the general area of the next platform
        yield return new WaitForSeconds(0.5f);

        Vector3 startPos = cam.transform.position;
        Vector3 targetPos = new Vector3(nextPlatform.position.x, cam.transform.position.y, 0f) + cameraOffset;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / panDuration;
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // Now that we've seen the overshoot, trigger the actual Game Over
        GameOver();
    }

    void ResetForNextRound()
    {
        state = State.Building;

        // Spawn a fresh plank for the new gap
        if (activePlank != null) Destroy(activePlank.gameObject);
        SpawnNewPlankAtCurrentPlatform();

        // Let physics resume
        player.Unfreeze();
    }

    public void GameOver()
    {
        if (state == State.GameOver || state == State.Win) return;
        state = State.GameOver;
        Debug.Log("GAME OVER");
        
        if (uiManager != null)
        {
            StartCoroutine(ShowLoseScreenDelayed());
        }
    }

    System.Collections.IEnumerator ShowLoseScreenDelayed()
    {
        // Wait 1.5 seconds so player can see what happened
        yield return new WaitForSeconds(1.5f);
        uiManager.ShowLoseScreen();
    }
}
