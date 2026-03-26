using System;
using StoryTool.Runtime;
using UnityEngine;

namespace StoryTool.BuiltInTasks
{
    [Serializable]
    public class StartTriggerWithFlag
    {
        [SerializeField]
        private StartTrigger startTrigger;

        public StartTrigger Trigger => startTrigger;

#if UNITY_EDITOR
        [SerializeField]
        private bool _isTriggered_editor = false;
        public bool IsTriggered
        {
            get => _isTriggered_editor;
            set => _isTriggered_editor = value;
        }
#else
        private bool _isTriggered = false;
        public bool IsTriggered
        {
            get => _isTriggered;
            set => _isTriggered = value;
        }
#endif
    }
}
