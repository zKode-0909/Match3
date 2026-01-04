using System.Collections.Generic;
using System;
using UnityEngine;


[CreateAssetMenu(menuName = "EltType/Base")]
public class EltType : ScriptableObject
{

    public GridSlot prefab;
    public Sprite sprite;
    public int weight;
    public int score;
    public string EltName;


    public void OnClick() {
        Debug.Log("I have been clicked");
    }


    public virtual GridSlot Create()
    {
        var go = Instantiate(prefab);
        go.gameObject.SetActive(false);
        go.gameObject.name = prefab.name;

        //var flyweight = go.GetComponent<GridSlot>(); //might want to just put the flyweight component on the prefab
        //go.GetComponent<GridSlot>().type = this;
        var gridSlot = go.GetComponent<GridSlot>();
        gridSlot.type = this;
        

        return gridSlot;
    }

    public virtual void OnGet(GridSlot f) => f.gameObject.SetActive(true);
    public virtual void OnRelease(GridSlot f) {
        Debug.Log("REEEEEALEEASE ME");
        f.gameObject.SetActive(false);
    } 
    public virtual void OnDestroyPoolObject(GridSlot f) => Destroy(f.gameObject);






}













