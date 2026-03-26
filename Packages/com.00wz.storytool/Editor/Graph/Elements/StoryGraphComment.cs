using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using StoryTool.Runtime;
using UnityEditor.UIElements;

namespace StoryTool.Editor
{
    /// <summary>
    /// Visual element representing a comment in the story graph.
    /// Inherits from StickyNote to use its built-in resizing, moving, and editing functionality.
    /// </summary>
    public class StoryGraphComment :  GraphElement, IResizable
    {
        private Label m_Title;
        private TextField m_TitleField;
        private SerializedProperty _commentProperty;
        public static readonly Vector2 defaultSize = new Vector2(200f, 160f);
        public override string title
        {
            get
            {
                return m_Title.text;
            }
            set
            {
                if (m_Title != null)
                {
                    m_Title.text = value;
                    if (!string.IsNullOrEmpty(m_Title.text))
                    {
                        m_Title.RemoveFromClassList("empty");
                    }
                    else
                    {
                        m_Title.AddToClassList("empty");
                    }
                }
            }
        }

        /// <summary>
        /// SerializedProperty associated with this comment's data.
        /// </summary>
        public SerializedProperty SerializedCommentProperty => _commentProperty;

        /// <summary>
        /// Creates a new StoryGraphCommentElement bound to the specified SerializedProperty.
        /// </summary>
        /// <param name="commentProperty">SerializedProperty associated with a StoryGraphComment instance.</param>
        public StoryGraphComment(SerializedProperty commentProperty) 
            : this("StoryGraphCommentVisualTree")
        {
            base.styleSheets.Add(EditorGUIUtility.Load("StyleSheets/GraphView/Selectable.uss") as StyleSheet);
            base.styleSheets.Add(EditorGUIUtility.Load("StyleSheets/GraphView/StickyNote.uss") as StyleSheet);
            this.styleSheets.Add(Resources.Load<StyleSheet>("StoryGraphComment"));
            AddToClassList("story-comment");

            _commentProperty = commentProperty;

            // Set initial values from property
            var rect = GetRectFromProperty();
            var titleText = GetTitleFromProperty();

            title = titleText;
            SetPosition(rect);

            // Bind to property changes
            BindToSerializedProperty();
        }

        public StoryGraphComment(string uiFile)
        {
            VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>(uiFile);
            visualTreeAsset.CloneTree(this);
            base.capabilities = Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable | Capabilities.Copiable;
            m_Title = this.Q<Label>("title");
            if (m_Title != null)
            {
                m_Title.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
            }

            m_TitleField = this.Q<TextField>("title-field");
            if (m_TitleField != null)
            {
                m_TitleField.style.display = DisplayStyle.None;
                m_TitleField.Q("unity-text-input").RegisterCallback<BlurEvent>(OnTitleBlur, TrickleDown.TrickleDown);
                m_TitleField.RegisterCallback<ChangeEvent<string>>(OnTitleChange);
            }

            AddToClassList("sticky-note");
            AddToClassList("selectable");
        }

        public override void SetPosition(Rect rect)
        {
            base.style.left = rect.x;
            base.style.top = rect.y;
            base.style.width = rect.width;
            base.style.height = rect.height;
        }
        
        public override Rect GetPosition()
        {
            return new Rect(base.resolvedStyle.left, base.resolvedStyle.top, base.resolvedStyle.width, base.resolvedStyle.height);
        }
        
        public override void UpdatePresenterPosition()
        {
            base.UpdatePresenterPosition();
            SaveRectToProperty(GetPosition());
        }
        
        public virtual void OnResized()
        {
            SaveRectToProperty(GetPosition());
        }

        private void OnTitleChange(EventBase e)
        {
            title = m_TitleField.value;
        }

        private void OnTitleBlur(BlurEvent e)
        {
            title = m_TitleField.value;
            m_TitleField.style.display = DisplayStyle.None;
            m_Title.UnregisterCallback<GeometryChangedEvent>(OnTitleRelayout);
            SaveTitleToProperty(title);
        }

