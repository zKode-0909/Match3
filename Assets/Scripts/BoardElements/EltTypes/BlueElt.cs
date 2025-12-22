using UnityEngine;

[CreateAssetMenu(fileName = "BlueElt", menuName = "EltType/Blue")]
public class BlueElt : EltType
{
    public override void OnLeftClick(int x, int y)
    {
        //Debug.Log($"left clicked Blue at {x},{y}");
    }

    public override void OnRightClick(int x, int y)
    {
       // Debug.Log($"right clicked Blue at {x},{y}");
    }
}