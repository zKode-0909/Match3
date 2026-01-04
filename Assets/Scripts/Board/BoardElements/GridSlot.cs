using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.CompilerServices;

public class GridSlot : MonoBehaviour
{
    [SerializeField] RectTransform highlightPrefab;
    RectTransform highlight;
    public int x;
    public int y;
    public EltType type;
    [SerializeField] Image imageComponent;
    public RectTransform rectTransform;
    
    public void Init(int x_,int y_) { 
        x = x_;
        y = y_;
        //Debug.Log($"image sprite: {gameObject.GetComponent<Image>().sprite}");
        imageComponent.sprite = type.sprite;
        gameObject.name = $"{x},{y}";
        
    }
    
    

    public void EnsureHighlight()
    {
        if (highlight != null) return;
        //highlight = Instantiate(highlightPrefab, imageComponent.transform, false);

        highlightPrefab.gameObject.SetActive(false);
    }

    public void ShowHighlightAt(Vector2 pos, float paddingPx,Vector2 cellSize)
    {
        if (highlightPrefab == null) return;
        Debug.Log($"showing highlight at: {pos}");
        highlightPrefab.gameObject.SetActive(true);

        // position highlight to cell center
        highlightPrefab.anchoredPosition = pos;

        // size it to cell size (+ optional padding)
        highlightPrefab.sizeDelta = new Vector2(
        (cellSize.x) + 25,
            (cellSize.y) + 25 
        );

        // ensure it renders on top
        highlightPrefab.SetAsLastSibling();
    }
    /*
    public void ShowHighlight(Vector2 cellSize, float paddingPx = 0f)
    {
        

        highlight.gameObject.SetActive(true);
        highlight.SetAsLastSibling();

        highlight.sizeDelta = new Vector2(
            cellSize.x + paddingPx,
            cellSize.y + paddingPx
        );
    }*/

    public void HideHighlight()
    {
        if (highlightPrefab != null) highlightPrefab.gameObject.SetActive(false);
    }

    public void HandleLeftClick() {
        Debug.Log($"just clicked {type.EltName}");


    }

    public void HandleRightClick() { 
     
    }


  

    







}
