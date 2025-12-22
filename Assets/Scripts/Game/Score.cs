using TMPro;
using UnityEngine;

public class Score : MonoBehaviour 
{
    //[SerializeField] int maxScore = 100;
    public TextMeshProUGUI scoreText;
    //GameManager gameManager;
    public int currentScore;

    private void Start()
    {
        
        currentScore = 0;
    }

    public void IncreaseScore(int score)
    {
        currentScore += score;
        //Debug.Log($"taking damage {damage}");
        //PublishScoreChange();
    }
    /*
    void PublishScoreChange()
    {
        if (gameScoreChannel != null)
        {
            gameScoreChannel.Invoke(currentScore);
        }
    }*/

    

}
