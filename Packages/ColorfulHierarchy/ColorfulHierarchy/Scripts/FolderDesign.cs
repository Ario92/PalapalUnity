#if UNITY_EDITOR
using UnityEngine;

namespace ThisSome1.ColorfulHierarchy
{
    /// <summary>
    /// Details of custom folders
    /// </summary>
    [System.Serializable]
    internal class FolderDesign
    {
        public Color textColor = Color.white, backgroundColor = Color.black;
        public TextAnchor textAlignment = TextAnchor.MiddleCenter;
        public FontStyle fontStyle = FontStyle.Bold;
        public int fontSize = 12;
    }
}
#endif