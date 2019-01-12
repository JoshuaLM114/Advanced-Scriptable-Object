using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "ASO/Type2")]
public class ASO_Type2 : AdvancedScriptableObject {

    public float num;

    public int[] intArray;
    //[ASOMergeable(typeof(ASO_Dummy))]
    [ASO]
    public ASO_Dummy dummy;
}
