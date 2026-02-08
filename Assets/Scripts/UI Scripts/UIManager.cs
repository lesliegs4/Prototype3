using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject winScreen;
    public GameObject loseScreen;
    public GameObject pauseMenu;
    
    [Header("Score Display")]
    public TextMeshProUGUI scoreText;
    
    [Header("References")]
    public GameManager gm;
    
    void Start()
    {
        // Hide all screens at start
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
        if (pauseMenu != null) pauseMenu.SetActive(false);
    }
    
    void Update()
    {
        // Update score display during gameplay
        if (scoreText != null && gm != null)
        {
            scoreText.text = gm.score + " / " + gm.winScore;
        }
        
        // Press ESC to pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    public void ShowWinScreen()
    {
        if (winScreen != null)
        {
            winScreen.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }
    }
    
    public void ShowLoseScreen()
    {
        if (loseScreen != null)
        {
            loseScreen.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f; // Unpause
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Unpause
        SceneManager.LoadScene("MainMenu");
    }
    
    public void TogglePause()
    {
        if (pauseMenu == null) return;
        
        bool isPaused = pauseMenu.activeSelf;
        
        if (isPaused)
        {
            // Resume
            pauseMenu.SetActive(false);
            Time.timeScale = 1f;
        }
        else
        {
            // Pause
            pauseMenu.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}