using System;
using UnityEditor;
using UnityEngine;

namespace SupaFabulus.Util.Tools.PackageAuthoring
{
    [Serializable]
    [CustomEditor(typeof(PackageCreator))]
    public class PackageCreatorEditor : Editor
    {
        [SerializeField]
        private PackageCreator _c;

        

        public override void OnInspectorGUI()
        {
            _c = target as PackageCreator;
            if (_c == null) return;

            RenderDefaultUIContainer();
            RenderMainControlStrip();
        }

        private bool IsReadyToCreatePackage => _c.IsReadyToCreatePackage;
        private void HandleCreatePackage() => _c.CreatePackage();

        private void RenderMainControlStrip()
        {
            GUIStyle s = new GUIStyle(GUI.skin.box);
            EditorGUILayout.BeginVertical(s);

            RenderOptionsManagementUI();
            RenderPackageCreationUI();

            EditorGUILayout.EndVertical();
        }

        private void RenderOptionsManagementUI()
        {
            
        }

        private void RenderPackageCreationUI()
        {
            bool isReady = IsReadyToCreatePackage;
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            Rect r = EditorGUILayout.GetControlRect(false, 22, btnStyle);
            GUI.enabled = isReady;
            bool didClickCreate = GUI.Button(r, "Create Package", btnStyle);
            if(didClickCreate) HandleCreatePackage();
            GUI.enabled = true;
        }

        private void RenderDefaultUIContainer()
        {
            GUIStyle s = new GUIStyle(GUI.skin.box);
            EditorGUILayout.BeginVertical(s);
            bool show = _c.ShowDefaultUI;
            show = EditorGUILayout.ToggleLeft("Show Options", show);
            if (show)
            {
                EditorGUILayout.BeginVertical(s);
                base.OnInspectorGUI();
                EditorGUILayout.EndVertical();
            }

            _c.ShowDefaultUI = show;

            EditorGUILayout.EndVertical();
        }
    }
}