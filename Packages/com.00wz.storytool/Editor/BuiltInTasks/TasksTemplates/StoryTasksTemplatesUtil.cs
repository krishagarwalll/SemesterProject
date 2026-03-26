using System.IO;
using UnityEditor;
using UnityEngine;

namespace StoryTool.Editor.BuiltInTasks
{
    public static class StoryTasksTemplatesUtil
    {
        [MenuItem("Assets/Create/StoryTool/StoryLine", false, 10)]
        public static void CreateStoryLine()
        {
            CreateTaskFromTemplate("StoryLine.cs.txt");
        }

        [MenuItem("Assets/Create/StoryTool/StoryPoint", false, 20)]
        public static void CreateStoryPoint()
        {
            CreateTaskFromTemplate("StoryPoint.cs.txt");
        }

        [MenuItem("Assets/Create/StoryTool/StoryTask", false, 30)]
        public static void CreateStoryTask()
        {
            CreateTaskFromTemplate("StoryTask.cs.txt");
        }

        private static void CreateTaskFromTemplate(string templateName)
        {
            string[] guids = AssetDatabase.FindAssets($"{nameof(StoryTasksTemplatesUtil)} t:Script");
        
            if (guids.Length == 0)
            {
                Debug.LogError($"[StoryTool] Class script not found: {nameof(StoryTasksTemplatesUtil)}");
                return;
            }

            string thisPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string thisDirectory = Path.GetDirectoryName(thisPath);
            string templatePath = Path.Combine(thisDirectory, templateName);
            string defaultName = string.Concat("New", Path.GetFileNameWithoutExtension(templateName));

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, defaultName);
        }
    }
}
