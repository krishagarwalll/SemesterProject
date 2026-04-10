using UnityEngine;
using UnityEditor;

namespace RetroShadersPro
{
    public class RetroInstallerWindow : EditorWindow
    {
        private static int width = 400, height = 320;
        private static Texture2D bannerTexture;

        private static GUIStyle _headerStyle;
        private static GUIStyle headerStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(GUI.skin.label)
                    {
                        wordWrap = true,
                        fontSize = 16,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                return _headerStyle;
            }
        }

        private static GUIStyle _buttonStyle;
        private static GUIStyle buttonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        padding = new RectOffset(0, 0, 8, 8)
                    };
                }

                return _buttonStyle;
            }
        }

        private static GUIStyle _infoStyle;
        private static GUIStyle infoStyle
        {
            get
            {
                if (_infoStyle == null)
                {
                    _infoStyle = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        wordWrap = true,
                        fontSize = 12
                    };
                }

                return _infoStyle;
            }
        }

        private static GUIStyle _smallHeaderStyle;
        private static GUIStyle smallHeaderStyle
        {
            get
            {
                if (_smallHeaderStyle == null)
                {
                    _smallHeaderStyle = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        wordWrap = true,
                        fontSize = 12,
                        fontStyle = FontStyle.Bold
                    };
                }

                return _smallHeaderStyle;
            }
        }

        private static GUIStyle _pipelineBox;
        private static GUIStyle pipelineBox
        {
            get
            {
                if (_pipelineBox == null)
                {
                    _pipelineBox = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(10, 10, 10, 10),
                        margin = new RectOffset(5, 5, 0, 0)
                    };
                }

                return _pipelineBox;
            }
        }

        [MenuItem("Tools/Retro Shaders Pro/Open Installer Window", false, 0)]
        public static void ShowWindow()
        {
            EditorWindow editorWindow = GetWindow(typeof(RetroInstallerWindow), false, "Retro Pro Installer", true);

            editorWindow.autoRepaintOnSceneChange = true;
            editorWindow.ShowAuxWindow();

            editorWindow.position = new Rect((Screen.currentResolution.width / 2f) - (width * 0.5f), (Screen.currentResolution.height / 2f) - (height * 0.7f), width, height);

            editorWindow.maxSize = new Vector2(width, height);
            editorWindow.minSize = new Vector2(width, height);

            RetroInstaller.Initialize();

            editorWindow.Show();
        }

        private void OnEnable()
        {
            RetroInstaller.Initialize();
        }

        private void OnGUI()
        {
            // Try and retrieve the banner texture if we don't have it yet.
            if (bannerTexture == null)
            {
                bannerTexture = Resources.Load<Texture2D>("InstallerBanner");
            }

            if (bannerTexture != null)
            {
                var height = width * bannerTexture.height / bannerTexture.width;
                Rect bannerRect = new Rect(0, 0, width, height);
                GUI.DrawTexture(bannerRect, bannerTexture, ScaleMode.ScaleToFit);
                GUILayout.Space(height);
            }
            else
            {
                EditorGUILayout.LabelField("Retro Shaders Pro", headerStyle);
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Thanks for downloading Retro Shaders Pro.", headerStyle);

            GUILayout.Space(10);

            if (RetroInstaller.GraphsAreInstalled())
            {
                using (new EditorGUILayout.VerticalScope(pipelineBox))
                {
                    EditorGUILayout.LabelField("Shader Graph package", smallHeaderStyle);
                    EditorGUILayout.LabelField("You already have the Shader Graph package installed.", infoStyle);
#if UNITY_6000_OR_NEWER
                    EditorGUILayout.LabelField("<b><color=\"red\">Warning:</color></b> You may need to increase the number of shader variants via <i>Project Settings -> Shader Graph</i>.", infoStyle);
#else
                    EditorGUILayout.LabelField("<b><color=\"red\">Warning:</color></b> You may need to increase the number of shader variants via <i>Preferences -> Shader Graph</i>.", infoStyle);
#endif

#if UNITY_6000_OR_NEWER
                    if (GUILayout.Button("Open Project Settings", buttonStyle))
                    {
                        SettingsService.OpenProjectSettings("Project/ShaderGraph");
                    }
#else
                    if (GUILayout.Button("Open Preferences", buttonStyle))
                    {
                        SettingsService.OpenUserPreferences("Preferences/Shader Graph");
                    }
#endif
                }

                GUILayout.Space(10);
            }
            else
            {
                using (new EditorGUILayout.VerticalScope(pipelineBox))
                {
                    EditorGUILayout.LabelField("Shader Graph package", smallHeaderStyle);
                    EditorGUILayout.LabelField("The additional Shader Graph package contains extra versions of the Lit and Unlit shaders for you to duplicate and modify more easily to create your own effects.", infoStyle);
#if UNITY_6000_OR_NEWER
                    EditorGUILayout.LabelField("<b><color=\"red\">Warning:</color></b> You may need to increase the number of shader variants via <i>Project Settings -> Shader Graph</i>.", infoStyle);
#else
                    EditorGUILayout.LabelField("<b><color=\"red\">Warning:</color></b> You may need to increase the number of shader variants via <i>Preferences -> Shader Graph</i>.", infoStyle);
#endif

                    GUILayout.Space(10);

                    if (GUILayout.Button("Install the additional Shader Graph package.", buttonStyle))
                    {
                        RetroInstaller.InstallGraphPackage();
                    }
                }
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
