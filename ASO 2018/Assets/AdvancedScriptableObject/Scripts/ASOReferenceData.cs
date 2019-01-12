using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ASOReferenceData : System.Object {

    [SerializeField]
    private AdvancedScriptableObject _referencingASO;
    public AdvancedScriptableObject ReferencerASO
    {
        get { return _referencingASO; }
    }

    [SerializeField]
    private AdvancedScriptableObject _referencedASO;
    public AdvancedScriptableObject ReferencedASO
    {
        get { return _referencedASO; }
    }

    [SerializeField]
    private string _referencingFieldName;
    public string ReferencingFieldName
    {
        get { return _referencingFieldName; }
    }

    [SerializeField]
    private bool _bIsArrayType;
    public bool BIsArrayType
    {
        get { return _bIsArrayType; }
    }

    [SerializeField]
    private int _arrayIndex;
    public int ArrayIndex
    {
        get { return _arrayIndex; }
    }

    public ASOReferenceData(AdvancedScriptableObject referencingASO,AdvancedScriptableObject referencedASO, string referencingFieldName)
    {
        _referencingASO = referencingASO;
        _referencedASO = referencedASO;
        _referencingFieldName = referencingFieldName;
    }

    public ASOReferenceData(AdvancedScriptableObject referencingASO,AdvancedScriptableObject referencedASO, string referencingFieldName, int arrayIndex)
    {
        _referencingASO = referencingASO;
        _referencedASO = referencedASO;
        _referencingFieldName = referencingFieldName;
        _arrayIndex = arrayIndex;
        _bIsArrayType = true;
        
    }
}
