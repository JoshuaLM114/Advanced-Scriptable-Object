using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[System.Serializable]
[CreateAssetMenu(menuName = "ASO/Manager")]
public class ASOManager : ScriptableObject {
    [SerializeField]
    private static ASOManager _me;
    public static ASOManager Me
    {
        get {
            if (_me == null)
            {
                var existing = AssetDatabase.FindAssets("t:ASOManager");
                if (existing.Length == 0)
                {
                    Debug.LogError("ASOManager does not exist. Please create one");
                    return null;
                }
                else
                {
                    _me = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(existing[0]), typeof(ASOManager)) as ASOManager;
                    return _me;
                }
            } 
            else
                return _me;
        }
    }

    [SerializeField]
    private AdvancedScriptableObject _copyBuffer;
    public AdvancedScriptableObject CopyBuffer
    {
        get
        {
            return _copyBuffer;
        }
        set
        {
            _copyBuffer = value;
        }
    }

    [SerializeField]
    private List<ASOReferenceData> _referenceData = new List<ASOReferenceData>();
    //public List<ASOReferenceData> ReferenceData
    //{
    //    get { return _referenceData; }
    //}

    public ASOReferenceData[] GetReferencers(AdvancedScriptableObject aso)
    {
        if (_referenceData.Count > 0)
        {
            var search = _referenceData.FindAll(x => x.ReferencedASO == aso);
            return search.ToArray();
        }
        else
            return new ASOReferenceData[0];
    }

    public ASOReferenceData[] GetReferencesOn(AdvancedScriptableObject aso)
    {
        if (_referenceData.Count > 0)
        {
            var search = _referenceData.FindAll(x => x.ReferencerASO == aso);
            return search.ToArray();
        }
        else
            return new ASOReferenceData[0];
    }

    public void RemoveReferencers(AdvancedScriptableObject aso)
    {
        var referencers = GetReferencers(aso);
        if(referencers.Length > 0)
            foreach(var refData in referencers)
            {
                _referenceData.Remove(refData);
            }
    }

    public void RemoveReferencesOn(AdvancedScriptableObject aso)
    {
        var references = GetReferencesOn(aso);
        if(references.Length > 0)
            foreach(var refData in references)
            {
                _referenceData.Remove(refData);
            }
    }

    public void RemoveReference(AdvancedScriptableObject referencer,AdvancedScriptableObject reference)
    {
        if (_referenceData.Count > 0)
        {
            var relevant = _referenceData.FindAll(x => x.ReferencerASO == referencer);
            var search = relevant.Find(x => x.ReferencedASO == reference);
            _referenceData.Remove(search);
        }
    }

    public void BreakReferencesOn(AdvancedScriptableObject aso)
    {
        var references = GetReferencesOn(aso);
        if (references.Length > 0) {
            for (int i = 0; i < references.Length; i++)
            {
                var refData = references[i];

                if (refData.BIsArrayType)
                {
                    var field = refData.ReferencerASO.GetType().GetField(refData.ReferencingFieldName);
                    var array = field.GetValue(refData.ReferencerASO) as Array;
                    array.SetValue(null, refData.ArrayIndex);
                }
                else
                {
                    var field = refData.ReferencerASO.GetType()
                        .GetField(refData.ReferencingFieldName);

                    field.SetValue(refData.ReferencerASO, null);
                }
                _referenceData.Remove(refData);
            }
        }


    }

    public void AddReferenceData(AdvancedScriptableObject referencer,AdvancedScriptableObject referenced, string referencingFieldName)
    {
        if (_referenceData.Count > 0)
        {
            var relevant = _referenceData.FindAll(x => x.ReferencerASO == referencer);
            var search = relevant.Find(x => x.ReferencedASO == referenced);
            if (search == null)
                _referenceData.Add(new ASOReferenceData(referencer, referenced, referencingFieldName));
        }
        else
            _referenceData.Add(new ASOReferenceData(referencer, referenced, referencingFieldName));
    }
    public void AddReferenceData(AdvancedScriptableObject referencer, AdvancedScriptableObject referenced, string referencingFieldName, int arrayIndex)
    {
        if (_referenceData.Count > 0)
        {
            var relevant = _referenceData.FindAll(x => x.ReferencerASO == referencer);
            var search = relevant.Find(x => x.ReferencedASO == referenced);
            if (search == null)
                _referenceData.Add(new ASOReferenceData(referencer, referenced, referencingFieldName, arrayIndex));
        }
        else
            _referenceData.Add(new ASOReferenceData(referencer, referenced, referencingFieldName, arrayIndex));
    }
    public void RemoveAllReferenceData(AdvancedScriptableObject aso)
    {
        if (_referenceData.Count > 0)
        {
            var referencers = _referenceData.FindAll(x => x.ReferencedASO == aso);
            if (referencers.Count > 0)
                foreach (var refData in referencers)
                    _referenceData.Remove(refData);

            var referenced = _referenceData.FindAll(x => x.ReferencerASO == aso);
            if (referenced.Count > 0)
                foreach (var refData in referenced)
                    _referenceData.Remove(refData);
        }
    }

    public void VaildateData()
    {
        if(_referenceData != null && _referenceData.Count > 0)
            for (int i = _referenceData.Count - 1; i != -1; i--)
            {
                if (_referenceData[i].ReferencedASO == null || _referenceData[i].ReferencerASO == null)
                    _referenceData.RemoveAt(i);
            }
    }
}
