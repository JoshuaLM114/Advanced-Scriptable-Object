using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

public static class AdvancedScriptableObjectUtility {

    #region MergingToolsPropertyLevel
    public static AdvancedScriptableObject UnmergeCurrent(AdvancedScriptableObject unmergeFrom, AdvancedScriptableObject toUnmerge)
    {
        //Copy
        var toUnmergeCopy = ScriptableObject.Instantiate(toUnmerge);
        toUnmergeCopy.name = toUnmerge.name;

        //create the Object at the same file location but not added to object
        string fileLoc = AssetDatabase.GetAssetPath(unmergeFrom);
        string fileName = Path.GetFileName(fileLoc);
        string folderLoc = fileLoc.Remove(fileLoc.Length - fileName.Length);
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(folderLoc + toUnmergeCopy.name + ".asset");
        AssetDatabase.CreateAsset(toUnmergeCopy, uniquePath);

        //Examines the object and retrieves properties
        var properties = toUnmergeCopy.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        #region Iterate Through Properties
        //Iterates through it's properties
        for (int i = 0; i < properties.Length; i++)
        {
            var curProp = properties[i];
            //Debug.Log(curProp.Name);

            if (curProp.Name == "_parentAsset")
            {
                //Debug.Log("Setting Parent");
                curProp.SetValue(toUnmergeCopy, null);
            }

            if (curProp.FieldType.IsArray && curProp.FieldType.GetElementType().IsSubclassOf(typeof(AdvancedScriptableObject)))
            {
                #region Handle Arrays
                Array array = curProp.GetValue(toUnmerge) as Array;
                Array newArray = curProp.GetValue(toUnmergeCopy) as Array;
                for (int x = 0; x < array.Length; x++)
                {
                    AdvancedScriptableObject arrayVal = (AdvancedScriptableObject)array.GetValue(x);
                    if (arrayVal != null)
                    {
                        if (BShareAsset(unmergeFrom, arrayVal))
                            newArray.SetValue(MergeCurrent(toUnmergeCopy,UnmergeCurrent(unmergeFrom,arrayVal)), x);
                        else
                        {
                            //set reference
                            newArray.SetValue(arrayVal, x);
                            ASOManager.Me.AddReferenceData(toUnmergeCopy, arrayVal, curProp.Name, x);
                        }
                    }
                }
                curProp.SetValue(toUnmergeCopy, newArray);
                #endregion
            }
            else
            {
                //Skips over irrelevant types
                if (!BRelevantType(curProp))
                    continue;

                //Ensures value is not null
                var val = (AdvancedScriptableObject)curProp.GetValue(toUnmerge);
                if (val != null)
                {
                    if (BShareAsset(toUnmerge, val))
                    {
                        //Remerge
                        curProp.SetValue(toUnmergeCopy, MergeCurrent(toUnmergeCopy, UnmergeCurrent(unmergeFrom, val)));
                    }
                    else
                    {
                        //Set reference
                        curProp.SetValue(toUnmergeCopy, val);
                        ASOManager.Me.AddReferenceData(toUnmergeCopy, val, curProp.Name);
                    }
                }
            }
        }
        #endregion
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //Delete original
        Delete(unmergeFrom, toUnmerge);

        return toUnmergeCopy;
    }

    public static AdvancedScriptableObject MergeCurrent(AdvancedScriptableObject toMergeWith, AdvancedScriptableObject toMerge,bool bInstanceMerging = false)
    {
        //Copies the object
        var toMergeCopy = ScriptableObject.Instantiate(toMerge);
        toMergeCopy.name = toMerge.name;
        toMergeCopy.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(toMergeCopy, toMergeWith);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(toMergeCopy));

