using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoadSceneFromButton2 : MonoBehaviour
{
    [SerializeField] InputReader inputReader;
    [SerializeField] GraphicCaster graphicCaster;


    private void Awake()
    {
        inputReader.ClickEvent += LoadScene;
    }
    public void LoadScene() {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    public void OnClick() {
        if (graphicCaster == null) { 
            return;
        }
        var results = graphicCaster.graphicCast(LayerMask.NameToLayer("MainMenuButton"));
        if (results.Count > 0) {
            Debug.Log($"result {results[0]}");
            LoadScene();
        }

        
    }
 
}
