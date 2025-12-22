using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] Score score;
    [SerializeField] Lives lives;
    [SerializeField] Canvas gameBoardCanvas;
    [SerializeField] Canvas mainMenuCanvas;
    [SerializeField] GameBoard gameBoard;
    [SerializeField] GameObject GameOverScreen;
    //public int Score { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        //gameBoardCanvas.gameObject.SetActive(false);
        //gameBoard.gameObject.SetActive(false);
        //mainMenuCanvas.gameObject.SetActive(true);

    }

    public void AddScore(int scoreFromElt)
    {
        score.IncreaseScore(scoreFromElt);
    }

    public void SpendLife() {
        lives.RemoveLife();
    }

    public int GetLives() { 
        return lives.GetLives();
    }

    private void Update()
    {
        score.scoreText.text = score.currentScore.ToString();
        lives.livesText.text = lives.livesLeft.ToString();
        if (lives.livesLeft <= 0) {
            GameOverScreen.SetActive(true);
        }
    }
}
