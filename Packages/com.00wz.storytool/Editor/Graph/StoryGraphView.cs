using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using StoryTool.Runtime;
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using BsiGame.UI.UIElements;

namespace StoryTool.Editor
{
    /// <summary>
    /// GraphView used to visualize and edit a <see cref="StoryGraph"/> in the editor.
    /// </summary>
    public class StoryGraphView : GraphView
    {
        private bool _handleGraphViewChanged = true;
        private IVisualElementScheduledItem _portLinkScheduler;
        private SerializedProperty _tasksProp;
        private SerializedProperty _commentsProp;

        private StoryGraphController _storyGraphController;

        /// <summary>
        /// Creates a new <see cref="StoryGraphView"/> bound to the specified <see cref="StoryGraph"/> serialized property.
        /// Validates that the given <paramref name="storyGraphProperty"/> references a <see cref="StoryGraph"/> field
        /// or a [SerializeReference] of that type before initializing the view.
        /// </summary>
        /// <param name="storyGraphProperty">SerializedProperty associated with a <see cref="StoryGraph"/> instance.</param>
        public StoryGraphView(SerializedProperty storyGraphProperty, StoryGraphController storyGraphController)
        {
            if (!storyGraphProperty.IsOfType<StoryGraph>())
            {
                throw new ArgumentException(
                    $"[StoryTool] SerializedProperty '{storyGraphProperty?.propertyPath}' must reference a serialized field " +
                    $"or [SerializeReference] whose type is {nameof(StoryGraph)}.",
                    nameof(storyGraphProperty));
            }
            
            _tasksProp = storyGraphProperty.FindPropertyRelative(StoryGraphPropertyNames.StoryTasks);
            _commentsProp = storyGraphProperty.FindPropertyRelative(StoryGraphPropertyNames.Comments);
            _storyGraphController = storyGraphController;

            var styleSheet = Resources.Load<StyleSheet>("StoryGraph");
            styleSheets.Add(styleSheet);
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ChildChangeManipulator());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();


            this.RegisterCallback<ChildChangeEvent>(OnHierarchyChanged);
            this.graphViewChanged += OnGraphViewChanged;

            this.TrackPropertyValue(_tasksProp, _ => RefreshNodes());
            this.TrackPropertyValue(_commentsProp, _ => RefreshComments());

            RefreshNodes();
            RefreshComments();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new System.Collections.Generic.List<Port>();
            foreach (var port in ports)
            {
                if (port != startPort &&
                    port.node != startPort.node &&
                    port.direction != startPort.direction &&
                    port.portType == startPort.portType)
                {
                    compatiblePorts.Add(port);
                }
            }
            return compatiblePorts;
        }

        /// <summary>
        /// Called when an element is added to or removed from the graph hierarchy.
        /// </summary>
        /// <param name="evt">Hierarchy change event data.</param>
        private void OnHierarchyChanged(ChildChangeEvent evt)
        {
            var isAdd = evt.newChildCount > evt.previousChildCount;
            var parent = evt.targetParent;
            var child = evt.targetChild;

            if (isAdd)
            {
                OnElementAdded(child);
            }
            else
            {
                OnElementRemoved(child);
            }
        }

        /// <summary>
        /// Called when a new element has been added to the graph hierarchy.
        /// </summary>
        /// <param name="newElement">Element that has been added.</param>
        private void OnElementAdded(VisualElement newElement)
        {
            newElement.Query<Foldout>().ForEach(f => f.ExpandPermanent());
            
            var newPorts = newElement.Query<Port>();
            if(newPorts.First() != null)
            {
                newPorts.ForEach(p =>
                {                        
                    try
                    {
                        if (p.userData is SerializedProperty portProperty 
                            && portProperty.IsOfType<EndTrigger>()
                            && portProperty.IsValueNotNull())
                        {
                            p.Unbind();
                            var triggerProperty = portProperty.FindPropertyRelative(StoryGraphPropertyNames.NextTriggerLink);
                            p.TrackPropertyValue(triggerProperty, (prop) => StartPortLinkScheduler());
                        } 
                    }
                    catch (ObjectDisposedException)
                    {
                        // "SerializedProperty has disappeared!" exception catching
                    }
                });
                StartPortLinkScheduler();
            }
        }

