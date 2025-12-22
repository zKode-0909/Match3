using UnityEngine;

[CreateAssetMenu(fileName = "EmptyElt", menuName = "EltType/Empty")]
public class EmptyElt : EltType
{
    public override void OnLeftClick(int x, int y)
    {
        //Debug.Log($"left clicked Empty at {x},{y}");
    }

    public override void OnRightClick(int x, int y)
    {
        // Debug.Log($"right clicked Green at {x},{y}");
    }
}