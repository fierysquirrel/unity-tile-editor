using UnityEditor;
using UnityEngine;

/// <summary>
/// Created by Henry Fernández
/// Fiery Squirrel (http://fierysquirrel.com/)
/// May, 2017
/// Version 0.9.0
/// </summary>
namespace Assets.Editor
{
    /// <summary>
    /// Helper Functions
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Add a new tag to the tag manager
        /// </summary>
        /// <param name="newTag">The new tag</param>
        public static void AddNewTag(string newTag)
        {
            // Open tag manager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            // First check if it is not already present
            bool found;

            found = TagExists(newTag);

            // if not found, add it
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                SerializedProperty n = tagsProp.GetArrayElementAtIndex(0);
                n.stringValue = newTag;
            }

            // and to save the changes
            tagManager.ApplyModifiedProperties();
        }

        /// <summary>
        /// Add a new layer to the tag manager
        /// </summary>
        /// <param name="newLayer">The new layer</param>
        public static void AddNewLayer(string newLayer)
        {
            // Open tag manager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("layers");
            // First check if it is not already present
            bool found;

            found = LayerExists(newLayer);

            // if not found, add it
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                SerializedProperty n = tagsProp.GetArrayElementAtIndex(10);
                n.stringValue = newLayer;
            }

            // and to save the changes
            tagManager.ApplyModifiedProperties();
        }

        /// <summary>
        /// Check of the tag exists
        /// </summary>
        /// <param name="tag">The tag</param>
        /// <returns>True if exists, False if does not</returns>
        public static bool TagExists(string tag)
        {
            return PropertyExists("tags", tag);
        }

        /// <summary>
        /// Check of the layer exists
        /// </summary>
        /// <param name="layer">The layer</param>
        /// <returns>True if exists, False if does not</returns>
        public static bool LayerExists(string layer)
        {
            return PropertyExists("layers", layer);
        }

        /// <summary>
        /// Get the layer Index using its name
        /// </summary>
        /// <param name="layer">Layer's name</param>
        /// <returns>Layer's index</returns>
        public static int GetLayerIndex(string layer)
        {
            // Open tag manager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            int index;

            index = -1;
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                SerializedProperty t = layersProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(layer))
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// A general function to checl of a property exists
        /// </summary>
        /// <param name="propertyTag">Could be "layers" or "tags"</param>
        /// <param name="property">The name of the property</param>
        /// <returns>True if exists, False if does not</returns>
        private static bool PropertyExists(string propertyTag,string property)
        {
            // Open tag manager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty(propertyTag);
            
            // First check if it is not already present
            bool found = false;
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                SerializedProperty t = layersProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(property))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Take a vector in the World's coordinates and transforms it to grid coordinates
        /// </summary>
        /// <param name="vector">Should be a World coordinates vector</param>
        /// <param name="gridX">i in (i,j)</param>
        /// <param name="gridY">j in (i,j)</param>
        /// <returns></returns>
        public static Vector2 ConvertVectorToGrid(Vector2 vector, float gridX, float gridY)
        {
            Vector2 transformedVec;
            float vecI, vecJ;

            vecI = Mathf.FloorToInt(vector.x / gridX);
            vecJ = Mathf.FloorToInt(vector.y / gridY);

            transformedVec = new Vector2(vecI, vecJ);


            return transformedVec;
        }
    }
}