        /// <summary>
        /// Called when an element is removed from the graph hierarchy.
        /// </summary>
        /// <param name="removedElement">Element that has been removed.</param>
        private void OnElementRemoved(VisualElement removedElement)
        {
            var removedPorts = removedElement.Query<Port>();
            if(removedPorts.First() != null)
            {
                StartPortLinkScheduler();
            }
        }

        /// <summary>
        /// Rebuilds and redraws all StoryTask nodes from the underlying serialized collection.
        /// </summary>
        private void RefreshNodes()
        {
            nodes.OfType<StoryTaskNode>().ToList().ForEach(n => RemoveElement(n));

            for (int i = 0; i < _tasksProp.arraySize; i++)
            {
                var taskProperty = _tasksProp.GetArrayElementAtIndex(i);
                if(taskProperty.managedReferenceId < 0)
                {
                    Debug.LogError($"[StoryTool] Failed to load a StoryTask. " +
                   "The underlying task type may have been removed or renamed, " +
                   "or its reference was cleared.");
                    continue;
                }

                StoryTaskNode node;
                try
                {
                    node = StoryTaskNodeFactory.CreateNode(taskProperty);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[StoryTool] Failed to create node. Exception: {e}");
                    continue;
                }
                AddElement(node);
            }
        }

        /// <summary>
        /// Rebuilds and redraws all comment elements from the underlying serialized collection.
        /// </summary>
        private void RefreshComments()
        {
            var existingComments = this.Query<StoryGraphComment>().ToList();
            existingComments.ForEach(c => RemoveElement(c));

            for (int i = 0; i < _commentsProp.arraySize; i++)
            {
                var commentProperty = _commentsProp.GetArrayElementAtIndex(i);

                StoryGraphComment commentElement;
                try
                {
                    commentElement = new StoryGraphComment(commentProperty);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[StoryTool] Failed to create comment element. Exception: {e}");
                    continue;
                }
                AddElement(commentElement);
            }
        }

        /// <summary>
        /// Schedules a deferred relinking of all ports based on trigger data.
        /// </summary>
        private void StartPortLinkScheduler()
        {
            if (_portLinkScheduler == null)
            {
                _portLinkScheduler = schedule.Execute(LinkPorts);
            }

            _portLinkScheduler.ExecuteLater(0);
        }

        /// <summary>
        /// Builds connections between ports based on trigger links.
        /// During the process, duplicate connections are detected and removed.
        /// </summary>
        private void LinkPorts()
        {
            _handleGraphViewChanged = false;

            try
            {
                DeleteElements(edges);
            
                _storyGraphController.SyncTriggerLinks();

                var allPorts = this.Query<Port>().ToList();
                if (allPorts.Count == 0)
                {
                    return;
                }

                Dictionary<TriggerLink, Port> outputPorts = new();
                Dictionary<TriggerLink, Port> inputPorts = new();

                foreach (var port in allPorts)
                {
                    if (port.userData is not SerializedProperty portProp)
                    {
                        continue;
                    }

                    try
                    {
                        if (portProp.IsOfType<EndTrigger>() && portProp.IsValueNotNull())
                        {
                            var triggerLinkProperty = portProp.FindPropertyRelative(StoryGraphPropertyNames.NextTriggerLink);
                            if(triggerLinkProperty.managedReferenceValue is TriggerLink triggerLink)
                            {
                                outputPorts.TryAdd(triggerLink, port);
                            }
                        }
                        else if (portProp.IsOfType<StartTrigger>() && portProp.IsValueNotNull())
                        {
                            var triggerLinkProperty = portProp.FindPropertyRelative(StoryGraphPropertyNames.TriggerLink);
                            if(triggerLinkProperty.managedReferenceValue is TriggerLink triggerLink)
                            {
                                inputPorts.TryAdd(triggerLink, port);
                            }
                        }
                    }
                    catch (ObjectDisposedException)// "SerializedProperty has disappeared!" exception catching
                    {
                        continue;
                    }
                    catch (InvalidOperationException)// "InvalidOperationException: The operation is not possible when moved past all properties" exception catching
                    {
                        continue;
                    }
                }
                
                foreach (var outputPortData in outputPorts)
                {
                    if (!inputPorts.TryGetValue(outputPortData.Key, out Port inputPort))
                    {
                        continue;
                    }

                    var outputPort = outputPortData.Value;

                    var edge = new AnimatedEdge
                    {
                        output = outputPort,
                        input = inputPort
                    };
                    outputPort.Connect(edge);
                    inputPort.Connect(edge);
                    // Draw edges above most other elements for better visibility.
                    edge.layer = 100;
                    AddElement(edge);
                }
            }
            finally
            {
                _handleGraphViewChanged = true;
            }
        }
        
