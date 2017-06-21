using System.Collections.Generic;
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
    /// Each editor element is inserted into a layer in the Hierarchy, this represents the layer
    /// </summary>
    public class Layer
    {
        public const string LAYER_TAG = "FSLayer";      //A tag to identify all layers
        public const string DEFAULT_NAME = "Default";   //A default name (the user can change it in the editor)
        public string name;                             //Layer's name
        private string id;                              //Layer's id (automatically assigned)
        private List<EditorObject> sceneElements;       //Elements that belong to the layer
        private GameObject container;                   //The game object in the Hierarchy

        public Layer()
        {
            GameObject[] layers;

            if (!Helper.TagExists(LAYER_TAG))
                Helper.AddNewTag(LAYER_TAG);

            layers = GameObject.FindGameObjectsWithTag(LAYER_TAG);

            if (layers == null)
                name = DEFAULT_NAME;
            else
                name = layers.Length == 0 ? DEFAULT_NAME : DEFAULT_NAME + layers.Length;

            id = System.Guid.NewGuid().ToString("N");   //We are creating a random ID here
            sceneElements = new List<EditorObject>();
            container = new GameObject();

            container.tag = LAYER_TAG;

            UpdateContainerName();
        }

        public Layer(string name, GameObject container)
        {
            this.name = name;
            id = System.Guid.NewGuid().ToString("N");   //We are creating a random ID here
            this.container = container;
            sceneElements = new List<EditorObject>();
        }

        /// <summary>
        /// Updates the name of the container depending on the user's preference
        /// </summary>
        public void UpdateContainerName()
        {
            if (container != null)
                container.name = name;
        }

        /// <summary>
        /// Get the unique ID
        /// </summary>
        /// <returns>A unique ID</returns>
        public string GetID()
        {
            return id;
        }

        /// <summary>
        /// Called when removing a layer
        /// </summary>
        public void Destroy()
        {
            GameObject.DestroyImmediate(container);
        }

        /// <summary>
        /// Add a new element
        /// </summary>
        /// <param name="e">A game object (prefab)</param>
        public void AddSceneElement(EditorObject e)
        {
            sceneElements.Add(e);
            e.reference.transform.parent = container.gameObject.transform;
        }

        /// <summary>
        /// Get all the elements that belong to this layer
        /// </summary>
        /// <returns>List of elements</returns>
        public List<EditorObject> GetSceneElements()
        {
            return sceneElements;
        }
    }
}