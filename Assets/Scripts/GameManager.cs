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
    
    [Tooltip("If enabled, rebuilds/sorts the platform list at runtime using the Platform tag.")]
    public bool autoSyncPlatformsFromTag = true;
    public string platformTag = "Platform";
    public bool sortPlatformsLeftToRight = true;

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

    [Header("Debug")]
    [Tooltip("Press the key to jump directly to a platform for quick testing.")]
    public bool enableDebugJump = true;
    public KeyCode debugJumpKey = KeyCode.C;
    [Tooltip("If set, jump will target the platform GameObject by name (scene name), e.g. 'StarterPlatform (5)'.")]
    public string debugJumpPlatformName = "StarterPlatform (5)";
    [Tooltip("When jumping by name, also override the score to this value (set -1 to keep current score).")]
    public int debugJumpScoreOverride = 4;
    [Tooltip("Fallback 0-based platform index if name isn't found.")]
    public int debugJumpPlatformIndex = 5;

    void Start()
    {
        Debug.Log("=== GAME MANAGER START ===");

        SyncAndSortPlatforms();
        
        if (allPlatforms.Length == 0)
        {
            Debug.LogError("❌ NO PLATFORMS ASSIGNED!");
            return;
        }

        Debug.Log("Found " + allPlatforms.Length + " platforms");

        // UI shows "score / winScore". Score increments on each successful landing (start platform doesn't count),
        // so for N platforms the target transitions is N-1.
        winScore = Mathf.Max(1, allPlatforms.Length - 1);

        player.ResetToPlatform(allPlatforms[0]);
        cam.transform.position = GetCameraPositionForCurrentAndNext();
        SpawnPlankAtPlatform(0);
        WireAllLandingTriggers();
        
        Debug.Log("=== SETUP COMPLETE ===");
    }

    void Update()
    {
        if (!enableDebugJump) return;
        if (Input.GetKeyDown(debugJumpKey))
        {
            if (!string.IsNullOrWhiteSpace(debugJumpPlatformName) && JumpToPlatformByName(debugJumpPlatformName, debugJumpScoreOverride))
                return;

            JumpToPlatformIndex(debugJumpPlatformIndex, keepScore: true);
        }
    }

    public void JumpToPlatformIndex(int platformIndex, bool keepScore = false)
    {
        if (allPlatforms == null || allPlatforms.Length == 0) return;
        if (platformIndex < 0 || platformIndex >= allPlatforms.Length) return;
        if (player == null || cam == null) return;

        // Clear any pending failure/lose UI.
        failurePending = false;
        if (loseRoutine != null)
        {
            StopCoroutine(loseRoutine);
            loseRoutine = null;
        }

        state = State.Building;
        currentPlatformIndex = platformIndex;
        if (!keepScore)
        {
            score = platformIndex; // score increments once per landing; index aligns with progress count.
        }

        // Reset player + camera to this segment.
        player.Unfreeze();
        player.ResetToPlatform(allPlatforms[currentPlatformIndex]);
        cam.transform.position = GetCameraPositionForCurrentAndNext();

        // Replace the current plank, if any.
        if (activePlank != null)
        {
            Destroy(activePlank.gameObject);
            activePlank = null;
        }
        SpawnPlankAtPlatform(currentPlatformIndex);

        Debug.Log($"🧪 Debug jump: now at platform {currentPlatformIndex} (score={score}, winScore={winScore}, keepScore={keepScore})");
    }

    public bool JumpToPlatformByName(string platformName, int scoreOverride = -1)
    {
        if (string.IsNullOrWhiteSpace(platformName)) return false;
        if (allPlatforms == null || allPlatforms.Length == 0) return false;

        int idx = -1;
        for (int i = 0; i < allPlatforms.Length; i++)
        {
            if (allPlatforms[i] != null && allPlatforms[i].name == platformName)
            {
                idx = i;
                break;
            }
        }

        if (idx < 0)
        {
            // As a backup, try finding it in the scene and mapping to our array.
            GameObject go = GameObject.Find(platformName);
            if (go != null)
            {
                Transform t = go.transform;
                for (int i = 0; i < allPlatforms.Length; i++)
                {
                    if (allPlatforms[i] == t)
                    {
                        idx = i;
                        break;
                    }
                }
            }
        }

        if (idx < 0) return false;

        JumpToPlatformIndex(idx, keepScore: true);
        if (scoreOverride >= 0) score = scoreOverride;
        return true;
    }

    void SyncAndSortPlatforms()
    {
        if (!autoSyncPlatformsFromTag) return;

        GameObject[] tagged = null;
        try
        {
            tagged = GameObject.FindGameObjectsWithTag(platformTag);
        }
        catch
        {
            // Tag doesn't exist or is invalid; leave list as-is.
            return;
        }

        if (tagged == null || tagged.Length == 0) return;

        // Always sync to match scene platforms so newly-added ones become part of the run without manual array edits.
        allPlatforms = new Transform[tagged.Length];
        for (int i = 0; i < tagged.Length; i++) allPlatforms[i] = tagged[i].transform;

        if (sortPlatformsLeftToRight)
        {
            System.Array.Sort(allPlatforms, (a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;

                int x = a.position.x.CompareTo(b.position.x);
                if (x != 0) return x;
                return a.position.y.CompareTo(b.position.y);
            });
        }
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

        // Win when reaching the final platform in the ordered list.
        if (platformIndex >= allPlatforms.Length - 1)
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