        /// <summary>
        /// Fills the graph view context menu when the user right-clicks on the background.
        /// </summary>
        /// <param name="evt">Context menu event data.</param>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            // Only handle the menu when right-clicking on the graph background itself.
            if (evt.target != this)
            {
                return;
            }

            var storyTaskTypes = TypeCache.GetTypesDerivedFrom<StoryTask>();
            var mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);

            foreach (var type in storyTaskTypes)
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                var attr = (StoryTaskMenuAttribute)Attribute.GetCustomAttribute(type, typeof(StoryTaskMenuAttribute));
                string menuPath = attr != null ? attr.MenuPath : type.Name;
                var localPos = mousePosition;

                evt.menu.AppendAction(
                    menuPath,
                    _ => _storyGraphController.CreateStoryTask(type, localPos));
            }

            evt.menu.AppendSeparator();
            evt.menu.AppendAction(
                "Create Comment",
                _ => _storyGraphController.CreateComment(mousePosition));

            evt.StopPropagation();// TODO: consider whether we need to stop propagation.
        }

        /// <summary>
        /// Handles graph element removal, edge creation and node movement events.
        /// </summary>
        /// <param name="change">Change description provided by GraphView.</param>
        /// <returns>The (potentially modified) change data.</returns>
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (!_handleGraphViewChanged)
            {
                return change;
            }

            HandleElementsRemoved(change);
            HandleEdgesCreated(change);
            HandleElementsMoved(change);

            return change;
        }

        private void HandleElementsRemoved(GraphViewChange change)
        {
            if (change.elementsToRemove == null)
            {
                return;
            }

            // handle edges removing
            foreach (var edge in change.elementsToRemove.OfType<Edge>())
            {
                var outputPort = edge.output;
                if(outputPort.userData is SerializedProperty endTriggerProp && endTriggerProp.IsOfType<EndTrigger>())
                {
                    _storyGraphController.Unlink(endTriggerProp);
                }
            }

            // handle nodes removing
            var removedTasksProperties = change.elementsToRemove
                .OfType<StoryTaskNode>()
                .Select(n => n.SerializedTaskProperty)
                .ToList();
            
            _storyGraphController.RemoveTasks(removedTasksProperties);

            // handle comments removing
            var removedCommentsProperties = change.elementsToRemove
                .OfType<StoryGraphComment>()
                .Select(c => c.SerializedCommentProperty)
                .ToList();
            
            _storyGraphController.RemoveComments(removedCommentsProperties);
        }

        private void HandleEdgesCreated(GraphViewChange change)
        {
            if (change.edgesToCreate == null)
            {
                return;
            }

            foreach (var edge in change.edgesToCreate)
            {
                var outputPort = edge.output;
                var inputPort = edge.input;
                if (outputPort.userData is SerializedProperty endTriggerProp
                    && endTriggerProp.IsOfType<EndTrigger>()
                    && inputPort.userData is SerializedProperty startTriggerProp
                    && startTriggerProp.IsOfType<StartTrigger>())
                {
                    _storyGraphController.Link(endTriggerProp, startTriggerProp);
                }
            }
        }

        private void HandleElementsMoved(GraphViewChange change)
        {
            if (change.movedElements == null)
            {
                return;
            }

            foreach (var element in change.movedElements)
            {
                if (element is StoryTaskNode node)
                {
                    var taskProperty = node.SerializedTaskProperty;
                    var newPosition = node.GetPosition().position;
                    _storyGraphController.SetNodePosition(taskProperty, newPosition);
                }
            }
        }
    }
}