        private void OnTitleRelayout(GeometryChangedEvent e)
        {
            UpdateTitleFieldRect();
        }

        private void UpdateTitleFieldRect()
        {
            Rect rect = m_Title.layout;
            m_Title.parent.ChangeCoordinatesTo(m_TitleField.parent, rect);
            m_TitleField.style.left = rect.xMin - 1f;
            m_TitleField.style.right = rect.yMin + m_Title.resolvedStyle.marginTop;
            m_TitleField.style.width = rect.width - m_Title.resolvedStyle.marginLeft - m_Title.resolvedStyle.marginRight;
            m_TitleField.style.height = rect.height - m_Title.resolvedStyle.marginTop - m_Title.resolvedStyle.marginBottom;
        }
        
        private void OnTitleMouseDown(MouseDownEvent e)
        {
            if (e.button == 0 && e.clickCount == 2)
            {
                m_TitleField.RemoveFromClassList("empty");
                m_TitleField.value = m_Title.text;
                m_TitleField.style.display = DisplayStyle.Flex;
                UpdateTitleFieldRect();
                m_Title.RegisterCallback<GeometryChangedEvent>(OnTitleRelayout);
                m_TitleField.Q(TextInputBaseField<string>.textInputUssName).Focus();
                m_TitleField.textSelection.SelectAll();
                e.StopPropagation();
                focusController.IgnoreEvent(e);
            }
        }

        /// <summary>
        /// Binds the element to the serialized property, tracking changes.
        /// </summary>
        private void BindToSerializedProperty()
        {
            var rectProperty = _commentProperty.FindPropertyRelative(StoryGraphPropertyNames.CommentRect);
            var titleProperty = _commentProperty.FindPropertyRelative(StoryGraphPropertyNames.CommentTitle);

            // Track rect changes
            this.TrackPropertyValue(rectProperty, property =>
            {
                var newRect = property.rectValue;
                    SetPosition(newRect);
            });

            // Track title changes
            this.TrackPropertyValue(titleProperty, property =>
            {
                title = property.stringValue;
            });
        }

        /// <summary>
        /// Gets the title from the serialized property.
        /// </summary>
        private string GetTitleFromProperty()
        {
            var titleProperty = _commentProperty.FindPropertyRelative(StoryGraphPropertyNames.CommentTitle);
            return titleProperty?.stringValue ?? "Comment";
        }

        /// <summary>
        /// Gets the rect from the serialized property.
        /// </summary>
        private Rect GetRectFromProperty()
        {
            var rectProperty = _commentProperty.FindPropertyRelative(StoryGraphPropertyNames.CommentRect);
            if (rectProperty != null && rectProperty.propertyType == SerializedPropertyType.Rect)
            {
                return rectProperty.rectValue;
            }
            return new Rect(Vector2.zero, StickyNote.defaultSize);
        }

        /// <summary>
        /// Saves the current rect to the serialized property.
        /// </summary>
        private void SaveRectToProperty(Rect rect)
        {
            if (_commentProperty == null)
                return;

            var rectProperty = _commentProperty.FindPropertyRelative(StoryGraphPropertyNames.CommentRect);
            if (rectProperty != null)
            {
                rectProperty.rectValue = rect;
                _commentProperty.serializedObject.ApplyModifiedProperties();
            }
        }
        /// <summary>
        /// Saves the current title to the serialized property.
        /// </summary>
        /// <param name="newTitleText">The new title text to save.</param>
        private void SaveTitleToProperty(string newTitleText)
        {
            var titleProperty = _commentProperty.FindPropertyRelative(StoryGraphPropertyNames.CommentTitle);
            if (titleProperty != null)
            {
                titleProperty.stringValue = newTitleText;
                _commentProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        public void OnStartResize()
        {
            //
        }
    }
}
