using UnityEngine;

[CreateAssetMenu(fileName = "EltType", menuName = "EltType/Yellow")]
public class YellowElt : EltType
{
    public override void OnLeftClick(int x, int y)
    {
        //Debug.Log($"left clicked Yellow at {x},{y}");
    }

    public override void OnRightClick(int x, int y)
    {
       // Debug.Log($"right clicked Yellow at {x},{y}");
    }
}