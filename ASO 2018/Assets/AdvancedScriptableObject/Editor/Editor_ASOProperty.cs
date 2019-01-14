using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System;

[CustomPropertyDrawer(typeof(ASOAttribute),true)]
public class Editor_ASOProperty : PropertyDrawer {

    AdvancedScriptableObject propertyASO;
    AdvancedScriptableObject parentASO;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float widthUnit = position.width / 10;
        EditorGUI.LabelField(new Rect(position.x, position.y, widthUnit * 2, EditorGUIUtility.singleLineHeight), property.displayName);

        parentASO = (AdvancedScriptableObject)property.serializedObject.targetObject;

        if (property.objectReferenceValue != null)
        {
            propertyASO = (AdvancedScriptableObject)property.objectReferenceValue;
            

            if (!BIsMerged(property))
            {
                GUI.color = Color.green;
                if (GUI.Button(new Rect(position.x + (widthUnit * 3), position.y, widthUnit, EditorGUIUtility.singleLineHeight), "><"))
                    MergeMenu(property);
                GUI.color = Color.white;
                if (GUI.Button(new Rect(position.x + (widthUnit * 4), position.y, widthUnit, EditorGUIUtility.singleLineHeight), "Null"))
                    NullASOProperty(property);
                GUI.color = Color.white;
                //Disabled for control over reference handling. 
                GUI.enabled = false;
                EditorGUI.PropertyField(new Rect(position.x + (widthUnit * 5), position.y, widthUnit * 5, EditorGUIUtility.singleLineHeight), property, GUIContent.none, true);
                GUI.enabled = true;
            }
            else
            {
                GUI.color = Color.cyan;
                if (GUI.Button(new Rect(position.x + (widthUnit * 3), position.y, widthUnit, EditorGUIUtility.singleLineHeight), "<>"))
                    UnmergeMenu(property);
                GUI.color = Color.red;
                if (GUI.Button(new Rect(position.x + (widthUnit * 4), position.y, widthUnit, EditorGUIUtility.singleLineHeight), "X"))
                    Delete(property);
                GUI.color = Color.white;
                if (GUI.Button(new Rect(position.x + (widthUnit * 5), position.y, widthUnit * 4, EditorGUIUtility.singleLineHeight), property.objectReferenceValue.name))
                    EnterChildASO(property);
                if (GUI.Button(new Rect(position.x + (widthUnit * 9), position.y, widthUnit, EditorGUIUtility.singleLineHeight), "C"))
                    AdvancedScriptableObjectUtility.CopyASO(property);


            }
        }
        else
        {
            if (GUI.Button(new Rect(position.x + (widthUnit * 2), position.y, widthUnit * 2, EditorGUIUtility.singleLineHeight), "Create"))
                CreateNew(property);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(position.x + (widthUnit * 4), position.y, widthUnit * 5, EditorGUIUtility.singleLineHeight), property, GUIContent.none, true);
            if (EditorGUI.EndChangeCheck())
            {
                if (AdvancedScriptableObjectUtility.BWillCreateCircleReference(parentASO, propertyASO))
                {
                    AlterProperty(property, null);
                    Debug.LogError("Object reference will create circular reference. These are not supported.");
                }
                else
                {
                    AlterProperty(property,(AdvancedScriptableObject)property.objectReferenceValue);
                    AddReferenceData(property);
                }
            }

