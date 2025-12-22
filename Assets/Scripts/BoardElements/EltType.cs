using System.Collections.Generic;
using System;
using UnityEngine;


[CreateAssetMenu(fileName = "EltType", menuName = "EltType/Base")]
public abstract class EltType : ScriptableObject
{

    public Sprite sprite;
    public int weight;
    public int score;

    public EltType() { 
        
    }

    public virtual void OnLeftClick(int x,int y) {
        Debug.Log($"left clicked base at {x},{y}");
    }

    public virtual void OnRightClick(int x, int y)
    {
        Debug.Log($"right clicked base at {x},{y}");
    }


    public virtual void OnDestroy() {
        Debug.Log($"destroyed me for {score}!");
    }



    


}









public enum EltTypes {Blue,Red,Green,Yellow,Bomb }



