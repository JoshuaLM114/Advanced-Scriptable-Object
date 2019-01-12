using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;

[System.Serializable]
public class Editor_List : System.Object {

    private SerializedProperty _property;
    public SerializedProperty Property
    {
        get { return _property; }
    }

    private ReorderableList _list;

    public Editor_List(SerializedProperty property)
    {
        _property = property;
        SetupList();
    }

    void SetupList()
    {
        _list = new ReorderableList(_property.serializedObject, _property, true, true, true, true);
        _list.drawHeaderCallback = List_DrawHeader;
        _list.onCanRemoveCallback = List_BCanRemove;
        //_list.onRemoveCallback = List_ElementDeleted;
        _list.drawElementCallback = DrawElement;
    }

    void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = _list.serializedProperty.GetArrayElementAtIndex(index);
        float widthUnit = rect.width / 10;
        EditorGUI.LabelField(new Rect(rect.x, rect.y, widthUnit, EditorGUIUtility.singleLineHeight), index.ToString());

        EditorGUI.PropertyField(new Rect(rect.x + (widthUnit), rect.y, rect.width - (widthUnit), EditorGUIUtility.singleLineHeight), element, GUIContent.none, true);
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
        
    }

    public void DrawList()
    {
        _list.DoLayoutList();
    }
}
