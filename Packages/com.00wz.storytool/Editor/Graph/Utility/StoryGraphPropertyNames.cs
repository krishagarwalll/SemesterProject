namespace StoryTool.Editor
{
    /// <summary>
    /// Centralized string constants for StoryGraph-related serialized property names.
    /// Used by editor classes such as StoryGraphController and StoryGraphView to
    /// avoid duplication and keep property names in sync.
    /// </summary>
    internal static class StoryGraphPropertyNames
    {
        /// <summary>
        /// Name of the collection of StoryTask instances in StoryGraph.
        /// </summary>
        internal const string StoryTasks = "storyTasks";

        /// <summary>
        /// Name of the field on EndTrigger that points to the next TriggerLink.
        /// </summary>
        internal const string NextTriggerLink = "_nextTriggerLink";

        /// <summary>
        /// Name of the field on StartTrigger that stores its TriggerLink.
        /// </summary>
        internal const string TriggerLink = "_triggerlink";

        /// <summary>
        /// Name of the field on StoryTask that stores the editor node position.
        /// </summary>
        internal const string NodePosition = "_editorNodePosition";

        /// <summary>
        /// Name of the collection of StoryGraphComment instances in StoryGraph.
        /// </summary>
        internal const string Comments = "comments";

        /// <summary>
        /// Name of the field on StoryGraphComment that stores the editor rect (position and size).
        /// </summary>
        internal const string CommentRect = "_editorRect";

        /// <summary>
        /// Name of the field on StoryGraphComment that stores the title.
        /// </summary>
        internal const string CommentTitle = "_title";
    }
}