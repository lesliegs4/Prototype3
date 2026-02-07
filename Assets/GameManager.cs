using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum State { Building, Rotating, Walking, GameOver, Win }
    public State state = State.Building;

    [Header("References")]
    public PlankController plank;
    public PlayerController player;

    [Header("Platforms")]
    public GameObject platformPrefab;
    public Transform currentPlatform;
    public Transform nextPlatform;

    [Header("Progress")]
    public int score = 0;
    public int winScore = 10;

    void Start()
    {
        // If you already placed 2 platforms manually, you can skip spawning here.
        // For barebones: assume you placed two platform instances in scene
        // and assigned them in the Inspector.
    }

    public void OnPlayerLandedSuccessfully()
    {
        score++;
        if (score >= winScore)
        {
            state = State.Win;
            Debug.Log("YOU WIN!");
            return;
        }

        // Advance: make next become current, spawn a new next
        AdvancePlatforms();
        ResetForNextRound();
    }

    void AdvancePlatforms()
    {
        // Move current out / destroy
        if (currentPlatform != null)
            Destroy(currentPlatform.gameObject);

        currentPlatform = nextPlatform;

        // Spawn new next platform ahead
        float gap = Random.Range(2.0f, 5.0f);
        float width = Random.Range(2.0f, 4.0f);

        Vector3 newPos = currentPlatform.position;
        newPos.x += gap + (currentPlatform.localScale.x * 0.5f) + (width * 0.5f);

        GameObject p = Instantiate(platformPrefab, newPos, Quaternion.identity);
        p.transform.localScale = new Vector3(width, 1f, 1f);
        nextPlatform = p.transform;
    }

    void ResetForNextRound()
    {
        state = State.Building;

        // Move player onto current platform top-left-ish
        player.ResetToPlatform(currentPlatform);

        // Reset plank at current platform edge
        plank.ResetAtPlatformEdge(currentPlatform);
    }

    public void GameOver()
    {
        if (state == State.GameOver || state == State.Win) return;
        state = State.GameOver;
        Debug.Log("GAME OVER");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
