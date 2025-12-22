using UnityEngine;

[CreateAssetMenu(fileName = "RedElt", menuName = "EltType/Red")]
public class RedElt : EltType
{
    public override void OnLeftClick(int x, int y)
    {
        //Debug.Log($"left clicked Red at {x},{y}");
    }

    public override void OnRightClick(int x, int y)
    {
        //Debug.Log($"right clicked Red at {x},{y}");
    }
}