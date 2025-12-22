using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoadSceneFromButton : MonoBehaviour
{
    public void LoadScene() {
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }
    
 
}
