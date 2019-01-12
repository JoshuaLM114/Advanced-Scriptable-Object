using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "ASO/Dummy")]
public class ASO_Dummy : AdvancedScriptableObject {

    public string dummyString;
    public float dummyFloat;

    //[ASOMergeable(typeof(ASO_Type2))]
    [ASO]
    public ASO_Type2 type2;

    public string[] stringArray;

    //[ASOMergeable(typeof(ASO_Type2))]
    [ASO]
    public ASO_Type2[] arrayType;

    public Material mat;
}
