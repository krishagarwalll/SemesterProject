using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using StoryTool.Runtime;
using System.Collections.Generic;

namespace StoryTool.Editor
{
    /// <summary>
    /// Custom visual element that renders a border around a <see cref="StoryTaskNode"/>.
    /// The border style reflects the current <see cref="StoryTaskActivityFlag"/>.
    /// </summary>
    public class StoryTaskNodeBorder : VisualElement
    {
        private static readonly Dictionary<StoryTaskActivityFlag, string> BorderClassMap = new()
        {
            { StoryTaskActivityFlag.Active, "active-border" },
            { StoryTaskActivityFlag.Failed, "failed-border" },
            { StoryTaskActivityFlag.Completed, "completed-border" }
        };

        public StoryTaskNodeBorder()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.top = 0;
            style.left = 0;
            style.right = 0;
            style.bottom = 0;

            var styleSheet = Resources.Load<StyleSheet>("StoryTaskNodeBorder");
            if (styleSheet != null)
                this.styleSheets.Add(styleSheet);

            // Use the "inactive" border style by default.
            SetActivityState(StoryTaskActivityFlag.Inactive);
        }

        public void SetActivityState(StoryTaskActivityFlag activityFlag)
        {
            foreach (var cls in BorderClassMap.Values)
                RemoveFromClassList(cls);

            if (BorderClassMap.TryGetValue(activityFlag, out var className))
                AddToClassList(className);
        }

    }
}
