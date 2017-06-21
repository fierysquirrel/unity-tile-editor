using System.Collections.Generic;
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
    /// This class represents an object in the editor.
    /// These are prefabs that the user customize, can be added, moved or deleted from the editor.
    /// </summary>
    public class EditorObject
    {
        public int i;                           //i from (i,j) in the grid
        public int j;                           //j from (i,j) in the grid
        public GameObject reference;            //Game object reference
        public Layer layer;                     //The layer that this object belongs to
        private List<GameObject> tileBoundings; //Boundings (in development)

        public EditorObject(GameObject reference, int i, int j, Layer layer)
        {
            this.reference = reference;
            this.i = i;
            this.j = j;
            this.layer = layer;
            tileBoundings = new List<GameObject>();
        }

        /// <summary>
        /// Verify if the coordinates (i,j) are the same of the object.
        /// </summary>
        /// <param name="i">i parameter in (i,j)</param>
        /// <param name="j">j parameter in (i,j)</param>
        /// <returns></returns>
        public bool CompareIndexes(int i, int j)
        {
            return this.i == i && this.j == j;
        }

        /// <summary>
        /// Verify if the reference is the same that the object contains
        /// </summary>
        /// <param name="reference">A reference to the game object</param>
        /// <returns></returns>
        public bool CompareReference(GameObject reference)
        {
            return this.reference == reference;
        }

        /// <summary>
        /// Include a new bounding (this feature is in development)
        /// </summary>
        /// <param name="bounding">A game object with a bounding box</param>
        public void AddTileBounding(GameObject bounding)
        {
            tileBoundings.Add(bounding);
        }

        /// <summary>
        /// Remove a bounding reference (this feature is in development)
        /// </summary>
        /// <param name="bounding">A game object with a bounding box</param>
        public void RemoveTileBounding(GameObject bounding)
        {
            tileBoundings.Remove(bounding);
        }

        /// <summary>
        /// Highlights which boundings does this element make reference to in the Hierarchy.
        /// This was created to make easier finding boundings in the Hierarchy
        /// </summary>
        public void HighlightTileBoundings()
        {
            foreach(GameObject o in tileBoundings)
                EditorGUIUtility.PingObject(o);
        }
    }
}
