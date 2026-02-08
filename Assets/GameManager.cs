using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// /
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Building, Rotating, Walking, Panning, GameOver, Win }
    public State state = State.Building;

    [Header("References")]
    public PlankController plank;
    public PlayerController player;

    [Header("Platforms")]
    public GameObject platformPrefab;
    public Transform currentPlatform;
    public Transform nextPlatform;

    [Header("Camera")]
    public Camera cam;
    public float panDuration = 0.5f;
    public Vector3 cameraOffset = new Vector3(0f, 0f, -10f); // keep z = -10


    [Header("Progress")]
    public int score = 0;
    public int winScore = 10;

    void Start()
    {
        // If you already placed 2 platforms manually, you can skip spawning here.
        // For barebones: assume you placed two platform instances in scene
        // and assigned them in the Inspector.
    }

    IEnumerator PanThenReset()
    {
        state = State.Panning;

        // Target camera position centered on the NEW current platform
        Vector3 startPos = cam.transform.position;

        // You can center camera on platform center OR slightly ahead
        Vector3 targetPos = new Vector3(currentPlatform.position.x, cam.transform.position.y, 0f) + cameraOffset;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, panDuration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        cam.transform.position = targetPos;

        // Now set up next round
        ResetForNextRound();
    }

    public void OnPlayerLandedSuccessfully()
    {
        if (state == State.GameOver || state == State.Win) return;

        score++;

        if (score >= winScore)
        {
            state = State.Win;
            Debug.Log("YOU WIN!");
            return;
        }

        // Stop the player from continuing to walk during transitions
        player.StopWalking();

        // Advance platforms: next becomes current, spawn a new next
        AdvancePlatforms();

        // Pan camera, then reset player/plank for next round
        StartCoroutine(PanThenReset());
    }


    void AdvancePlatforms()
    {
        // Destroy old current platform
        if (currentPlatform != null)
            Destroy(currentPlatform.gameObject);

        // Promote next -> current
        currentPlatform = nextPlatform;

        // Spawn new next platform ahead
        float gap = Random.Range(2.0f, 5.0f);
        float width = Random.Range(2.0f, 4.0f);

        Vector3 newPos = currentPlatform.position;
        newPos.x += gap + (currentPlatform.localScale.x * 0.5f) + (width * 0.5f);

        GameObject p = Instantiate(platformPrefab, newPos, Quaternion.identity);
        p.transform.localScale = new Vector3(width, 1f, 1f);
        nextPlatform = p.transform;

        // Wire landing trigger gm reference
        LandingTrigger lt = p.GetComponentInChildren<LandingTrigger>();
        if (lt != null) lt.gm = this;
    }


    void ResetForNextRound()
    {
        state = State.Building;

        player.ResetToPlatform(currentPlatform);
        plank.ResetAtPlatformEdge(currentPlatform);
    }


    public void GameOver()
    {
        if (state == State.GameOver || state == State.Win) return;
        state = State.GameOver;
        Debug.Log("GAME OVER");
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
