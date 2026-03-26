using UnityEngine;
using UnityEngine.UIElements;

namespace StoryTool.Editor
{
    public static class VisualElementExtentions
    {
        public static void ExpandPermanent(this Foldout foldout)
        {
            foldout.value = true;
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (!evt.newValue)
                    foldout.value = true;
            });
        }
    }
}
