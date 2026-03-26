using StoryTool.BuiltInTasks;
using StoryTool.Runtime;
using UnityEngine;

public class TestStoryPpint : StoryPoint
{
    protected override void ReceiveExecute()
    {
        Debug.Log("A thing has occured"); // This is where the StoryPoint actions take place
    }
}