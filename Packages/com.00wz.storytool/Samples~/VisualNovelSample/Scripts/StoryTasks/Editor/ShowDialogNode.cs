using StoryTool.Editor;
using StoryTool.Editor.BuiltInTasks;
using UnityEditor;

namespace StoryTool.Samples.VisualNovel
{
    [StoryTaskNodeDrawer(typeof(ShowDialog), typeof(ShowDialogWithChoises))]
    public class ShowDialogNode : StoryLineNode
    {
        public ShowDialogNode(SerializedProperty taskProperty) : base(taskProperty)
        {
        }

        protected override void BuildContent()
        {
            base.BuildContent();
            style.width = 250f;
        }
    }
}