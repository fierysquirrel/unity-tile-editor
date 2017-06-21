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
    /// This represents an object creation menu in the editor
    /// </summary>
    public class EditorObjectsMenu
    {
        public const string DEFAULT_FOLDER = "Tiles";   //Default folder that appears in the folder field
        public string name;                             //This will change depending on the folder's name
        public string folder;                           //Folder's name
        public int selGridInt;                          //Selected element
        public int columns;                             //To organize the elements graphically, number of columns
        public Texture[] tiles;                         //Textures of each prefab (from SpriteRenderer)
        public Object[] prefabs;                        //Prefabs that represent game objects

        public EditorObjectsMenu()
        {
            selGridInt = -1;
            name = DEFAULT_FOLDER;
            folder = DEFAULT_FOLDER;
            columns = 3;
        }

        /// <summary>
        /// Load all the prefabs in folder "folder"
        /// </summary>
        public void LoadPrefabs()
        {
            prefabs = Resources.LoadAll(folder, typeof(Object));
            tiles = new Texture[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
                tiles[i] = ((GameObject)prefabs[i]).GetComponent<SpriteRenderer>().sprite.texture;
        }

        /// <summary>
        /// Get the the selected element
        /// </summary>
        /// <returns>The selected element</returns>
        public Object GetCurrentSelection()
        {
            Object sel;

            sel = null;
            if (selGridInt != -1)
                sel = prefabs[selGridInt];

            return sel;
        }

        /// <summary>
        /// Transform the object's parameters into a string (to save the state of the object)
        /// </summary>
        /// <returns>Serialized string of the data</returns>
        public string Serialize()
        {
            return name + "," + folder + "," + selGridInt + "," + columns;
        }

        /// <summary>
        /// Takes a serialized string and loads all the parameters of the object
        /// </summary>
        /// <param name="data">Serialized data (using Serialize)</param>
        public void Deserialize(string data)
        {
            string[] attributes = data.Split(',');

            name = attributes[0];
            folder = attributes[1];
            selGridInt = int.Parse(attributes[2]);
            columns = int.Parse(attributes[3]);
        }
    }
}