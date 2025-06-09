#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("ThisSome1.ColorfulHierarchy")]
namespace ThisSome1.ColorfulHierarchy
{
    [AddComponentMenu("Palapal/ColorfulHierarchy/Color Settings")]
    internal class ColorDesign : MonoBehaviour
    {
        [Header("This GameObject should not contain any component\nand will not remain in the build.\n\nName Format: \\\\ [Title]"), Space(15)]
#pragma warning disable IDE0044 // Add readonly modifier
        [SerializeField] private bool _removeInPlayMode;
#pragma warning restore IDE0044 // Add readonly modifier
        [SerializeField] internal FolderDesign Settings;

        internal static Object GetMonoScript => MonoScript.FromMonoBehaviour(new());

        private void Awake()
        {
            if (_removeInPlayMode && EditorApplication.isPlaying)
            {
                transform.DetachChildren();
                Destroy(gameObject);
            }
        }
    }
}
#endif