        //Examines the object and retrieves properties
        var properties = toMerge.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        #region Iterate Through Properties
        //Iterates through it's properties
        for (int i = 0; i < properties.Length; i++)
        {
            var curProp = properties[i];
            //Debug.Log(curProp.Name);
            if (curProp.Name == "_parentAsset")
            {
                //Debug.Log("Setting Parent");
                curProp.SetValue(toMergeCopy, toMergeWith);
            }

            if (curProp.FieldType.IsArray && curProp.FieldType.GetElementType().IsSubclassOf(typeof(AdvancedScriptableObject)))
            {
                #region Handle Arrays
                Array array = curProp.GetValue(toMerge) as Array;
                Array newArray = curProp.GetValue(toMergeCopy) as Array;
                for (int x = 0; x < array.Length; x++)
                {
                    AdvancedScriptableObject arrayVal = (AdvancedScriptableObject)array.GetValue(x);
                    if (arrayVal != null)
                    {
                        if (BShareAsset(toMerge, arrayVal))
                        {
                            newArray.SetValue(MergeCurrent(toMergeCopy, arrayVal, bInstanceMerging), x);
                        }
                        else
                        {
                            newArray.SetValue(arrayVal, x);
                            ASOManager.Me.AddReferenceData(toMergeCopy, arrayVal, curProp.Name);
                        }
                    }
                }
                curProp.SetValue(toMergeCopy, newArray);
                #endregion
            }
            else
            {
                //Skips over irrelevant types
                if (!BRelevantType(curProp))
                    continue;

                //Ensures value is not null
                var val = (AdvancedScriptableObject)curProp.GetValue(toMerge);
                if (val != null)
                {
                    //Debug.Log(string.Format("Value at {0} was {1}", curProp.Name, val));
                    if (BShareAsset(val, toMerge))
                    {
                        //Remerge value along with children
                        curProp.SetValue(toMergeCopy, MergeCurrent(toMergeCopy,val,bInstanceMerging));
                    }
                    else
                    {
                        //Set reference
                        curProp.SetValue(toMergeCopy, val);
                        ASOManager.Me.AddReferenceData(toMergeCopy, val, curProp.Name);
                    }
                }
            }
        }
        #endregion

