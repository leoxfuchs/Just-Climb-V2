using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class TagCreator
{
    static TagCreator()
    {
        CreateTag("Ledge");
    }
    
    static void CreateTag(string tagName)
    {
        UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if ((asset != null) && (asset.Length > 0))
        {
            SerializedObject serializedObject = new SerializedObject(asset[0]);
            SerializedProperty tags = serializedObject.FindProperty("tags");
            
            bool found = false;
            for (int i = 0; i < tags.arraySize; i++)
            {
                SerializedProperty t = tags.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tagName))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                SerializedProperty newTag = tags.GetArrayElementAtIndex(tags.arraySize - 1);
                newTag.stringValue = tagName;
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                
                Debug.Log("Tag '" + tagName + "' has been added to the Tags list.");
            }
        }
    }
}