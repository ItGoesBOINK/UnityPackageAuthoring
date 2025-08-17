using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SupaFabulus.Util.Tools.PackageAuthoring
{

    [Serializable]
    public struct PackageCreationProperties
    {
        
        
        [Space(8)][Header("Package Identity")][Space(2)]
        [SerializeField]
        public string prefixName;
        [SerializeField]
        public string categoryName;
        [SerializeField]
        public string moduleName;
        [SerializeField]
        public string packageName;
        [SerializeField]
        public string packageVersion;
        
        [Space(8)][Header("Package Display")][Space(2)]
        [SerializeField]
        public string displayName;
        [SerializeField][Multiline]
        public string description;
        
        [Space(8)][Header("Editor Origin")][Space(2)]
        [SerializeField]
        public string unityEditorVersion;
        [SerializeField]
        public string unityEditorRelease;
        
        [Space(8)][Header("Support Links")][Space(2)]
        [SerializeField]
        public string documentationURL;
        [SerializeField]
        public string changeLogURL;
        [SerializeField]
        public string licenseURL;
        
        [Space(8)][Header("Author Info")][Space(2)]
        [SerializeField]
        public string authorName;
        [SerializeField]
        public string authorEMail;
        [SerializeField]
        public string authorURL;
    }


    [Serializable]
    [CreateAssetMenu(
        fileName = "PackageCreator", 
        menuName = "SupaFabulus/Util/Package Authoring/Package Creator")]
    public class PackageCreator : ScriptableObject
    {
        
        [SerializeField][HideInInspector]
        protected bool _showDefaultUI;
        
        [SerializeField]
        protected DefaultAsset _packageTemplateSource;
        [SerializeField]
        protected DefaultAsset _newPackageDestination;
        [SerializeField]
        protected PackageCreationProperties _configuration;

        public bool ShowDefaultUI { get => _showDefaultUI; set => _showDefaultUI = value; }

        

        public bool PackageTemplateSourceIsValid =>
            _packageTemplateSource != null && _packageTemplateSource != _newPackageDestination;
        
        public bool TargetPackageDestinationIsValid => _newPackageDestination != null;
        
        public bool TargetPackagePropertiesAreValid => !IsBlank(_configuration.prefixName) &&
                                                       !IsBlank(_configuration.packageName) &&
                                                       !IsBlank(_configuration.packageVersion) &&
                                                       !IsBlank(_configuration.displayName) &&
                                                       !IsBlank(_configuration.description) &&
                                                       !IsBlank(_configuration.unityEditorVersion) &&
                                                       !IsBlank(_configuration.unityEditorRelease) &&
                                                       !IsBlank(_configuration.documentationURL) &&
                                                       !IsBlank(_configuration.changeLogURL) &&
                                                       !IsBlank(_configuration.licenseURL) &&
                                                       !IsBlank(_configuration.authorName) &&
                                                       !IsBlank(_configuration.authorEMail) &&
                                                       !IsBlank(_configuration.authorURL);

        public bool IsReadyToCreatePackage =>  PackageTemplateSourceIsValid    &&
                                               TargetPackageDestinationIsValid &&
                                               TargetPackagePropertiesAreValid;

        private static readonly List<string> _tokenizableFileTypeSuffixes = new ()
        { "asmdef", "md", "txt", "html", "json" };

        private string GetAssetPath(DefaultAsset asset) => asset != null
            ? AssetDatabase.GetAssetPath(asset)
            : null;
        
        private string GetPathFromGUID(string id) => id != null
            ? AssetDatabase.GUIDToAssetPath(id)
            : null;

        private string GetAssetNameFromPath(string path)
        {
            if (IsBlank(path)) return null;
            int first = path.LastIndexOf('/') + 1;
            int last = path.LastIndexOf('.');
            return path.Substring(first, last - first);
        }



        private string[] GetChildAssetPaths(DefaultAsset parentFolder, string filter = null)
        {
            if (parentFolder == null) return null;
            return GetChildAssetPaths(GetAssetPath(parentFolder));
        }
        private string[] GetChildAssetPaths(string parentFolderPath, string filter = null)
        {
            if (IsBlank(parentFolderPath)) return null;
            string[] childIDs = AssetDatabase.FindAssets(filter, new [] {parentFolderPath});
            if (childIDs == null || childIDs.Length == 0) return null;

            int c = childIDs.Length;
            string[] paths = new string[c];
            for (int i = 0; i < c; i++)
            {
                paths[i] = GetPathFromGUID(childIDs[i]);
            }

            return paths;
        }

        private bool IsBlank(string str) => string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);

        private bool IsValidTokenizedAssetSuffix(string suffix) =>
            !IsBlank(suffix) && _tokenizableFileTypeSuffixes.Contains(suffix);

        public bool CreatePackage()
        {
            bool ready = IsReadyToCreatePackage;
            if (ready)
            {
                string srcPath = GetAssetPath(_packageTemplateSource);
                string destPath = GetAssetPath(_newPackageDestination);
                if (!AssetDatabase.IsValidFolder(destPath))
                {
                    Debug.LogError($"Invalid Destination: {destPath}");
                    return false;
                }

                destPath += $"/{_configuration.packageName}";
                AssetDatabase.CopyAsset(srcPath, destPath);
                DefaultAsset dupeRoot = AssetDatabase.LoadAssetAtPath<DefaultAsset>(destPath);
                if (dupeRoot == null)
                {
                    Debug.LogError("Error duplicating Package Template!");
                    return false;
                }

                string[] childPaths = GetChildAssetPaths(destPath);
                if (childPaths == null || childPaths.Length < 1)
                {
                    Debug.LogError($"No child assets found in source template path!");
                    return false;
                }

                int c = childPaths.Length;
                string p;
                string suffix;
                DefaultAsset child;
                for (int i = 0; i < c; i++)
                {
                    p = childPaths[i];
                    suffix = p.Substring(p.LastIndexOf('.') + 1);
                    if (!IsValidTokenizedAssetSuffix(suffix)) continue;
                    ReplaceTokensInFileAtPath(p);
                    child = AssetDatabase.LoadAssetAtPath<DefaultAsset>(p);
                    if(child != null) EditorUtility.SetDirty(child);
                }

                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                return true;
            }
            
            Debug.LogError("Unable to Create New Package because the PackageCreator was not ready.");

            return false;
        }

        private void ReplaceTokensInFileAtPath(string path)
        {
            if (IsBlank(path)) return;

            string content = File.ReadAllText(path);
            string tokenizedContent = ReplaceTokensInTextContent(content);
            File.WriteAllText(path, tokenizedContent);

            string assetName = GetAssetNameFromPath(path);
            AssetDatabase.RenameAsset(path, ReplaceTokensInTextContent(assetName));
        }

        
        public const string TOKEN__PREFIX = "[PKG_PREFIX]";
        public const string TOKEN__CATEGORY = "[PKG_CATEGORY]";
        public const string TOKEN__MODULE_NAME = "[MODULE_NAME]";
        public const string TOKEN__PACKAGE_NAME = "[PKG_NAME]";
        public const string TOKEN__PACKAGE_ID = "[PKG_ID]";
        public const string TOKEN__PACKAGE_NAMESPACE = "[PKG_NAMESPACE]";
        public const string TOKEN__DISPLAY_NAME = "[PKG_DISPLAY_NAME]";
        public const string TOKEN__DESCRIPTION = "[PKG_DESC]";
        
        public const string TOKEN__PKG_VER = "[PKG_VERSION]";
        public const string TOKEN__UNITY_VER = "[UNITY_VERSION]";
        public const string TOKEN__UNITY_RELEASE = "[UNITY_RELEASE]";
        
        public const string TOKEN__URL_DOCS = "[URL_DOCS]";
        public const string TOKEN__URL_CHANGELOG = "[URL_CHANGELOG]";
        public const string TOKEN__URL_LICENSE = "[URL_LICENSE]";
        
        public const string TOKEN__AUTHOR_NAME = "[AUTHOR_NAME]";
        public const string TOKEN__AUTHOR_EMAIL = "[AUTHOR_EMAIL]";
        public const string TOKEN__AUTHOR_URL = "[AUTHOR_URL]";


        private string ComposedPackageID => $"{_configuration.prefixName}." +
                                            (!IsBlank(_configuration.categoryName) 
                                                ? $"{_configuration.categoryName}." : string.Empty) +
                                            (!IsBlank(_configuration.moduleName) 
                                                ? $"{_configuration.moduleName}." : string.Empty) +
                                            $"{_configuration.packageName}";
        
        private string ReplaceTokensInTextContent(string content)
        {
            return (IsBlank(content))
                ? content
                : content
                    .Replace(TOKEN__PACKAGE_ID, ComposedPackageID.ToLower())
                    .Replace(TOKEN__PACKAGE_NAMESPACE, ComposedPackageID)
                    .Replace(TOKEN__PREFIX,     _configuration.prefixName)
                    .Replace(TOKEN__CATEGORY,     _configuration.categoryName)
                    .Replace(TOKEN__MODULE_NAME,  _configuration.moduleName)
                    .Replace(TOKEN__PACKAGE_NAME, _configuration.packageName)
                    .Replace(TOKEN__DISPLAY_NAME, _configuration.displayName)
                    .Replace(TOKEN__DESCRIPTION,  _configuration.description)
                    .Replace(TOKEN__PKG_VER,  _configuration.packageVersion)
                    .Replace(TOKEN__UNITY_VER,  _configuration.unityEditorVersion)
                    .Replace(TOKEN__UNITY_RELEASE,  _configuration.unityEditorRelease)
                    .Replace(TOKEN__URL_DOCS,  _configuration.documentationURL)
                    .Replace(TOKEN__URL_CHANGELOG,  _configuration.changeLogURL)
                    .Replace(TOKEN__URL_LICENSE,  _configuration.licenseURL)
                    .Replace(TOKEN__AUTHOR_NAME,  _configuration.authorName)
                    .Replace(TOKEN__AUTHOR_EMAIL,  _configuration.authorEMail)
                    .Replace(TOKEN__AUTHOR_URL,  _configuration.authorURL);

        }
    }
}