        if (!bInstanceMerging)
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(toMerge));
        else
            ASOManager.Me.RemoveReference(toMergeWith, toMerge);

        //update assetdatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return toMergeCopy;
    }

    public static AdvancedScriptableObject RemergeChildren(AdvancedScriptableObject toMerge)
    {
        //Examines the object and retrieves properties
        var properties = toMerge.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        #region Iterate Through Properties
        //Iterates through it's properties
        for (int i = 0; i < properties.Length; i++)
        {
            var curProp = properties[i];
            //Debug.Log(curProp.Name);

            if (curProp.FieldType.IsArray && curProp.FieldType.GetElementType().IsSubclassOf(typeof(AdvancedScriptableObject)))
            {
                #region Handle Arrays
                Array array = curProp.GetValue(toMerge) as Array;
                for (int x = 0; x < array.Length; x++)
                {
                    AdvancedScriptableObject arrayVal = (AdvancedScriptableObject)array.GetValue(x);
                    if (arrayVal != null)
                    {
                        if(BShareAsset(toMerge,arrayVal))
                            array.SetValue(MergeAll(toMerge, toMerge, arrayVal), x);
                        else
                        {
                            //set reference
                            array.SetValue(arrayVal, x);
                            ASOManager.Me.AddReferenceData(toMerge, arrayVal, curProp.Name, x);
                        }
                    }
                }
                curProp.SetValue(toMerge, array);
                #endregion
            }
            else
            {
                //Skips over irrelevant types
                if (!BRelevantType(curProp))
                {
                    continue;
                }

                //Ensures value is not null
                var val = (AdvancedScriptableObject)curProp.GetValue(toMerge);
                if (val != null)
                {
                    ////Then remerge to the copy
                    if(BShareAsset(toMerge,val))
                        curProp.SetValue(toMerge, MergeAll(toMerge, toMerge, val));
                    else
                    {
                        //Set reference
                        curProp.SetValue(toMerge, val);
                        ASOManager.Me.AddReferenceData(toMerge, val, curProp.Name);
                    }
                }
            }
        }
        #endregion
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return toMerge;
    }

    public static AdvancedScriptableObject UnmergeAll(AdvancedScriptableObject unmergeFrom, AdvancedScriptableObject toUnmerge)
    {
        //Copies the object
        var toUnmergeCopy = ScriptableObject.Instantiate(toUnmerge);
        toUnmergeCopy.name = toUnmerge.name;

        //create the Object at the same file location but not added to object
        string fileLoc = AssetDatabase.GetAssetPath(unmergeFrom);
        string fileName = Path.GetFileName(fileLoc);
        string folderLoc = fileLoc.Remove(fileLoc.Length - fileName.Length);
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(folderLoc + toUnmergeCopy.name + ".asset");
        AssetDatabase.CreateAsset(toUnmergeCopy, uniquePath);

        //Examines the object and retrieves properties
        var properties = toUnmerge.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        #region Iterate Through Properties
        //Iterates through it's properties
        for (int i = 0; i < properties.Length; i++)
        {
            var curProp = properties[i];
            //Debug.Log(curProp.Name);
            if (curProp.Name == "_parentAsset")
            {
                //Debug.Log("Setting Parent");
                curProp.SetValue(toUnmergeCopy, null);
            }

            if (curProp.FieldType.IsArray && curProp.FieldType.GetElementType().IsSubclassOf(typeof(AdvancedScriptableObject)))
            {
                #region Handle Arrays
                Array array = curProp.GetValue(toUnmerge) as Array;
                Array newArray = curProp.GetValue(toUnmergeCopy) as Array;
                for (int x = 0; x < array.Length; x++)
                {
                    AdvancedScriptableObject arrayVal = (AdvancedScriptableObject)array.GetValue(x);
                    if (arrayVal != null)
                    {
                        if (BShareAsset(unmergeFrom, arrayVal))
                        {
                            AdvancedScriptableObject unmergedPropVal = UnmergeAll(unmergeFrom, arrayVal);
                            newArray.SetValue(unmergedPropVal,x);
                            //Set up reference cachedata
                            ASOManager.Me.AddReferenceData(toUnmergeCopy, unmergedPropVal, curProp.Name, x);
                        }
                    }
                }
                curProp.SetValue(toUnmergeCopy, newArray);
                #endregion
            }
            else
            {
                //Skips over irrelevant types
                if (!BRelevantType(curProp))
                    continue;

                //Ensures value is not null
                var val = (AdvancedScriptableObject)curProp.GetValue(toUnmerge);

                if (val != null)
                {
                    if (BShareAsset(unmergeFrom, val))
                    {
                        //Remerge value along with children
                        AdvancedScriptableObject unmergedPropVal = UnmergeAll(unmergeFrom, val);
                        curProp.SetValue(toUnmergeCopy, unmergedPropVal);
                        ASOManager.Me.AddReferenceData(toUnmergeCopy, unmergedPropVal, curProp.Name);
                    }
                }
            }          
        }
        #endregion
        MonoBehaviour.DestroyImmediate(toUnmerge, true);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(toUnmergeCopy));

        //update assetdatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return toUnmergeCopy;
    }

    public static AdvancedScriptableObject MergeAll(AdvancedScriptableObject assetToMergeWith,AdvancedScriptableObject referencingObj,
        AdvancedScriptableObject toMerge,bool bInstanceMerging = false)
    {
        //Copies the object
        var toMergeCopy = MonoBehaviour.Instantiate(toMerge);
        toMergeCopy.name = toMerge.name;
        toMergeCopy.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(toMergeCopy, assetToMergeWith);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(toMergeCopy));

        //Examines the object and retrieves properties
        var properties = toMerge.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        
        #region Iterate Through Properties
        //Iterates through it's properties
        for (int i = 0; i < properties.Length; i++)
        {
            var curProp = properties[i];
            //Debug.Log(curProp.Name);
            if (curProp.Name == "_parentAsset")
            {
                //Debug.Log("Setting Parent");
                curProp.SetValue(toMergeCopy, referencingObj);
            }

            if (curProp.FieldType.IsArray && curProp.FieldType.GetElementType().IsSubclassOf(typeof(AdvancedScriptableObject)))
            {
                #region Handle Arrays
                Array array = curProp.GetValue(toMerge) as Array;
                Array newArray = curProp.GetValue(toMergeCopy) as Array;
                for (int x = 0; x < array.Length; x++)
                {
                    if (array.GetValue(x) != null)
                    {
                        if (!BShareAsset(assetToMergeWith, (AdvancedScriptableObject)array.GetValue(x)))
                            newArray.SetValue(MergeAll(assetToMergeWith,toMergeCopy,(AdvancedScriptableObject)array.GetValue(x),bInstanceMerging),x);
                    }
                }
                curProp.SetValue(toMergeCopy, newArray);
                #endregion
            }
            else
            {
                //Skips over irrelevant types
                if (!BRelevantType(curProp))
                    continue;

                //Ensures value is not null
                var val = (AdvancedScriptableObject)curProp.GetValue(toMerge);
                if (val != null)
                {
                    if (!BShareAsset(assetToMergeWith, val))
                        curProp.SetValue(toMergeCopy, MergeAll(assetToMergeWith, toMergeCopy, val,bInstanceMerging));
                }
            }
        }
        #endregion

        if (!bInstanceMerging)
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(toMerge));          
        else
            ASOManager.Me.RemoveReference(assetToMergeWith, toMerge);

        //update assetdatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return toMergeCopy;
    }

    #endregion

    static bool BRelevantType(FieldInfo field)
    {
        if (!field.FieldType.IsSubclassOf(typeof(AdvancedScriptableObject)))
            return false;
        else
            return true;
    }

    public static void Delete(AdvancedScriptableObject deleteFrom, AdvancedScriptableObject objProperty)
    {
        //Examines the object and retrieves properties
        var properties = objProperty.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        #region Iterate Through Properties
        //Iterates through it's properties
        for (int i = 0; i < properties.Length; i++)
        {
            var curProp = properties[i];

            #region ArrayTypes
            if (curProp.FieldType.IsArray)
            {
                Array list = curProp.GetValue(objProperty) as Array;
                //Debug.Log("Array found");
                if (list.Length > 0)
                {
                    Type elType = curProp.GetValue(objProperty).GetType().GetElementType();

                    if (elType.IsSubclassOf(typeof(AdvancedScriptableObject)))
                    {
                        AdvancedScriptableObject[] newList = list as AdvancedScriptableObject[];
                        //Delete all elements
                        for (int x = 0; x < list.Length; x++)
                        {
                            AdvancedScriptableObject element = (AdvancedScriptableObject)newList.GetValue(x);
                            if (element != null &&
                                BShareAsset(deleteFrom, element))
                            {
                                Delete(deleteFrom, element);
                                MonoBehaviour.DestroyImmediate(element, true);
                            }
                        }
                        curProp.SetValue(objProperty, newList);
                    }

                }
            }
            #endregion
            if (!BRelevantType(curProp))
                continue;

            //Ensures value is not null
            var val = (AdvancedScriptableObject)curProp.GetValue(objProperty);
            if (val != null)
            {
                    if (BShareAsset(deleteFrom, val))
                        Delete(deleteFrom, val);
            }

        }
        #endregion
        MonoBehaviour.DestroyImmediate(objProperty, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static bool BShareAsset(AdvancedScriptableObject a, AdvancedScriptableObject b)
    {
        return AssetDatabase.GetAssetPath(a) == AssetDatabase.GetAssetPath(b);
    }

    #region PrototypingTools

    public static void CloneData(AdvancedScriptableObject destObj, AdvancedScriptableObject from, AdvancedScriptableObject to)
    {
        //Debug.Log("--------BEGIN COPYING----------");
        //Examines the object and retrieves properties
        var properties = from.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        //Iterates through it's properties setting any non merged SObj data
        for (int i = 0; i < properties.Length; i++)
        {
            var curProp = properties[i];

            if (BIsIgnoredField(curProp))
                continue;
            //Debug.Log(string.Format("CurProp:{0}", curProp.Name));

            if (curProp.FieldType.IsArray)
            {
                #region ArrayHandling
                Type elType = curProp.FieldType.GetElementType();

                //Debug.Log(string.Format("ArrayElementType:{0}", elType.Name));

                if (elType.IsSubclassOf(typeof(AdvancedScriptableObject)))
                {
                    #region ASO Array
                    Array fromList = curProp.GetValue(from) as Array;
                    Array toList = Array.CreateInstance(curProp.FieldType.GetElementType(), fromList.Length);

                    //Copy each element
                    for (int x = 0; x < fromList.Length; x++)
                    {
                        AdvancedScriptableObject element = (AdvancedScriptableObject)fromList.GetValue(x);

                        if (element != null)
                        {
                            //if element is merged to the asset it's being copied from.
                            if (BShareAsset(from, element))
                            {
                                var newObj = ScriptableObject.CreateInstance(element.GetType()) as AdvancedScriptableObject;

                                newObj.hideFlags = HideFlags.HideInHierarchy;
                                newObj.name = element.name;

                                SerializedObject serObj = new SerializedObject(newObj);
                                serObj.FindProperty("_parentAsset").objectReferenceValue = to;
                                serObj.ApplyModifiedProperties();

                                //Merge the object
                                AssetDatabase.AddObjectToAsset(newObj, destObj);
                                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newObj));

                                EditorUtility.SetDirty(destObj);
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();

                               
                                //copy the data from the other object
                                CloneData(destObj, element, newObj);
                                toList.SetValue(newObj, x);
                                new SerializedObject(to).ApplyModifiedProperties();
                                Debug.Log(string.Format("New ARray val:{0}",toList.GetValue(x)));
                            }
                            else
                            {
                                toList.SetValue(fromList.GetValue(x), x);
                                ASOManager.Me.AddReferenceData(to, element, curProp.Name, x);
                                new SerializedObject(to).ApplyModifiedProperties();
                            }
                        }
                    }
                    //Array newArray = Array.ConvertAll(toList, x => (elType)x);
                    curProp.SetValue(to, toList);

                    new SerializedObject(to).ApplyModifiedProperties();

                    EditorUtility.SetDirty(to);
                    EditorUtility.SetDirty(destObj);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    #endregion
                }
                else
                {
                    #region Other Arrays
                    
                    Array fromList = curProp.GetValue(from) as Array;
                    Array toList = Array.CreateInstance(curProp.FieldType.GetElementType(),fromList.Length);
                    Array.Copy(fromList, toList, fromList.Length);
                    curProp.SetValue(to, toList);
                    #endregion
                }
                #endregion

            }
            else
            {
                #region Standard Properties
                //Ensures value is not null
                var valFrom = curProp.GetValue(from);
                //Debug.Log(string.Format("Field:[{0}]  Value:[{1}]", curProp.Name,valFrom));
                if (valFrom != null)
                {
                    if (!valFrom.GetType().IsSubclassOf((typeof(AdvancedScriptableObject)))
                        && valFrom.GetType() != typeof(AdvancedScriptableObject))
                    {
                        //Debug.Log("Setting as reference");
                        curProp.SetValue(to, valFrom);
                    }
                    else
                    {
                        //If from var doesn't share asset with from object then set as reference
                        if (!BShareAsset(from, (AdvancedScriptableObject)valFrom))
                        {
                            curProp.SetValue(to, valFrom);
                            ASOManager.Me.AddReferenceData(to, (AdvancedScriptableObject)valFrom, curProp.Name);
                        }
                        else
                        {
                            //Create a new object                            
                            var asoFromVal = (AdvancedScriptableObject)valFrom;
                            Debug.Log(string.Format("Type of element:{0}", asoFromVal.GetType()));
                            var newObj = ScriptableObject.CreateInstance(asoFromVal.GetType()) as AdvancedScriptableObject;
                            newObj.hideFlags = HideFlags.HideInHierarchy;
                            newObj.name = asoFromVal.name;

                            SerializedObject serObj = new SerializedObject(newObj);
                            serObj.FindProperty("_parentAsset").objectReferenceValue = to;
                            serObj.ApplyModifiedProperties();
                            
                            //Merge the object
                            AssetDatabase.AddObjectToAsset(newObj, destObj);
                            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newObj));

                            curProp.SetValue(to, newObj);
                            EditorUtility.SetDirty(destObj);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            //copy the data from the other object
                            CloneData(destObj, (AdvancedScriptableObject)valFrom, newObj);
                        }
                    }
                }
                #endregion
            }

        }

        new SerializedObject(to).ApplyModifiedProperties();
        EditorUtility.SetDirty(to);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        //Debug.Log("--------COPYING FINISHED----------");
    }

    /// <summary>
    /// Removes and deletes any embedded subassets. used recursively
    /// </summary>
    /// <param name="aSObj"></param>
    public static void CleanObject(AdvancedScriptableObject aSObj)
    {
        //Debug.Log("--------BEGIN CLEANING----------");
        //Debug.Log(string.Format("Cleaning [{0}]",aSObj.name));
        //Examines the object and retrieves properties
        var properties = aSObj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        //Iterates through it's properties
        for (int i = 0; i < properties.Length; i++)
        {
            var curProp = properties[i];

            if (BIsIgnoredField(curProp))
                continue;

            if (curProp.FieldType.IsArray)
            {
                Array list = curProp.GetValue(aSObj) as Array;
                if (list != null && list.Length > 0)
                {
                    Type elType = curProp.GetValue(aSObj).GetType().GetElementType();

                    if (elType.IsSubclassOf(typeof(AdvancedScriptableObject)))
                    {
                        AdvancedScriptableObject[] newList = list as AdvancedScriptableObject[];
                        //Delete all elements
                        for (int x = 0; x < list.Length; x++)
                        {
                            AdvancedScriptableObject element = (AdvancedScriptableObject)newList.GetValue(x);
                            if (element != null &&
                                BShareAsset(aSObj, element))
                            {
                                Delete(aSObj, element);
                                MonoBehaviour.DestroyImmediate(element, true);
                            }
                            else
                            {
                                ASOManager.Me.RemoveReference(aSObj, element);
                            }
                            //Null cur index
                            newList.SetValue(null, x);
                        }
                        curProp.SetValue(aSObj, null);
                    }
                    else
                    {
                        curProp.SetValue(aSObj, null);
                    }
                }
            }
            else
            {
                //Skips over irrelevant types
                if (!BRelevantType(curProp))
                    continue;

                //Ensures value is not null
                var val = (AdvancedScriptableObject)curProp.GetValue(aSObj);
                if (val != null)
                {

                    if (BShareAsset(aSObj, val))
                    {
                        Delete(aSObj, val);
                        MonoBehaviour.DestroyImmediate(val, true);
                    }
                    else
                    {
                        ASOManager.Me.RemoveReference(aSObj, val);
                        curProp.SetValue(aSObj, null);
                    }
                }
            }
        }
        new SerializedObject(aSObj).ApplyModifiedProperties();
        EditorUtility.SetDirty(aSObj);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        new SerializedObject(aSObj).UpdateIfRequiredOrScript();
        EditorApplication.RepaintHierarchyWindow();
        //Debug.Log("--------CLEANING FINISHED----------");

    }

    public static void UpdateChildren(AdvancedScriptableObject source)
    {
        //Get children assets
        List<AdvancedScriptableObject> children = new List<AdvancedScriptableObject>();
        for (int i = 0; i < source.ProtoChildren.Count; i++)
        {
            AdvancedScriptableObject aso = source.ProtoChildren[i];

            if (aso != null)
                children.Add(aso);
            else
                Debug.LogWarning("Null child reference found!");
        }

        if (EditorUtility.DisplayDialog("Update Children", "Overwrite values of all children?", "Confirm", "Cancel"))
            for (int i = 0; i < children.Count; i++)
            {
                CloneData(children[i], source, children[i]);
            }
        
    }

    #endregion

    public static System.Type GetSerializedPropertyType(SerializedProperty prop)
    {
        Type parentType = prop.serializedObject.targetObject.GetType();
        bool bIsArrayElement;

        if (prop.propertyPath.Contains("["))
            bIsArrayElement = true;
        else
            bIsArrayElement = false;

        FieldInfo field;
        Type type = null;
        if (!bIsArrayElement)
        {
            field = prop.serializedObject.targetObject.GetType().GetField(prop.name);
            if(field != null)
                type = field.FieldType;
        }
        else
        {
            field = prop.serializedObject.targetObject.GetType().GetField(GetArrayName(prop));
            if(field != null)
                type = field.FieldType.GetElementType();
        }
        
        return type;
    }
    #region CutCopyPaste

    public static void CopyASO(SerializedProperty toCopy)
    {
        if (ASOManager.Me == null)
            Debug.Log("ASO Manager not found");
        else if (toCopy == null)
            Debug.Log("No property to copy");
        else if (toCopy.objectReferenceValue == null)
            Debug.Log("Property object reference is null");
        ASOManager.Me.CopyBuffer = (AdvancedScriptableObject)toCopy.objectReferenceValue;
    }

    public static void PasteASO(SerializedProperty pasteTo)
    {
        AdvancedScriptableObject newObj = ScriptableObject.Instantiate(ASOManager.Me.CopyBuffer);
        newObj.hideFlags = HideFlags.HideInHierarchy;
        newObj.name = ASOManager.Me.CopyBuffer.name;

        SerializedObject serObj = new SerializedObject(newObj);
        serObj.FindProperty("_parentAsset").objectReferenceValue = pasteTo.serializedObject.targetObject;
        serObj.ApplyModifiedProperties();

        //Merge the object
        AssetDatabase.AddObjectToAsset(newObj, pasteTo.serializedObject.targetObject);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newObj));
        pasteTo.objectReferenceValue = newObj;
        
        EditorUtility.SetDirty(pasteTo.serializedObject.targetObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CloneData((AdvancedScriptableObject)pasteTo.serializedObject.targetObject, ASOManager.Me.CopyBuffer, newObj);
    }

    public static bool BCanPaste(SerializedProperty toPasteTo)
    {
        Type parentType = toPasteTo.serializedObject.targetObject.GetType();
        bool bIsArrayElement;

        if (toPasteTo.propertyPath.Contains("["))
            bIsArrayElement = true;
        else
            bIsArrayElement = false;

        //return false;
        if (ASOManager.Me != null &&
            ASOManager.Me.CopyBuffer != null)
        {
            FieldInfo field;
            Type type;
            if (!bIsArrayElement)
            {
                field = parentType.GetField(toPasteTo.propertyPath);
                type = field.FieldType;
            }
            else
            {
                //Get the array field
                field = parentType.GetField(GetArrayName(toPasteTo));
                type = field.FieldType.GetElementType();
            }
            Type copyBufferType = ASOManager.Me.CopyBuffer.GetType();
            if (copyBufferType == type ||
                copyBufferType.IsSubclassOf(type))
                return true;
            else
                return false;
        }
        else
            return false;
    }

    #endregion

    /// <summary>
    /// Run through all properties recursively searching for aso references and
    /// adds the references
    /// </summary>
    /// <param name="toUpdate"></param>
    public static void AddReferences(AdvancedScriptableObject toUpdate)
    {
        //Debug.Log("-----------Add References Begin---------");
        //Debug.Log(string.Format("Adding references for {0}", toUpdate.name));

        //Examines the object and retrieves properties
        var properties = toUpdate.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        #region Iterate Through Properties
        //Iterates through it's properties
        for (int i = 0; i < properties.Length; i++)
        {
            var curProp = properties[i];
            //Debug.Log(curProp.Name);
            if (curProp.FieldType.IsArray && curProp.FieldType.GetElementType().IsSubclassOf(typeof(AdvancedScriptableObject)))
            {
                #region Handle Arrays
                Array array = curProp.GetValue(toUpdate) as Array;
                if(array != null && array.Length > 0)
                    for (int x = 0; x < array.Length; x++)
                    {
                        if (array.GetValue(x) != null)
                        {
                            AdvancedScriptableObject propASO = (AdvancedScriptableObject)array.GetValue(x);
                            if (!BShareAsset(toUpdate, propASO))
                            {
                                ASOManager.Me.AddReferenceData(toUpdate,propASO, curProp.Name,x);
                            }
                            else
                            {
                                //recursively enter to update child references.
                                AddReferences(propASO);
                            }
                        }
                    }
                #endregion
            }
            else
            {
                if (curProp.Name == "_protoParent")
                {
                    AdvancedScriptableObject parentASO = (AdvancedScriptableObject)curProp.GetValue(toUpdate);
                    if (parentASO != null)
                    {
                        if(!parentASO.ProtoChildren.Contains(toUpdate))
                            parentASO.ProtoChildren.Add(toUpdate);
                    }
                    continue;
                }

                //Skips over irrelevant types
                if (!BRelevantType(curProp))
                    continue;

                //Ensures value is not null
                var val = (UnityEngine.Object)curProp.GetValue(toUpdate);
                if (val != null)
                {
                    AdvancedScriptableObject propASO = (AdvancedScriptableObject)val;

                    if (!BShareAsset(toUpdate, propASO))
                    {
                        ASOManager.Me.AddReferenceData(toUpdate, propASO, curProp.Name);
                    }
                    else
                    {
                        AddReferences(propASO);
                    }
                }
            }
        }
        #endregion

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        //Debug.Log("---------Add References End---------");
    }

    public static string GetArrayName(SerializedProperty serProp)
    {
        string baseName = serProp.propertyPath;
        //Remove [i] index
        string trimmedName = baseName.Remove(baseName.Length - 3);
        trimmedName = trimmedName.Remove(trimmedName.Length - ".Array.data".Length);

        return trimmedName;
    }

    public static bool BWillCreateCircleReference(AdvancedScriptableObject mainAsset,AdvancedScriptableObject referenceAttempt)
    {       
        if (AssetDatabase.GetAssetPath(mainAsset) == AssetDatabase.GetAssetPath(referenceAttempt))
            return true;
        else
            return false;
    }

    static bool BIsIgnoredField(FieldInfo field)
    {

        if (field.Name == "_protoChildren" 
            || field.Name == "_protoParentGUID" ||
            field.Name == "_parentAsset")
            return true;
        else
            return false;
    }

}
