# StoryTool - Documentation

**StoryTool** is a powerful Unity tool designed for creating interactive branching narratives. The tool provides a visual graph editor where you can create complex storylines by connecting atomic events.

## Table of Contents

- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [Creating Custom Events](#creating-custom-events)
- [Built-in Events](#built-in-events)
- [Node Customization](#node-customization)
- [Zenject Integration](#zenject-integration)
- [API Reference](#api-reference)
- [Samples](#samples)
- [Roadmap](#roadmap)

---

## Requirements

- **Unity**: 6.0 or higher
- **Dependencies**: `com.unity.nuget.mono-cecil` (1.10.2)

---

## Quick Start

### 1. Adding Component to Scene

1. Create an empty GameObject in the scene
2. Add the `StoryTool` component via `Add Component > StoryTool`
3. Click the **"Open graph"** button in the component inspector

### 2. Opening Graph Editor

After clicking the button, the graph editor window will open. Here you can visually create and edit your story.

### 3. Adding First Event

1. **Right-click** in an empty area of the graph to open the context menu
2. In the menu, select an event category (e.g., `BuiltIn/Start`)
3. The created node will appear on the graph

### 4. Connecting Events

1. **Drag** the output port (on the right) of one event to the input port (on the left) of another
2. This creates a connection between events in the story

### 5. Adding Comments (Optional)

For better organization of large graphs, you can add comments:
1. In the context menu (right-click), select the option to create a comment
2. Add comment text to describe a section of the graph
3. Move and resize the comment as needed

**Note:** Comments are only used in the editor and do not affect story execution.

---

## Core Concepts

### StoryGraph

The story is represented as a **graph**, where:
- **Nodes** are atomic events (`StoryTask`)
- **Edges** are connections between events that define the execution sequence
- **Comments** are text notes for organizing and documenting the graph (editor only)

### StoryTask

`StoryTask` is an abstract class that all story events inherit from. Each event:
- Has its own state (`ActivityFlag`)
- Can contain input and output triggers
- Executes in a specific sequence according to the graph

### Triggers

#### StartTrigger

- Displayed as an **input port** (on the left) in the node
- Used to **receive control** from previous events
- Provides a `Triggered` event to subscribe to

#### EndTrigger

- Displayed as an **output port** (on the right) in the node
- Used to **pass control** to the next events
- Called via the `Trigger()` method to activate the next event

### ActivityFlag

The activity flag defines the current state of an event:

- **`Inactive`** — the event is inactive or idle
- **`Active`** — the event is active (executing, processing)
- **`Failed`** — the event has finished with an error
- **`Completed`** — the event has successfully completed and should not run again

The `ActivityFlag` has a public getter, allowing developers to use it for various purposes such as:
- Visualizing node state during **Playmode** in the editor
- Conditional logic based on event state
- Debugging and state tracking
- Integration with external systems

---

## Creating Custom Events

### Basic Example

To create your own event, you need to:

1. Create a class inheriting from `StoryTask`
2. Define input (`StartTrigger`) and output (`EndTrigger`) triggers
3. Subscribe to the `StartTrigger.Triggered` event in the `OnAwake()` method
4. Implement the execution logic

```csharp
using StoryTool.Runtime;
using UnityEngine;

public class MyStoryTask : StoryTask
{
    [SerializeField]
    private StartTrigger start;

    [SerializeField]
    private EndTrigger end;

    [SerializeField]
    private int someIntegerValue;

    [SerializeField]
    private string someStringValue;

    protected override void OnAwake()
    {
        start.Triggered += OnStartTask;
    }

    private void OnStartTask()
    {
        Debug.Log($"someIntegerValue: {someIntegerValue}, someStringValue: {someStringValue}.");
        end.Trigger();
    }
}
```

**Important:**
- All fields with the `[SerializeField]` attribute are automatically displayed in the node in the editor
- Fields of type `StartTrigger` and `EndTrigger` are automatically displayed as input and output ports
- Subscription to `StartTrigger.Triggered` should occur in `OnAwake()`

### Using ActivityFlag

It is recommended to set `ActivityFlag` depending on the event state:

```csharp
using System.Collections;
using StoryTool.Runtime;
using UnityEngine;

[StoryTaskMenu("MyTasks/DelayTask")] 
public class DelayTask : StoryTask
{
    [SerializeField]
    private StartTrigger start;

    [SerializeField]
    private EndTrigger end;

    [SerializeField] 
    private float delaySeconds; 

    protected override void OnAwake()
    {
        start.Triggered += OnStartTask;
    }

    private void OnStartTask()
    {
        ActivityFlag = StoryTaskActivityFlag.Active;
        MonoBehaviour.FindAnyObjectByType<MonoBehaviour>().StartCoroutine(FinishAfterDelay(delaySeconds));
    }

    IEnumerator FinishAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        end.Trigger();
        ActivityFlag = StoryTaskActivityFlag.Inactive;
    }
}
```

### StoryTaskMenu Attribute

The `[StoryTaskMenu]` attribute allows you to specify the path in the context menu for creating events (similar to `CreateAssetMenu` for ScriptableObjects):

```csharp
[StoryTaskMenu("MyTasks/DelayTask")]
public class DelayTask : StoryTask
{
    // ...
}
```

Events with this attribute will be displayed in the context menu at the specified path.

### Convenient Base Classes

The tool provides two abstract classes to simplify development:

#### StoryPoint

`StoryPoint` already has one `StartTrigger` and one `EndTrigger`. You only need to implement the `ReceiveExecute()` method:

```csharp
using StoryTool.BuiltInTasks;
using StoryTool.Runtime;
using UnityEngine;

[StoryTaskMenu("MyTasks/SimpleTask")]
public class SimpleTask : StoryPoint
{
    [SerializeField] 
    private string message;

    protected override void ReceiveExecute()
    {
        Debug.Log(message);
        // Execution automatically proceeds to the next event
    }
}
```

**Features:**
- After `ReceiveExecute()` executes, control is **automatically** passed to the next event
- Execution occurs **synchronously** (immediately)

#### StoryLine

`StoryLine` also has one `StartTrigger` and one `EndTrigger`, but is designed for events that can run for an **unlimited duration** and complete at a time determined by the developer:

```csharp
using System.Collections;
using StoryTool.BuiltInTasks;
using StoryTool.Runtime;
using UnityEngine;

[StoryTaskMenu("MyTasks/DelayTask")]
public class DelayTask : StoryLine
{
    [SerializeField] 
    private float delaySeconds; 

    protected override void ReceiveExecute()
    {
        MonoBehaviour.FindAnyObjectByType<MonoBehaviour>().StartCoroutine(FinishAfterDelay(delaySeconds));
    }

    IEnumerator FinishAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        FinishExecute(); // Explicitly complete the event
    }
}
```

**Features:**
- You must **explicitly** call `FinishExecute()` to complete the event
- Suitable for events that run for extended periods and complete when the developer determines

**Note:** `StoryPoint` automatically completes after `ReceiveExecute()` executes, while `StoryLine` requires explicit completion via `FinishExecute()`.

### Creating Templates via Menu

You can create new event classes through the Unity menu:

1. **Assets > Create > StoryTool > StoryTask** — create a basic `StoryTask`
2. **Assets > Create > StoryTool > StoryLine** — create a `StoryLine`
3. **Assets > Create > StoryTool > StoryPoint** — create a `StoryPoint`

---

## Built-in Events

The tool comes with a set of ready-made events for controlling execution flow:

### Start

**Menu path:** `BuiltIn/Start`

**Description:** The starting point of the story. Automatically launched when the graph starts (in the `OnStart()` method). Has one output trigger that activates when the story starts.

**Usage:** Usually added once at the beginning of the graph as an entry point. Connect your first story event to it.

**Usage example:**
```
[Start] → [First Event] → [Second Event] → ...
```

### WhenAll

**Menu path:** `BuiltIn/WhenAll`

**Description:** Waits for **all** connected input events to complete, then passes control to the output event **once**.

**Characteristics:**
- Has a variable number of input triggers (`List<StartTrigger>`)
- Has one output trigger (`EndTrigger`)
- Executes only once, even if input events complete multiple times

**Usage:** For synchronizing multiple parallel story branches.

### Any

**Menu path:** `BuiltIn/Any`

**Description:** Passes control to the output event **every time** any of the connected input events completes.

**Characteristics:**
- Has a variable number of input triggers (`List<StartTrigger>`)
- Has one output trigger (`EndTrigger`)
- Can execute **multiple times** (each time any input fires)

**Usage:** For handling any of several parallel events.

### WhenAny

**Menu path:** `BuiltIn/WhenAny`

**Description:** Similar to `Any`, but executes **only once** — when the first of any input events completes.

**Characteristics:**
- Has a variable number of input triggers (`List<StartTrigger>`)
- Has one output trigger (`EndTrigger`)
- Executes only once (like `WhenAll`, but for any input)

**Usage:** For selecting one of several parallel events (e.g., player choice).

### Branch

**Menu path:** `BuiltIn/Branch`

**Description:** Passes control to **all** connected output events simultaneously.

**Characteristics:**
- Has one input trigger (`StartTrigger`)
- Has a variable number of output triggers (`List<EndTrigger>`)
- All outputs are activated **simultaneously**

**Usage:** For creating parallel execution branches.

---

## Node Customization

You can completely override the appearance of a node for a specific event type by creating a class that inherits from `StoryTaskNode` and marking it with the `[StoryTaskNodeDrawer]` attribute.

**Note:** `StoryTaskNode` inherits from Unity's `Node` class (part of GraphView). When overriding `BuildContent()`, you can use all properties and methods available in the `Node` class.

### Basic Customization Example

```csharp
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using StoryTool.Editor;

[StoryTaskNodeDrawer(typeof(StartStoryTask))]
public class StartStoryTaskNode : StoryTaskNode
{
    public StartStoryTaskNode(SerializedProperty taskProperty)
        : base(taskProperty)
    {
    }

    /// <summary>
    /// Override the node title
    /// </summary>
    protected override string GetTitle()
    {
        return "Entry point";
    }

    /// <summary>
    /// Fully control the node content
    /// </summary>
    protected override void BuildContent()
    {
        // Manually output the StartStoryTask.start property
        var so = SerializedTaskProperty.serializedObject;
        var startProp = SerializedTaskProperty.FindPropertyRelative("start").Copy();
        if (startProp != null)
        {
            var startField = new PropertyField(startProp);
            startField.Bind(so);
            mainContainer.Add(startField);
        }

        // Change background color
        mainContainer.style.backgroundColor = Color.green;
    }
}
```

### Methods to Override

#### GetTitle()

Override this method to change the node title:

```csharp
protected override string GetTitle()
{
    return "My Custom Title";
}
```

#### BuildContent()

Override this method for full control over the node content. You have access to `SerializedTaskProperty` which contains the serialized event. Since `StoryTaskNode` inherits from `Node`, you can use all `Node` class properties and methods.

**Example:**

```csharp
protected override void BuildContent()
{
    // Add custom field
    var customField = new TextField("Custom Label");
    mainContainer.Add(customField);

    // Manually add input port
    var startProp = SerializedTaskProperty.FindPropertyRelative("start");
    var startField = new PropertyField(startProp);
    startField.Bind(SerializedTaskProperty.serializedObject);
    inputContainer.Add(startField);

    // Manually add output port
    var endProp = SerializedTaskProperty.FindPropertyRelative("end");
    var endField = new PropertyField(endProp);
    endField.Bind(SerializedTaskProperty.serializedObject);
    outputContainer.Add(endField);

    // Apply styles
    mainContainer.style.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
    mainContainer.style.borderTopWidth = 2;
    mainContainer.style.borderTopColor = Color.yellow;
}
```

### StoryTaskNodeDrawer Attribute

The attribute links a custom node to an event type:

```csharp
[StoryTaskNodeDrawer(typeof(MyStoryTask))]
public class MyStoryTaskNode : StoryTaskNode
{
    // ...
}
```

You can specify multiple types:

```csharp
[StoryTaskNodeDrawer(typeof(TaskA), typeof(TaskB))]
public class CombinedTaskNode : StoryTaskNode
{
    // ...
}
```

---

## Zenject Integration

StoryTool can be easily integrated with the **Zenject** dependency injection system. For a complete integration example, see: [StoryTool-example-using-zenject](https://github.com/00wz/StoryTool-example-using-zenject.git)

---

## API Reference

### StoryTask

Base abstract class for all events.

#### Lifecycle Methods

- **`protected internal virtual void OnAwake()`**
  - Called from `StoryTool.Awake()` to initialize the event
  - Use for subscribing to triggers and one-time setup

- **`protected internal virtual void OnStart()`**
  - Called from `StoryTool.Start()` when the graph starts
  - Use for initial launch (e.g., `StartStoryTask`)

#### Properties

- **`public virtual StoryTaskActivityFlag ActivityFlag { get; protected set; }`**
  - Current state of the event
  - Public getter allows reading the state for various purposes (visualization, conditional logic, debugging, etc.)
  - Protected setter allows derived classes to modify the state

### StartTrigger

Class for input ports.

- **`public event Action Triggered`**
  - Event raised when the trigger is activated
  - Subscribe in `OnAwake()`

### EndTrigger

Class for output ports.

- **`public void Trigger()`**
  - Activates the next connected event
  - Call after completing the event logic

### StoryPoint

Abstract class for synchronous events.

- **`protected abstract void ReceiveExecute()`**
  - Implement the event logic
  - Execution automatically proceeds to the next event

### StoryLine

Abstract class for events that can run for unlimited duration.

- **`protected abstract void ReceiveExecute()`**
  - Implement the event launch logic

- **`protected void FinishExecute()`**
  - Call to complete the event and pass control forward

### StoryTaskNode

Base class for customizing nodes in the editor. Inherits from Unity's `Node` class.

- **`protected SerializedProperty SerializedTaskProperty { get; }`**
  - Property with the serialized event

- **`protected virtual string GetTitle()`**
  - Returns the node title (can be overridden)

- **`protected virtual void BuildContent()`**
  - Builds the node content (can be overridden)

### StoryTaskMenuAttribute

Attribute for specifying the path in the context menu.

```csharp
[StoryTaskMenu("Category/Subcategory/TaskName")]
```

### StoryTaskNodeDrawerAttribute

Attribute for linking a custom node to an event type.

```csharp
[StoryTaskNodeDrawer(typeof(MyStoryTask))]
```

### StoryTaskActivityFlag

Enumeration of event states.

- `Inactive` — inactive
- `Active` — active
- `Failed` — completed with error
- `Completed` — successfully completed

---

## Samples

The package includes sample projects demonstrating StoryTool usage:

- **Visual Novel Sample** — A complete visual novel example showing dialogue flow, player choices, scene transitions, and multiple endings. Available in the `Samples~/VisualNovelSample` folder.

To import samples, go to **Window > Package Manager**, select StoryTool, and click "Import" next to the desired sample.

---

## Roadmap

For planned features and future improvements, see [ROADMAP.md](ROADMAP.md).

---

## Support and Feedback

If you have questions or encounter issues, please create an issue in the project repository or contact the developer.

---

**Documentation Version:** 1.0.0  
**Last Updated:** 2026
