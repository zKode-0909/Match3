using UnityEngine;

[CreateAssetMenu(fileName = "GreenElt", menuName = "EltType/Green")]
public class GreenElt : EltType
{
    public override void OnLeftClick(int x, int y)
    {
        //Debug.Log($"left clicked Green at {x},{y}");
    }

    public override void OnRightClick(int x, int y)
    {
       // Debug.Log($"right clicked Green at {x},{y}");
    }



}