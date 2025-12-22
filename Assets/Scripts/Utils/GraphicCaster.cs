using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class GraphicCaster : MonoBehaviour
{
    [SerializeField] MouseClickHandler MouseTracker;
    [SerializeField] GraphicRaycaster raycaster;





    public List<RaycastResult> graphicCast(int targetLayer)
    {

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Mouse.current.position.x.value, Mouse.current.position.y.value)
        };
        //moveImage.gameObject.transform.position = pointerEventData.position;
       // Debug.Log($"positions: {pointerEventData.position}   {moveImage.transform.position}    {MouseTracker.GetMousePosition()}");
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);


        return results.Where(r => r.gameObject.layer == targetLayer).ToList();
    }
}