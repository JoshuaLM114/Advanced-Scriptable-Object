using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Used to catch duplications. 
/// </summary>
public class ASOPostProcessor : AssetPostprocessor {

    static void OnPostprocessAllAssets(string[] importedAssets,
        string[] deletedAssets,string[]movedAssets,string[] movedFromAssetPaths)
    {
        foreach (string str in importedAssets)
        {
            Object obj = AssetDatabase.LoadAssetAtPath(str, typeof(Object));
            if (obj != null)
            {
                if (obj.GetType().IsSubclassOf(typeof(AdvancedScriptableObject)))
                {
                    //Check if there are existing references if so then no need to update.
                    AdvancedScriptableObject aso = (AdvancedScriptableObject)obj;
                    if (ASOManager.Me.GetReferencesOn(aso).Length == 0)
                        AdvancedScriptableObjectUtility.AddReferences(aso);
                }
            }
        }
    }

}