            if (AdvancedScriptableObjectUtility.BCanPaste(property))
            {
                if (GUI.Button(new Rect(position.x + (widthUnit * 9), position.y, widthUnit, EditorGUIUtility.singleLineHeight), "P"))
                {
                    AdvancedScriptableObjectUtility.PasteASO(property);
                }
            }
            else
            {
                GUI.color = Color.grey;
                GUI.Button(new Rect(position.x + (widthUnit * 9), position.y, widthUnit, EditorGUIUtility.singleLineHeight), "P");
                GUI.color = Color.white;
            }
        }


        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label);
    }

    void EnterChildASO(SerializedProperty property)
    {
        EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(property.objectReferenceValue)));
        Selection.activeObject = property.objectReferenceValue;
    }

    void AlterProperty(SerializedProperty property,AdvancedScriptableObject deltaASO)
    {
        propertyASO = deltaASO;
        property.objectReferenceValue = deltaASO;
        property.serializedObject.ApplyModifiedProperties();
    }

    public struct ObjectSelection
    {
        private SerializedProperty _property;
        public SerializedProperty Property
        {
            get { return _property; }
        }
        private string _selected;
        public string Selected
        {
            get { return _selected; }
        }

        public ObjectSelection(SerializedProperty property, string selected)
        {
            _property = property;
            _selected = selected;
        }
    }

    bool BIsMerged(SerializedProperty property)
    {
        return AssetDatabase.GetAssetPath(property.objectReferenceValue) == AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
    }

    void UnmergeMenu(SerializedProperty property)
    {
        var menu = new GenericMenu();
        string[] options = new string[] { "Current", "All" };
        for (int i = 0; i < options.Length; i++)
        {
            GUIContent gc = new GUIContent(options[i]);
            menu.AddItem(gc, false, UnmergeTypeSelected, new ObjectSelection(property, options[i]));
        }
        menu.ShowAsContext();
    }

    void UnmergeTypeSelected(object userData)
    {
        ObjectSelection selection = (ObjectSelection)userData;
        if (selection.Selected == "Current")
        {
            AlterProperty(selection.Property,AdvancedScriptableObjectUtility.UnmergeCurrent(
                parentASO,
                propertyASO));
            
            AddReferenceData(selection.Property);
        }
        else if (selection.Selected == "All")
        {
            //Need to first remove the object from serialized object
            AlterProperty(selection.Property,AdvancedScriptableObjectUtility.UnmergeAll(
                parentASO,
                propertyASO));
            AddReferenceData(selection.Property);

        }
    }

    void AddReferenceData(SerializedProperty property)
    { 
        bool bIsArrayElement;
        if (property.propertyPath.Contains("["))
            bIsArrayElement = true;
        else
            bIsArrayElement = false;

        if (!bIsArrayElement)
        {
            ASOManager.Me.AddReferenceData(parentASO, propertyASO, property.name);
        }
        else
        {
            FieldInfo field;
            System.Type parentType = parentASO.GetType();
            string arrayName = AdvancedScriptableObjectUtility.GetArrayName(property);
            string arrayIndex = "";
            field = parentType.GetField(arrayName);
            if (field != null)
            {
                var arrayElements = field.GetValue(parentASO) as Array;
                for (int i = 0; i < arrayElements.Length; i++)
                {
                    if((AdvancedScriptableObject)arrayElements.GetValue(i) == propertyASO)
                    {
                        arrayIndex = i.ToString();
                        break;
                    }
                }
            }
            ASOManager.Me.AddReferenceData(parentASO, propertyASO, string.Format("{0}[{1}]",arrayName,arrayIndex));
        }      
    }

    void MergeMenu(SerializedProperty property)
    {
        var menu = new GenericMenu();
        string[] options = new string[] { "Current", "All", "Current Copy", "All Copy" };
        for (int i = 0; i < options.Length; i++)
        {
            GUIContent gc = new GUIContent(options[i]);
            menu.AddItem(gc, false, MergeTypeSelected, new ObjectSelection(property, options[i]));
        }
        menu.ShowAsContext();
    }

    void MergeTypeSelected(object userData)
    {
        ObjectSelection selection = (ObjectSelection)userData;

        ASOReferenceData[] toMergeReferences = ASOManager.Me.GetReferencesOn(propertyASO);
        if (selection.Selected == "Current")
        {

            if (toMergeReferences.Length > 1)
            {
                if (!EditorUtility.DisplayDialog(
                    "Object is referenced",
                    string.Format("Merging this object will lose references on {0} objects. Continue?", toMergeReferences.Length - 1),
                    "Confirm",
                    "Cancel"))
                    return;
            }

            AlterProperty(selection.Property,AdvancedScriptableObjectUtility.MergeCurrent(
                parentASO,
                propertyASO));
        }
        else if (selection.Selected == "All")
        {

            if (toMergeReferences.Length > 1)
            {
                if (!EditorUtility.DisplayDialog(
                    "Object is referenced",
                    string.Format("Merging this object will lose references on multiple objects. Continue?"),
                    "Confirm",
                    "Cancel"))
                    return;
            }

            AlterProperty(selection.Property,AdvancedScriptableObjectUtility.MergeAll(
                parentASO,
                parentASO,
                propertyASO));
        }
        else if (selection.Selected == "Current Copy")
        {
            AlterProperty(selection.Property, AdvancedScriptableObjectUtility.MergeCurrent(
                parentASO,
                propertyASO,
                true));
        }
        else if (selection.Selected == "All Copy")
        {
            AlterProperty(selection.Property, AdvancedScriptableObjectUtility.MergeAll(
                parentASO,
                parentASO,
                propertyASO,
                true));
        }
    }

    void Delete(SerializedProperty property)
    {
        AdvancedScriptableObjectUtility.Delete(parentASO, propertyASO);
        AlterProperty(property, null);
        property.serializedObject.UpdateIfRequiredOrScript();
    }

    void NullASOProperty(SerializedProperty property)
    {
        ASOManager.Me.RemoveReference(parentASO, propertyASO);
        AlterProperty(property, null);
    }

    void CreateNew(SerializedProperty property)
    {
        System.Type type = AdvancedScriptableObjectUtility.GetSerializedPropertyType(property);
        System.Type[] types = Assembly.GetAssembly(type).GetTypes();

        string[] allTypes = (from System.Type t in types where t.IsSubclassOf(type) select type.FullName).ToArray();
        if (!type.IsAbstract)
        {
            List<string> list = allTypes.ToList();
            list.Add(type.FullName);
            allTypes = list.ToArray();
        }


        var menu = new GenericMenu();

        for (int i = 0; i < allTypes.Length; i++)
        {
            GUIContent gc = new GUIContent(allTypes[i]);
            menu.AddItem(gc, false, CreateObjectSelected, new ObjectSelection(property, allTypes[i]));
        }
        menu.ShowAsContext();
    }

    void CreateObjectSelected(object userdata)
    {
        if (userdata != null)
        {
            ObjectSelection objSel = (ObjectSelection)userdata;
            AdvancedScriptableObject newObj = ScriptableObject.CreateInstance(objSel.Selected) as AdvancedScriptableObject;

            if (newObj == null)
                Debug.Log("New obj is null");

            newObj.name = objSel.Selected;
            newObj.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(newObj, parentASO);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newObj));

            AdvancedScriptableObject newASO = (AdvancedScriptableObject)newObj;
            SerializedObject sObjAso = new SerializedObject(newASO);
            sObjAso.FindProperty("_parentAsset").objectReferenceValue = parentASO;
            sObjAso.ApplyModifiedProperties();

            if (objSel.Property == null)
                Debug.Log("Property is null");
            AlterProperty(objSel.Property, newObj);

            EditorUtility.SetDirty(objSel.Property.serializedObject.targetObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }


}
