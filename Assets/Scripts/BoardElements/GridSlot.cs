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
    private bool MarkedForDestroy = false;
    [SerializeField] Image imageComponent;
    public RectTransform rectTransform;

    public void Init(EltType type_,int x_,int y_) { 
        type = type_;
        x = x_;
        y = y_;
        //Debug.Log($"image sprite: {gameObject.GetComponent<Image>().sprite}");
        gameObject.GetComponent<Image>().sprite = type.sprite;
        gameObject.name = $"{x},{y}";
    }

    

    void EnsureHighlight()
    {
        if (highlight != null) return;
        highlight = Instantiate(highlightPrefab, imageComponent.transform, false);
        highlight.gameObject.SetActive(false);
    }

    public void ShowHighlightAt(Vector2 pos, float paddingPx,Vector2 cellSize)
    {
        EnsureHighlight();

        highlight.gameObject.SetActive(true);

        // position highlight to cell center
        highlight.anchoredPosition = pos;

        // size it to cell size (+ optional padding)
        highlight.sizeDelta = new Vector2(
        cellSize.x + paddingPx * 2f,
            cellSize.y + paddingPx * 2f
        );

        // ensure it renders on top
        highlight.SetAsLastSibling();
    }

    public void HideHighlight()
    {
        if (highlight != null) highlight.gameObject.SetActive(false);
    }

    public void HandleLeftClick() {
        type.OnLeftClick(x,y);

    }

    public void HandleRightClick() { 
        type.OnRightClick(x,y);
    }

    public bool GetMarkedForDestroy() { 
        return MarkedForDestroy;
    }

    public void SetMarkedForDestroy(bool mark) { 
        MarkedForDestroy=mark;
    }


   




}
