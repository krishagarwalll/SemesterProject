#if UNITY_EDITOR
using System;
using UnityEngine;

namespace StoryTool.Runtime
{
    /// <summary>
    /// Serializable data container for graph comments.
    /// Stores position, size, and title of a comment in the story graph.
    /// </summary>
    [Serializable]
    public class StoryGraphComment
    {
        /// <summary>
        /// Position and size of the comment in graph space.
        /// </summary>
        [SerializeField]
        private Rect _editorRect;

        /// <summary>
        /// Title/header text of the comment.
        /// </summary>
        [SerializeField]
        private string _title;
        
        public StoryGraphComment(Rect rect, string title)
        {
            _editorRect = rect;
            _title = title;
        }
    }
}
#endif
