using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AdvancedScriptableObject : ScriptableObject {


#if UNITY_EDITOR
    //used to maintain link to prototype parent
    [SerializeField][HideInInspector]
    protected AdvancedScriptableObject _protoParent;
    public AdvancedScriptableObject ProtoParent
    {
        get { return _protoParent; }
    }
    //used to maintain references to current prototype children
    [SerializeField]
    [HideInInspector]
    protected List<AdvancedScriptableObject> _protoChildren = new List<AdvancedScriptableObject>();
    public List<AdvancedScriptableObject> ProtoChildren
    {
        get { return _protoChildren; }
    }

    //Used for easy navigation when in isolated editing mode
    [SerializeField]
    protected AdvancedScriptableObject _parentAsset;
    public AdvancedScriptableObject ParentAsset
    {
        get { return _parentAsset; }
    }

#endif

}
