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
    public float cameraY = 0f;

    [Header("Progress")]
    public int score = 0;
    public int winScore = 10;

    [Header("UI")]
    public UIManager uiManager;

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
        PositionCameraAtPlatform(0);
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
                Debug.Log("✅ Platform " + i + " (" + platform.name + ") - Landing Trigger WIRED");
            }
            else
            {
                Debug.LogError("❌ Platform " + i + " (" + platform.name + ") - NO LandingTrigger found!");
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
        if (state == State.GameOver || state == State.Win) return;

        Debug.Log("🎯 Player landed on platform " + platformIndex);

        score++;

        // REMOVED: No sound effect when landing on regular platforms
        // Only visual feedback (score counter)

        // Check if this is the last platform (win condition)
        if (platformIndex >= allPlatforms.Length - 1)
        {
            state = State.Win;
            Debug.Log("🏆 YOU WIN!");
            
            // Play win sound only when actually winning
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySuccess();
            
            if (uiManager != null) uiManager.ShowWinScreen();
            return;
        }

        player.SnapToPlatformTopOnly(allPlatforms[platformIndex]);
        player.FreezeInPlace();

        if (activePlank != null) activePlank.CleanupAfterSuccess();

        currentPlatformIndex = platformIndex;

        StartCoroutine(PanToNextPlatform());
    }

    IEnumerator PanToNextPlatform()
    {
        state = State.Panning;
        Debug.Log("📷 Camera panning...");

        int nextIndex = currentPlatformIndex + 1;
        if (nextIndex >= allPlatforms.Length)
        {
            Debug.LogError("No next platform!");
            yield break;
        }

        Vector3 startPos = cam.transform.position;
        Transform nextPlatform = allPlatforms[nextIndex];
        float targetX = (allPlatforms[currentPlatformIndex].position.x + nextPlatform.position.x) / 2f + cameraOffsetX;
        Vector3 targetPos = new Vector3(targetX, cameraY, -10f);

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

    void PositionCameraAtPlatform(int platformIndex)
    {
        if (platformIndex >= allPlatforms.Length) return;
        
        float targetX = allPlatforms[platformIndex].position.x + cameraOffsetX;
        cam.transform.position = new Vector3(targetX, cameraY, -10f);
    }

    public Transform GetNextPlatform()
    {
        int nextIndex = currentPlatformIndex + 1;
        if (nextIndex >= allPlatforms.Length) return null;
        return allPlatforms[nextIndex];
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
            StartCoroutine(ShowLoseScreenDelayed());
        }
    }

    IEnumerator ShowLoseScreenDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        uiManager.ShowLoseScreen();
    }
}