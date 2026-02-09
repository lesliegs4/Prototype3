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
    public Transform[] allPlatforms; // Assign ALL platforms in order in Inspector
    private int currentPlatformIndex = 0;

    [Header("Plank")]
    public GameObject plankPivotPrefab;
    private PlankController activePlank;

    [Header("Camera")]
    public float panDuration = 0.5f;
    public float cameraOffsetX = 2f; // How far ahead to show
    public float cameraY = 0f; // Fixed Y position

    [Header("Progress")]
    public int score = 0;
    public int winScore = 10;

    [Header("UI")]
    public UIManager uiManager;

    void Start()
    {
        if (allPlatforms.Length == 0)
        {
            Debug.LogError("No platforms assigned! Drag platforms into 'All Platforms' array.");
            return;
        }

        // Start at first platform
        player.ResetToPlatform(allPlatforms[0]);
        
        // Position camera to show first platform
        PositionCameraAtPlatform(0);
        
        // Spawn first plank
        SpawnPlankAtPlatform(0);
        
        // Wire all landing triggers
        WireAllLandingTriggers();
    }

    void WireAllLandingTriggers()
    {
        foreach (Transform platform in allPlatforms)
        {
            LandingTrigger lt = platform.GetComponentInChildren<LandingTrigger>();
            if (lt != null)
            {
                lt.gm = this;
                lt.platformIndex = System.Array.IndexOf(allPlatforms, platform);
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
    }

    public bool IsPlankTipOnPlatform(float tipX, int platformIndex)
    {
        if (platformIndex >= allPlatforms.Length) return false;

        Transform platform = allPlatforms[platformIndex];
        Collider2D c = platform.GetComponent<Collider2D>();
        if (c == null) return false;

        return tipX >= c.bounds.min.x && tipX <= c.bounds.max.x;
    }

    public void OnPlayerLandedOnPlatform(int platformIndex)
    {
        if (state == State.GameOver || state == State.Win) return;

        Debug.Log("Player landed on platform " + platformIndex);

        score++;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySuccess();

        // Check if this is the last platform (win condition)
        if (platformIndex >= allPlatforms.Length - 1)
        {
            state = State.Win;
            Debug.Log("YOU WIN!");
            if (uiManager != null) uiManager.ShowWinScreen();
            return;
        }

        // Freeze player where they landed
        player.SnapToPlatformTopOnly(allPlatforms[platformIndex]);
        player.FreezeInPlace();

        // Clean up old plank
        if (activePlank != null) activePlank.CleanupAfterSuccess();

        // Update current platform index
        currentPlatformIndex = platformIndex;

        // Pan camera to show next platform
        StartCoroutine(PanToNextPlatform());
    }

    IEnumerator PanToNextPlatform()
    {
        state = State.Panning;

        int nextIndex = currentPlatformIndex + 1;
        if (nextIndex >= allPlatforms.Length)
        {
            Debug.LogError("No next platform!");
            yield break;
        }

        Vector3 startPos = cam.transform.position;
        
        // Target: Show area between current and next platform
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

        // Spawn new plank at current platform
        SpawnPlankAtPlatform(currentPlatformIndex);

        // Unfreeze player
        player.Unfreeze();

        state = State.Building;
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
        Debug.Log("GAME OVER");

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