using UnityEngine.UI;
using UnityEngine;

public class CanvasScalerOrientationDriver : MonoBehaviour
{
    bool? Landscape;

    const float Baseline = 600;

    void Drive()
    {
        var cs = GetComponent<CanvasScaler>();

        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        float aspect = (float)Screen.width / Screen.height;

        if ((bool)Landscape)
        {
            cs.referenceResolution = new Vector2(aspect * Baseline, Baseline);
            cs.matchWidthOrHeight = 1;
        }
        else
        {
            cs.referenceResolution = new Vector2(Baseline, aspect * Baseline);
            cs.matchWidthOrHeight = 0;
        }
    }

    void Update()
    {
        bool landscape = Screen.width > Screen.height;

        if (Landscape != landscape)
        {
            Landscape = landscape;

            Drive();
        }
    }
}