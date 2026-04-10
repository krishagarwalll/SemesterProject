using UnityEngine;
using UnityEditor;

namespace RetroShadersPro
{
    public class RetroInstaller : Editor
    {
        private static readonly string additionalGraphPackageGUID = "02db46bcdeb35a74cad670bcb243b251";
        private static readonly string additionalGraphInstallGUID = "719e91fc4570c294fb4705c2a1fe9e9f";

        public class RetroImport : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
            {
                foreach (string str in importedAssets)
                {
                    // If we detect that this very file was reimported, trigger the installation window.
                    if (str.Contains("RetroInstaller.cs"))
                    {
                        RetroInstallerWindow.ShowWindow();
                    }
                }
            }
        }

        static RetroInstaller()
        {
            AssetDatabase.importPackageCompleted += Initialize;
        }

        private static void Initialize(string packagename)
        {
            Initialize();
        }

        public static void Initialize()
        {

        }

        public static void InstallGraphPackage()
        {
            string path = AssetDatabase.GUIDToAssetPath(additionalGraphPackageGUID);

            if (path.Length > 0)
            {
                AssetDatabase.ImportPackage(path, true);
            }
            else
            {
                Debug.LogError($"(Retro Shaders Pro): Could not locate package file for the additional Shader Graphs. Consider manually installing the package.");
            }
        }

        public static bool GraphsAreInstalled()
        {
            string path = string.Empty;
            path = AssetDatabase.GUIDToAssetPath(additionalGraphInstallGUID);

            if (path.Length > 0)
            {
                var file = AssetDatabase.LoadAssetAtPath(path, typeof(object));

                return (file != null);
            }

            return false;
        }
    }
}


