using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;

[CustomEditor(typeof(AdvancedScriptableObject), true)]
public class Editor_AdvancedScriptableObject : Editor {

    private AdvancedScriptableObject _backupParent;
    private AdvancedScriptableObject _parent;
    private AdvancedScriptableObject _aso;

    private ASOReferenceData[] _relevantRefData;

    /// <summary>
    /// Property Name, list
    /// </summary>
    private Dictionary<string, Editor_List> _lists;
    private Dictionary<string, Editor_ASOList> _asoLists;
    private bool _bListsCreated;

    private void OnEnable()
    {
        _aso = (AdvancedScriptableObject)target;
        _parent = _aso.ProtoParent;
        _backupParent = _parent;
        ASOManager.Me.VaildateData();
        GetRelevantRefData();
        CheckNullChildren();
        if (!_bListsCreated)
            SetupLists();
    }

    void GetRelevantRefData()
    {
        _relevantRefData = ASOManager.Me.GetReferencers(_aso);
        if (_relevantRefData == null)
            _relevantRefData = new ASOReferenceData[0];
    }

    void SetupLists()
    {
        _lists = new Dictionary<string, Editor_List>();
        _asoLists = new Dictionary<string, Editor_ASOList>();
        //Create new lists for each array property
        #region Create Lists from Properties

        SerializedProperty properties = serializedObject.GetIterator();
        while (properties.NextVisible(true))
        {
            if (properties.isArray && properties.propertyType != SerializedPropertyType.String)
            {
                //Debug.Log(properties.name);

                if (properties.name == "data")
                    continue;

                System.Type type = AdvancedScriptableObjectUtility.GetSerializedPropertyType(properties);
                //Debug.Log(type);

                if (type != null)
                {
                    if (type.GetElementType().IsSubclassOf(typeof(AdvancedScriptableObject)))
                    {

                        SerializedProperty asoPropCopy = properties.Copy();
                        if (!_asoLists.ContainsKey(asoPropCopy.name))
                            _asoLists.Add(asoPropCopy.name, new Editor_ASOList(asoPropCopy));
                        continue;
                    }
                    else
                    {
                        SerializedProperty propCopy = properties.Copy();
                        if (!_asoLists.ContainsKey(propCopy.name))
                            _lists.Add(propCopy.name, new Editor_List(propCopy));
                        continue;
                    }
                }
            }
        }
        #endregion
        _bListsCreated = true;

        //Debug.Log(string.Format("List count:{0}  ASOList count:{1}",_lists.Count,_asoLists.Count));
    }

    void CheckNullChildren()
    {
        var possibleChildren = _aso.ProtoChildren;
        for (int i = 0; i < possibleChildren.Count; i++)
        {
            if (possibleChildren[i] == null)
            {
                possibleChildren.RemoveAt(i);
                i--;
                continue;
            }
        }

    }

