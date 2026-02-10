using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum State { Building, Rotating, Walking, Panning, GameOver, Win }
    public State state = State.Building;

    [Header("References")]
    public PlayerController player;
    public Camera cam;

    [Header("Level Platforms")]
    public Transform[] allPlatforms;
    private int currentPlatformIndex = 0;

    [Header("Plank")]
    public GameObject plankPivotPrefab;
    private PlankController activePlank;

    [Header("Camera")]
    public float panDuration = 0.5f;
    public float cameraOffsetX = 2f;
    public float cameraOffsetY = 2.5f;

    [Header("Progress")]
    public int score = 0;
    public int winScore = 10;

    [Header("UI")]
    public UIManager uiManager;

    [Header("Failure Handling")]
    [Tooltip("How far below the lower of (current/next) platform tops the player must fall before Game Over triggers.")]
    public float fallOutOfBoundsMargin = 2.5f;

    private bool failurePending = false;
    private float failureYThreshold = float.NegativeInfinity;
    private Coroutine loseRoutine;

    void Start()
    {
        Debug.Log("=== GAME MANAGER START ===");
        
        if (allPlatforms.Length == 0)
        {
            Debug.LogError("❌ NO PLATFORMS ASSIGNED!");
            return;
        }

        Debug.Log("Found " + allPlatforms.Length + " platforms");

        player.ResetToPlatform(allPlatforms[0]);
        cam.transform.position = GetCameraPositionForCurrentAndNext();
        SpawnPlankAtPlatform(0);
        WireAllLandingTriggers();
        
        Debug.Log("=== SETUP COMPLETE ===");
    }

    void WireAllLandingTriggers()
    {
        Debug.Log("--- Wiring Landing Triggers ---");
        
        for (int i = 0; i < allPlatforms.Length; i++)
        {
            Transform platform = allPlatforms[i];
            
            LandingTrigger lt = platform.GetComponentInChildren<LandingTrigger>();
            if (lt != null)
            {
                lt.gm = this;
                lt.platformIndex = i;
                Debug.Log($"✅ Platform {i} - Player Landing Trigger WIRED");
            }
            else
            {
                Debug.LogError($"❌ Platform {i} - NO Player LandingTrigger found!");
            }

            PlankLandingDetector pld = platform.GetComponentInChildren<PlankLandingDetector>();
            if (pld != null)
            {
                pld.gm = this;
                pld.platformIndex = i;
                Debug.Log($"✅ Platform {i} - Plank Landing Detector WIRED");
            }
            else
            {
                Debug.LogWarning($"⚠️ Platform {i} - NO PlankLandingDetector found!");
            }
        }
    }


    void SpawnPlankAtPlatform(int platformIndex)
    {
        if (platformIndex >= allPlatforms.Length) return;

        Transform platform = allPlatforms[platformIndex];
        Collider2D pc = platform.GetComponent<Collider2D>();
        
        float platformRight = pc != null ? pc.bounds.max.x : platform.position.x;
        float platformTop = pc != null ? pc.bounds.max.y : platform.position.y;
        
        Vector3 spawnPos = new Vector3(platformRight, platformTop, 0f);

        GameObject go = Instantiate(plankPivotPrefab, spawnPos, Quaternion.identity);
        activePlank = go.GetComponent<PlankController>();
        activePlank.gm = this;
        activePlank.plankVisual.localScale = new Vector3(activePlank.plankVisual.localScale.x, 0.1f, 1f);
        
        Debug.Log("🪵 Plank spawned at platform " + platformIndex);
    }

    public void OnPlayerLandedOnPlatform(int platformIndex)
    {
        if (state == State.Win) return;

        Debug.Log("🎯 Player landed on platform " + platformIndex);

        // If we previously thought the plank failed, landing overrides that.
        failurePending = false;

        // If a lose screen delay was scheduled, cancel it.
        if (loseRoutine != null)
        {
            StopCoroutine(loseRoutine);
            loseRoutine = null;
        }

        score++;

        // Primary win condition is score-based (matches UI "score / winScore").
        if (score >= winScore)
        {
            state = State.Win;
            Debug.Log("🏆 YOU WIN!");
            
            int lastPlatformIndex = allPlatforms.Length - 1;
            player.SnapToPlatformTopOnly(allPlatforms[lastPlatformIndex]);
            player.StopWalking();
            player.FreezeInPlace();
            
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySuccess();
            
            if (uiManager != null) uiManager.ShowWinScreen();
            return;
        }

        // If we haven't hit the score target but we're at the end of the platform list,
        // there is nowhere to pan/spawn the next plank—treat as a forced win.
        if (platformIndex >= allPlatforms.Length - 1)
        {
            state = State.Win;
            Debug.LogWarning("🏁 Reached final platform before winScore; forcing win because no next platform exists.");
            
            int lastPlatformIndex = allPlatforms.Length - 1;
            player.SnapToPlatformTopOnly(allPlatforms[lastPlatformIndex]);
            player.StopWalking();
            player.FreezeInPlace();

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySuccess();

            if (uiManager != null) uiManager.ShowWinScreen();
            return;
        }

        player.SnapToPlatformTopOnly(allPlatforms[platformIndex]);
        player.FreezeInPlace();

        if (activePlank != null) activePlank.CleanupAfterSuccess();

        currentPlatformIndex = platformIndex;

        StartCoroutine(PanCameraToCurrentAndNext());
    }

    IEnumerator PanCameraToCurrentAndNext()
    {
        state = State.Panning;
        Debug.Log("📷 Camera panning...");

        Vector3 startPos = cam.transform.position;
        Vector3 targetPos = GetCameraPositionForCurrentAndNext();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, panDuration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        cam.transform.position = targetPos;

        SpawnPlankAtPlatform(currentPlatformIndex);
        player.Unfreeze();

        state = State.Building;
        Debug.Log("✅ Ready for next plank");
    }

    Vector3 GetCameraPositionForCurrentAndNext()
    {
        int nextIndex = currentPlatformIndex + 1;
        
        if (nextIndex >= allPlatforms.Length)
        {
            Transform lastPlatform = allPlatforms[currentPlatformIndex];
            float targetX = lastPlatform.position.x + cameraOffsetX;
            float targetY = lastPlatform.position.y + cameraOffsetY;
            return new Vector3(targetX, targetY, -10f);
        }

        Transform currentPlatform = allPlatforms[currentPlatformIndex];
        Transform nextPlatform = allPlatforms[nextIndex];
        
        float midX = (currentPlatform.position.x + nextPlatform.position.x) / 2f + cameraOffsetX;
        float midY = (currentPlatform.position.y + nextPlatform.position.y) / 2f + cameraOffsetY;
        
        return new Vector3(midX, midY, -10f);
    }

    public Transform GetNextPlatform()
    {
        int nextIndex = currentPlatformIndex + 1;
        if (nextIndex >= allPlatforms.Length) return null;
        return allPlatforms[nextIndex];
    }

    public Transform GetCurrentPlatform()
    {
        if (currentPlatformIndex < 0 || currentPlatformIndex >= allPlatforms.Length) return null;
        return allPlatforms[currentPlatformIndex];
    }

    public void OnPlankLandedOnPlatform(int platformIndex)
    {
        if (state != GameManager.State.Rotating) return;
        
        int expectedIndex = currentPlatformIndex + 1;
        if (platformIndex == expectedIndex)
        {
            Debug.Log($"✅ Plank successfully bridged to platform {platformIndex}");
            if (activePlank != null)
            {
                activePlank.SetPlankLandedSuccessfully();
            }
        }
    }


    public void GameOver()
    {
        if (state == State.GameOver || state == State.Win) return;
        state = State.GameOver;
        Debug.Log("💀 GAME OVER");

        if (AudioManager.instance != null)
            AudioManager.instance.PlayFail();

        if (uiManager != null)
        {
            loseRoutine = StartCoroutine(ShowLoseScreenDelayed());
        }
    }

    IEnumerator ShowLoseScreenDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        uiManager.ShowLoseScreen();
    }

    public void BeginFailurePending()
    {
        // Keep the game in Walking so the player can still potentially land.
        // We only trigger actual GameOver if the player falls below a threshold.
        failurePending = true;

        Transform current = GetCurrentPlatform();
        Transform next = GetNextPlatform();

        float currentTopY = current != null ? GetPlatformTopY(current) : player.transform.position.y;
        float nextTopY = next != null ? GetPlatformTopY(next) : currentTopY;

        float baseTop = Mathf.Min(currentTopY, nextTopY);
        failureYThreshold = baseTop - Mathf.Abs(fallOutOfBoundsMargin);

        Debug.Log($"⚠️ Failure pending. Fall threshold Y={failureYThreshold:F2} (currentTop={currentTopY:F2}, nextTop={nextTopY:F2})");
    }

    public bool IsFailurePending()
    {
        return failurePending;
    }

    public float GetFailureYThreshold()
    {
        return failureYThreshold;
    }

    float GetPlatformTopY(Transform platform)
    {
        if (platform == null) return 0f;
        Collider2D c = platform.GetComponent<Collider2D>();
        return c != null ? c.bounds.max.y : platform.position.y;
    }
}