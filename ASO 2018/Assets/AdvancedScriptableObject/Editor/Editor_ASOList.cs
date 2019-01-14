using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;

[System.Serializable]
public class Editor_ASOList : System.Object {

    private SerializedProperty _property;
    public SerializedProperty Property
    {
        get { return _property; }
    }

    private ReorderableList _list;

    public Editor_ASOList(SerializedProperty property)
    {
        _property = property;
        SetupList();
    }

    void SetupList()
    {
        _list = new ReorderableList(_property.serializedObject, _property, true, true, true, true);
        _list.drawHeaderCallback = List_DrawHeader;
        _list.onCanRemoveCallback = List_BCanRemove;
        _list.onRemoveCallback = List_ElementDeleted;
        _list.drawElementCallback = NewDrawElement;
        _list.onAddDropdownCallback = List_AddDropdown;
    }

    void NewDrawElement(Rect rect,int index,bool isActive, bool isFocused)
    {
        var element = _list.serializedProperty.GetArrayElementAtIndex(index);
        EditorGUI.PropertyField(rect, element);
    }

    bool List_BCanRemove(ReorderableList l)
    {
        return l.count > 0;
    }

    void List_DrawHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, _property.displayName);
    }

    void List_ElementDeleted(ReorderableList list)
    {
        var element = _list.serializedProperty.GetArrayElementAtIndex(_list.index);
        MonoBehaviour.DestroyImmediate(element.objectReferenceValue, true);
        element.objectReferenceValue = null;

        ReorderableList.defaultBehaviours.DoRemoveButton(_list);

        EditorUtility.SetDirty(_property.serializedObject.targetObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
    }


    public void DrawList()
    {
        _list.DoLayoutList();
    }


    void List_AddDropdown(Rect buttonRect,ReorderableList list)
    {
        System.Type propType = AdvancedScriptableObjectUtility.GetSerializedPropertyType(_property).GetElementType();
        System.Type[] types = Assembly.GetAssembly(propType).GetTypes();

        List<string> allTypes = (from System.Type t in types where t.IsSubclassOf(propType) select t.FullName).ToList();
        if (!propType.IsAbstract)
        {
            allTypes.Add(propType.FullName);
        }
        allTypes.Insert(0,"Empty");

        var menu = new GenericMenu();

        for (int i = 0; i < allTypes.Count; i++)
        {
            GUIContent gc = new GUIContent(allTypes[i]);
            menu.AddItem(gc, false, List_Dropdown_OnSelected, allTypes[i]);
        }
        menu.ShowAsContext();
    }

    void List_Dropdown_OnSelected(object selection)
    {
        if (selection != null)
        {            
            string objSel = (string)selection;

            if (objSel != "Empty")
            {
                var newObj = ScriptableObject.CreateInstance(objSel);

                if (newObj == null)
                    Debug.Log("New obj is null");

                newObj.name = objSel;
                newObj.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(newObj, _property.serializedObject.targetObject);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newObj));

                AdvancedScriptableObject aso = (AdvancedScriptableObject)newObj;
                SerializedObject sObjAso = new SerializedObject(aso);
                sObjAso.FindProperty("_parentAsset").objectReferenceValue = _property.serializedObject.targetObject;
                sObjAso.ApplyModifiedProperties();

                //Add new array element
                _list.serializedProperty.arraySize++;
                var newEl = _list.serializedProperty.GetArrayElementAtIndex(_list.serializedProperty.arraySize - 1);
                newEl.objectReferenceValue = newObj;

                _property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_property.serializedObject.targetObject);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                _list.serializedProperty.arraySize++;
                SerializedProperty prop = _property.GetArrayElementAtIndex(_list.serializedProperty.arraySize - 1);
                prop.objectReferenceValue = null;
                _property.serializedObject.ApplyModifiedProperties();
            }
        }
    }

    //void NullElement(int index)
    //{
    //    SerializedProperty property = _property.GetArrayElementAtIndex(index);
    //    AdvancedScriptableObject aso = (AdvancedScriptableObject)property.objectReferenceValue;
    //    //var search = aso.ReferencingASOs.Find(x => x.ReferencingASO == _property.serializedObject.targetObject);
    //    //aso.ReferencingASOs.Remove(search);

    //    ASOManager.Me.RemoveReference((AdvancedScriptableObject)_property.serializedObject.targetObject, aso);
    //    property.objectReferenceValue = null;
    //    property.serializedObject.ApplyModifiedProperties();
    //}


    //struct MenuSelection
    //{
    //    private int _index;
    //    public int Index
    //    {
    //        get { return _index; }
    //    }
    //    private string _selection;
    //    public string Selected
    //    {
    //        get { return _selection; }
    //    }

    //    public MenuSelection(int index,string selection)
    //    {
    //        _index = index;
    //        _selection = selection;
    //    }
    //}

    //void CreateNew(int index)
    //{
    //    System.Type propType = AdvancedScriptableObjectUtility.GetSerPropType(_property).GetElementType();
    //    System.Type[] types = Assembly.GetAssembly(propType).GetTypes();

    //    List<string> allTypes = (from System.Type t in types where t.IsSubclassOf(propType) select t.FullName).ToList();
    //    if (!propType.IsAbstract)
    //    {
    //        allTypes.Add(propType.FullName);
    //    }

    //    var menu = new GenericMenu();

    //    for (int i = 0; i < allTypes.Count; i++)
    //    {
    //        GUIContent gc = new GUIContent(allTypes[i]);
    //        menu.AddItem(gc, false, CreateNewSelected, new MenuSelection(index, allTypes[i]));
    //    }
    //    menu.ShowAsContext();
    //}

    //void CreateNewSelected(object userData)
    //{
    //    MenuSelection selection = (MenuSelection)userData;

    //    var newObj = ScriptableObject.CreateInstance(selection.Selected);

    //    if (newObj == null)
    //        Debug.Log("New obj is null");

    //    newObj.name = selection.Selected;
    //    newObj.hideFlags = HideFlags.HideInHierarchy;
    //    AssetDatabase.AddObjectToAsset(newObj, _property.serializedObject.targetObject);
    //    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newObj));

    //    AdvancedScriptableObject aso = (AdvancedScriptableObject)newObj;
    //    SerializedObject sObjAso = new SerializedObject(aso);
    //    sObjAso.FindProperty("_parentAsset").objectReferenceValue = _property.serializedObject.targetObject;
    //    sObjAso.ApplyModifiedProperties();

    //    //Set to property
    //    SerializedProperty prop = _property.GetArrayElementAtIndex(selection.Index);
    //    prop.objectReferenceValue = newObj;

    //    _property.serializedObject.ApplyModifiedProperties();
    //    EditorUtility.SetDirty(_property.serializedObject.targetObject);
    //    AssetDatabase.SaveAssets();
    //    AssetDatabase.Refresh();
    //}


    //void UnmergeMenu(int index)
    //{
    //    var menu = new GenericMenu();
    //    string[] options = new string[] { "Current", "All" };
    //    for (int i = 0; i < options.Length; i++)
    //    {
    //        GUIContent gc = new GUIContent(options[i]);
    //        menu.AddItem(gc, false, UnmergeSelected, new MenuSelection(index,options[i]));
    //    }
    //    menu.ShowAsContext();
    //}

    //void UnmergeSelected(object userData)
    //{
    //    MenuSelection selection = (MenuSelection)userData;

    //    if (selection.Selected == "Current")
    //    {
    //        SerializedProperty prop = _property.GetArrayElementAtIndex(selection.Index);

    //        prop.objectReferenceValue = AdvancedScriptableObjectUtility.UnmergeCurrent(
    //            (AdvancedScriptableObject)_property.serializedObject.targetObject,
    //            (AdvancedScriptableObject)prop.objectReferenceValue);
    //        prop.serializedObject.ApplyModifiedProperties();
    //        AdvancedScriptableObject aso = (AdvancedScriptableObject)prop.objectReferenceValue;
    //        //aso.ReferencingASOs.Add(new ASOReferenceInfo((AdvancedScriptableObject)_property.serializedObject.targetObject, AdvancedScriptableObjectUtility.GetArrayName(prop), selection.Index));
    //        ASOManager.Me.AddReferenceData((AdvancedScriptableObject)_property.serializedObject.targetObject,aso, AdvancedScriptableObjectUtility.GetArrayName(prop), selection.Index);
    //    }
    //    else if (selection.Selected == "All")
    //    {
    //        SerializedProperty prop = _property.GetArrayElementAtIndex(selection.Index);
    //        //Need to first remove the object from serialized object
    //        prop.objectReferenceValue = AdvancedScriptableObjectUtility.UnmergeAll(
    //            (AdvancedScriptableObject)_property.serializedObject.targetObject,
    //            (AdvancedScriptableObject)prop.objectReferenceValue);
    //        prop.serializedObject.ApplyModifiedProperties();
    //        AdvancedScriptableObject aso = (AdvancedScriptableObject)prop.objectReferenceValue;
    //        //aso.ReferencingASOs.Add(new ASOReferenceInfo((AdvancedScriptableObject)_property.serializedObject.targetObject, AdvancedScriptableObjectUtility.GetArrayName(prop), selection.Index));
    //        ASOManager.Me.AddReferenceData((AdvancedScriptableObject)_property.serializedObject.targetObject,aso, AdvancedScriptableObjectUtility.GetArrayName(prop), selection.Index);

    //    }
    //}

    //void MergeMenu(int index)
    //{
    //    var menu = new GenericMenu();
    //    string[] options = new string[] { "Current", "All", "Current Copy", "All Copy" };
    //    for (int i = 0; i < options.Length; i++)
    //    {
    //        GUIContent gc = new GUIContent(options[i]);
    //        menu.AddItem(gc, false, MergeSelected, new MenuSelection(index, options[i]));
    //    }
    //    menu.ShowAsContext();
    //}

    //void MergeSelected(object userData)
    //{
    //    MenuSelection selection = (MenuSelection)userData;
    //    SerializedProperty property = _property.GetArrayElementAtIndex(selection.Index);
    //    AdvancedScriptableObject asoToMerge = (AdvancedScriptableObject)property.objectReferenceValue;
    //    ASOReferenceData[] toMergeReferences = ASOManager.Me.GetReferencers(asoToMerge);

    //    if (selection.Selected == "Current")
    //    {
    //        if (toMergeReferences.Length > 1)
    //        {
    //            if (!EditorUtility.DisplayDialog(
    //                "Object is referenced",
    //                string.Format("Merging this object will lose references on {0} objects. Continue?", toMergeReferences.Length - 1),
    //                "Confirm",
    //                "Cancel"))
    //                return;
    //        }

    //        property.objectReferenceValue = AdvancedScriptableObjectUtility.MergeCurrent(
    //            (AdvancedScriptableObject)_property.serializedObject.targetObject,
    //            (AdvancedScriptableObject)property.objectReferenceValue);

    //        property.serializedObject.ApplyModifiedProperties();
    //    }
    //    else if (selection.Selected == "All")
    //    {
    //        if (toMergeReferences.Length > 1)
    //        {
    //            if (!EditorUtility.DisplayDialog(
    //                "Object is referenced",
    //                string.Format("Merging this object will lose references on multiple objects. Continue?"),
    //                "Confirm",
    //                "Cancel"))
    //                return;
    //        }

    //        property.objectReferenceValue = AdvancedScriptableObjectUtility.MergeAll(
    //            (AdvancedScriptableObject)_property.serializedObject.targetObject,
    //            (AdvancedScriptableObject)property.serializedObject.targetObject,
    //            (AdvancedScriptableObject)property.objectReferenceValue);

    //        property.serializedObject.ApplyModifiedProperties();
    //    }
    //    else if (selection.Selected == "Current Copy")
    //    {
    //        property.objectReferenceValue = AdvancedScriptableObjectUtility.MergeCurrent(
    //            (AdvancedScriptableObject)_property.serializedObject.targetObject,
    //            (AdvancedScriptableObject)property.objectReferenceValue,
    //            true);

    //        property.serializedObject.ApplyModifiedProperties();
    //    }
    //    else if(selection.Selected == "All Copy")
    //    {
    //        property.objectReferenceValue = AdvancedScriptableObjectUtility.MergeAll(
    //            (AdvancedScriptableObject)_property.serializedObject.targetObject,
    //            (AdvancedScriptableObject)property.serializedObject.targetObject,
    //            (AdvancedScriptableObject)property.objectReferenceValue,
    //            true);

    //        property.serializedObject.ApplyModifiedProperties();
    //    }
    //}

    //void Delete(int index)
    //{
    //    SerializedProperty property = _property.GetArrayElementAtIndex(index);
    //    AdvancedScriptableObjectUtility.Delete(
    //        (AdvancedScriptableObject)_property.serializedObject.targetObject,
    //        (AdvancedScriptableObject)property.objectReferenceValue);
    //    property.objectReferenceValue = null;
    //    property.serializedObject.ApplyModifiedProperties();
    //}


    //void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
    //{
    //    var element = _list.serializedProperty.GetArrayElementAtIndex(index);
    //    float widthUnit = rect.width / 10;
    //    EditorGUI.LabelField(new Rect(rect.x, rect.y, widthUnit, EditorGUIUtility.singleLineHeight), index.ToString());

    //    if (element.objectReferenceValue != null)
    //    {
    //        if (!BIsMerged(index))
    //        {
    //            GUI.color = Color.green;
    //            if (GUI.Button(new Rect(rect.x + (widthUnit), rect.y, widthUnit, EditorGUIUtility.singleLineHeight), "><"))
    //                MergeMenu(index);
    //            GUI.color = Color.white;
    //            if (GUI.Button(new Rect(rect.x + (widthUnit * 2), rect.y, widthUnit, EditorGUIUtility.singleLineHeight), "Null"))
    //                NullElement(index);
    //            GUI.enabled = false;
    //            EditorGUI.PropertyField(new Rect(rect.x + (widthUnit * 3), rect.y, widthUnit * 7, EditorGUIUtility.singleLineHeight), element, GUIContent.none, true);
    //            GUI.enabled = true;
    //        }
    //        else
    //        {
    //            GUI.color = Color.cyan;
    //            if (GUI.Button(new Rect(rect.x + (widthUnit), rect.y, widthUnit, EditorGUIUtility.singleLineHeight), "<>"))
    //                UnmergeMenu(index);
    //            GUI.color = Color.red;
    //            if (GUI.Button(new Rect(rect.x + (widthUnit * 2), rect.y, widthUnit, EditorGUIUtility.singleLineHeight), "X"))
    //                Delete(index);
    //            GUI.color = Color.white;
    //            if (GUI.Button(new Rect(rect.x + (widthUnit * 3), rect.y, widthUnit * 6, EditorGUIUtility.singleLineHeight), element.objectReferenceValue.name))
    //                Selection.activeObject = element.objectReferenceValue;
    //            if (GUI.Button(new Rect(rect.x + (widthUnit * 9), rect.y, widthUnit, EditorGUIUtility.singleLineHeight), "C"))
    //                AdvancedScriptableObjectUtility.CopyASO(element);
    //        }
    //    }
    //    else
    //    {
    //        if (GUI.Button(new Rect(rect.x + (widthUnit), rect.y, widthUnit * 2, EditorGUIUtility.singleLineHeight), "Create"))
    //            CreateNew(index);

    //        EditorGUI.BeginChangeCheck();
    //        EditorGUI.PropertyField(new Rect(rect.x + (widthUnit * 3), rect.y, widthUnit * 6, EditorGUIUtility.singleLineHeight), element, GUIContent.none, true);
    //        if (EditorGUI.EndChangeCheck())
    //        {
    //            AdvancedScriptableObject aso = (AdvancedScriptableObject)element.objectReferenceValue;
    //            //aso.ReferencingASOs.Add(new ASOReferenceInfo((AdvancedScriptableObject)_property.serializedObject.targetObject, _property.name, index));
    //            ASOManager.Me.AddReferenceData((AdvancedScriptableObject)_property.serializedObject.targetObject,aso, _property.name, index);
    //        }

    //        if (AdvancedScriptableObjectUtility.BCanPaste(element,true))
    //        {
    //            if (GUI.Button(new Rect(rect.x + (widthUnit * 9), rect.y, widthUnit, EditorGUIUtility.singleLineHeight), "P"))
    //            {
    //                AdvancedScriptableObjectUtility.PasteASO(element);
    //            }
    //        }
    //        else
    //        {
    //            GUI.color = Color.grey;
    //            GUI.Button(new Rect(rect.x + (widthUnit * 9), rect.y, widthUnit, EditorGUIUtility.singleLineHeight), "P");
    //            GUI.color = Color.white;
    //        }
    //    }
    //}

    //bool BIsMerged(int index)
    //{
    //    return AssetDatabase.GetAssetPath(_property.serializedObject.targetObject) == AssetDatabase.GetAssetPath(_property.GetArrayElementAtIndex(index).objectReferenceValue);
    //}

}