    public override void OnInspectorGUI()
    {

        //If in isolated editing mode
        if (Selection.activeObject == serializedObject.targetObject)
        {
            if (_aso.ParentAsset != null)
            {
                var parentChain = GetParentChain(new List<AdvancedScriptableObject>(), _aso);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("Object Hierarchy");

                EditorGUILayout.BeginHorizontal();
                for (int i = parentChain.Count - 1; i > -1; i--)
                {
                    if (GUILayout.Button(parentChain[i].name))
                        Selection.activeObject = parentChain[i];
                    GUILayout.Label("<");
                    //Create new line every 4 buttons
                    if (i % 4 == 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

            }
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();

        _parent = EditorGUILayout.ObjectField("Prototype Parent", _parent, typeof(AdvancedScriptableObject), true) as AdvancedScriptableObject;
        if (EditorGUI.EndChangeCheck())
            ParentChanged();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(_parent == null);

        if (GUILayout.Button("Apply"))
        {
            if (EditorUtility.DisplayDialog("Apply values to prototype", "Overwrite values of parent?", "Confirm", "Cancel"))
            {
                var parSerObj = new SerializedObject(_parent);
                AdvancedScriptableObjectUtility.CleanObject(_parent);
                //parSerObj.ApplyModifiedProperties();
                //parSerObj.Update();
                AdvancedScriptableObjectUtility.CloneData(_parent, _aso, _parent);
                //parSerObj.Update();
                //parSerObj.ApplyModifiedProperties();
                //AdvancedScriptableObjectUtility.CopyDataUnity(parSerObj, serializedObject, parSerObj);
            }
        }

        if (GUILayout.Button("Restore"))
        {
            if (EditorUtility.DisplayDialog("Restore to prototype values", "Overwrite values of this object to match prototype?",
                "Confirm", "Cancel"))
            {
                AdvancedScriptableObjectUtility.CleanObject(_aso);
                serializedObject.ApplyModifiedProperties();
                AdvancedScriptableObjectUtility.CloneData(_aso, _parent, _aso);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                serializedObject.UpdateIfRequiredOrScript();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        if (GUILayout.Button("Break"))
        {
            BreakTieToParent();
        }

        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Clean"))
        {
            if (EditorUtility.DisplayDialog("Clean", "Delete all values of this object?", "Confirm", "Cancel"))
            {
                AdvancedScriptableObjectUtility.CleanObject(_aso);
                serializedObject.ApplyModifiedProperties();
                serializedObject.UpdateIfRequiredOrScript();
            }
        }

        if (GUILayout.Button("Alter"))
            SetToPrototype();

        EditorGUILayout.EndHorizontal();


        if (_aso != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(_aso.ProtoChildren.Count < 1);

            EditorGUILayout.LabelField(string.Format("Children:{0}", _aso.ProtoChildren.Count));

            if (GUILayout.Button("Goto"))
            {
                GoToChild();
            }

            if (GUILayout.Button("Update"))
            {
                AdvancedScriptableObjectUtility.UpdateChildren(_aso);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            //Referencing
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(_relevantRefData == null || _relevantRefData.Length == 0);
            
            EditorGUILayout.LabelField(string.Format("Externally Referenced by:{0}", _relevantRefData.Length));



            if (GUILayout.Button("GoTo"))
                GoToReferencerMenu();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }


        EditorGUILayout.EndVertical();

        //DrawDefaultInspector();
        DrawBaseExcept();
    }

    List<AdvancedScriptableObject> GetParentChain(List<AdvancedScriptableObject> chain, AdvancedScriptableObject curObj)
    {
        if (curObj.ParentAsset != null)
        {
            chain.Add(curObj.ParentAsset);
            //continue chain
            return GetParentChain(chain, curObj.ParentAsset);
        }
        else
            return chain;
    }

    void DrawBaseExcept()
    {
        SerializedProperty myProp = serializedObject.GetIterator();

        while (myProp.NextVisible(true))
        {
            if (!myProp.isArray && myProp.propertyType != SerializedPropertyType.ArraySize ||
                myProp.isArray && myProp.propertyType == SerializedPropertyType.String)
            {
                if (myProp.name != "_parentAsset" && myProp.name != "data")
                {

                    if (myProp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        System.Type type = AdvancedScriptableObjectUtility.GetSerializedPropertyType(myProp);
                        if (type != null)
                        {
                            if (type.IsSubclassOf(typeof(AdvancedScriptableObject)))
                            {
                                EditorGUILayout.PropertyField(myProp);
                                //var asoPropEditor = Editor.CreateEditor(myProp.objectReferenceValue);
                                //asoPropEditor.OnInspectorGUI();
                                //ManualDrawASOProperty(myProp, type);
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(myProp, new GUIContent(myProp.displayName), false);
                                serializedObject.ApplyModifiedProperties();
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(myProp, new GUIContent(myProp.displayName), false);
                        serializedObject.ApplyModifiedProperties();
                    }

                }
            }
            else if (myProp.isArray)
            {
                if (myProp.name != "_protoChildren")
                {
                    System.Type type = AdvancedScriptableObjectUtility.GetSerializedPropertyType(myProp);
                    if (type != null)
                    {
                        if (type.GetElementType().IsSubclassOf(typeof(AdvancedScriptableObject)))
                        {
                            if (_asoLists.ContainsKey(myProp.name))
                                _asoLists[myProp.name].DrawList();
                        }
                        else
                        {
                            if (_lists.ContainsKey(myProp.name))
                                _lists[myProp.name].DrawList();
                        }
                    }
                }
            }
        }

    }

    void ParentChanged()
    {
        if (_parent == _aso)
        {
            Debug.LogError("Cannot set parent as self!");
            _parent = _backupParent;
        }
        else
        {
            if (_backupParent != null && _backupParent.ProtoChildren.Contains(_aso))
                _backupParent.ProtoChildren.Remove(_aso);
            _backupParent = _parent;
            serializedObject.FindProperty("_protoParent").objectReferenceValue = _parent;

            _parent.ProtoChildren.Add(_aso);
        }
        serializedObject.ApplyModifiedProperties();

    }

    void BreakTieToParent()
    {
        serializedObject.FindProperty("_protoParent").objectReferenceValue = null;

        _parent.ProtoChildren.Remove(_aso);
        new SerializedObject(_parent).ApplyModifiedProperties();
        _parent = null;
        serializedObject.ApplyModifiedProperties();
    }

    void SetToPrototype()
    {
        System.Type type = _aso.GetType();
        string[] availablePrototypes = AssetDatabase.FindAssets(string.Format("t:{0}", type));
        if (availablePrototypes.Length > 0)
        {
            //Get the actual names of each object
            List<AdvancedScriptableObject> assets = new List<AdvancedScriptableObject>();
            for (int i = 0; i < availablePrototypes.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(availablePrototypes[i]);
                AdvancedScriptableObject obj = AssetDatabase.LoadAssetAtPath(path, typeof(AdvancedScriptableObject)) as AdvancedScriptableObject;
                //Prevent parenting to self
                assets.Add(obj);
            }
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < availablePrototypes.Length; i++)
            {
                GUIContent gc = new GUIContent(assets[i].name);
                menu.AddItem(gc, false, OnSelectedPrototype, availablePrototypes[i]);
            }
            menu.ShowAsContext();
        }
        else
            Debug.LogWarning("No suitable prototypes found");
    }

    void OnSelectedPrototype(object userData)
    {
        AdvancedScriptableObject obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath((string)userData), typeof(AdvancedScriptableObject)) as AdvancedScriptableObject;
        _parent = obj;
        serializedObject.ApplyModifiedProperties();
        ParentChanged();
    }

    void GoToChild()
    {
        GenericMenu menu = new GenericMenu();
        for (int i = 0; i < _aso.ProtoChildren.Count; i++)
        {
            GUIContent gc = new GUIContent(string.Format("[{0}]{1}", i, _aso.ProtoChildren[i].name));
            menu.AddItem(gc, false, GoToChildSelected, _aso.ProtoChildren[i]);
        }
        menu.ShowAsContext();
    }

    void GoToChildSelected(object userData)
    {
        UnityEngine.Object obj = (UnityEngine.Object)userData;
        Selection.activeObject = obj;
        if (AssetDatabase.IsSubAsset(obj))
        {
            var mainAsset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(obj), typeof(AdvancedScriptableObject));
            EditorGUIUtility.PingObject(mainAsset);
        }
        else
            EditorGUIUtility.PingObject(obj);
    }

    void GoToReferencerMenu()
    {
        GenericMenu menu = new GenericMenu();
        for (int i = 0; i < _relevantRefData.Length; i++)
        {
            GUIContent gc;
            if (_relevantRefData[i].BIsArrayType)
                gc = new GUIContent(string.Format("[{0}]{1}:[{2}][{3}]", i, _relevantRefData[i].ReferencerASO.name, _relevantRefData[i].ReferencingFieldName, _relevantRefData[i].ArrayIndex));
            else
                gc = new GUIContent(string.Format("[{0}]{1}:[{2}]", i, _relevantRefData[i].ReferencerASO.name, _relevantRefData[i].ReferencingFieldName));
            menu.AddItem(gc, false, GoToReferencerSelected, _relevantRefData[i]);
        }
        menu.ShowAsContext();
    }

    void GoToReferencerSelected(object userData)
    {
        ASOReferenceData refInfo = (ASOReferenceData)userData;
        Selection.activeObject = refInfo.ReferencerASO;

        EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(refInfo.ReferencerASO)));
    }

}
