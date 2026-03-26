using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using StoryTool.Runtime;
using System;
using UnityEditor;

namespace StoryTool.Editor
{
    /// <summary>
    /// Editor GraphView edge that reacts to <see cref="TriggerLink.Triggered"/> and visualizes
    /// small "pulse" markers traveling along the edge trajectory.
    /// </summary>
    /// <remarks>
    /// Rendering happens inside the associated <see cref="EdgeControl"/> via
    /// <see cref="VisualElement.generateVisualContent"/>, ensuring local coordinates match
    /// the edge line precisely.
    /// Prefer using EdgeControl's internal render polyline (via reflection); if it is not available,
    /// fall back to sampling the cubic Bézier defined by <see cref="EdgeControl.controlPoints"/>.
    /// Uses experimental UnityEditor APIs and is intended for Editor use only.
    /// </remarks>
    public class AnimatedEdge : Edge
    {
        // -------------------- Configuration constants --------------------
        private const int ScheduleIntervalMs = 16;     // ~60 FPS tick interval
        private const int PulseCircleSegments = 16;    // Circle tessellation for pulse mesh
        private const float Epsilon = 0.0001f;

        // -------------------- References / state --------------------
        private TriggerLink _link;                         // Source of trigger events
        private IVisualElementScheduledItem _schedule;     // Animation tick scheduler
        private bool _isAnimating;                         // Whether the animation is active
        private bool _isRenderHookAttached;                // Guard to avoid double hook
        private float _elapsedTimeSec;                     // Accumulated animation time
        private float _phaseOffsetPx;                      // Phase offset along the line (px)
        private float _lastTickTime;                       // Timestamp of last tick

        // -------------------- Appearance and motion parameters --------------------
        private Color _pulseColor = new Color(255 / 255f, 128 / 255f, 0f);
        private float _pulseDiameterPx = 6f;
        private float _flowSpeedPx = 500f;         // Pulse travel speed (px/sec)
        private float _pulseSpacingPx = 60f;       // Distance between pulses (px)
        private float _animationDurationSec = 1.0f;

        // -------------------- Reflection hook for EdgeControl internal polyline --------------------
        private static readonly System.Reflection.FieldInfo s_RenderPointsField =
            typeof(EdgeControl).GetField("m_RenderPoints", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // -------------------- Public parameters (external tuning) --------------------
        /// <summary>Pulse marker color.</summary>
        public Color PulseColor
        {
            get => _pulseColor;
            set => _pulseColor = value;
        }

        /// <summary>Pulse marker diameter in pixels.</summary>
        public float PulseDiameterPx
        {
            get => _pulseDiameterPx;
            set => _pulseDiameterPx = Mathf.Max(1f, value);
        }

        /// <summary>Pulse travel speed (px/sec).</summary>
        public float FlowSpeedPx
        {
            get => _flowSpeedPx;
            set => _flowSpeedPx = Mathf.Max(0f, value);
        }

        /// <summary>Distance between pulses along the edge (px).</summary>
        public float PulseSpacingPx
        {
            get => _pulseSpacingPx;
            set => _pulseSpacingPx = Mathf.Max(1f, value);
        }

        /// <summary>Total animation duration (sec).</summary>
        public float AnimationDurationSec
        {
            get => _animationDurationSec;
            set => _animationDurationSec = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Creates a new instance and subscribes to panel attach/detach events.
        /// </summary>
        public AnimatedEdge()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <summary>
        /// Panel attach handler: bind TriggerLink and set up the render hook.
        /// </summary>
        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            try
            {
                // 1) Bind to TriggerLink stored inside the StartTrigger of the input port
                if (input?.userData is SerializedProperty prop && prop.IsOfType<StartTrigger>())
                {
                    var linkProp = prop.FindPropertyRelative(StoryGraphPropertyNames.TriggerLink);
                    if (linkProp != null)
                    {
                        var link = linkProp.managedReferenceValue as TriggerLink;
                        if (link != null)
                        {
                            _link = link;
                            _link.Triggered += OnTriggered;
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // "SerializedProperty has disappeared!" exception catching
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AnimatedEdge: Error binding trigger: {e.Message}");
            }
 
            // 2) Draw in EdgeControl local space for pixel-perfect alignment with the line
            if (edgeControl != null && !_isRenderHookAttached)
            {
                edgeControl.generateVisualContent += OnEdgeGenerate;
                _isRenderHookAttached = true;
            }
        }

        /// <summary>
        /// Panel detach handler: unsubscribe from events and stop the animation.
        /// </summary>
        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            if (_link != null)
            {
                _link.Triggered -= OnTriggered;
                _link = null;
            }

            StopAnimation();

            if (edgeControl != null && _isRenderHookAttached)
            {
                edgeControl.generateVisualContent -= OnEdgeGenerate;
                _isRenderHookAttached = false;
            }
        }

        /// <summary>
        /// Starts the animation when <see cref="TriggerLink.Triggered"/> is raised.
        /// </summary>
        private void OnTriggered()
        {
            // start animation on main thread 
            EditorApplication.delayCall += StartAnimation;
        }

        /// <summary>
        /// Starts the animation
        /// </summary>
        private void StartAnimation()
        {
            if (_isAnimating || edgeControl == null)
                return;

            _isAnimating = true;
            _phaseOffsetPx = 0f;
            _elapsedTimeSec = 0f;
            _lastTickTime = Time.realtimeSinceStartup; 

            edgeControl.MarkDirtyRepaint();
            _schedule?.Pause();
            _schedule = this.schedule.Execute(TickAnimation).Every(ScheduleIntervalMs);
        }

        /// <summary>
        /// Stops the animation and clears the scheduler.
        /// </summary>
        private void StopAnimation()
        {
            _schedule?.Pause();
            _schedule = null;
            _isAnimating = false;
            _phaseOffsetPx = 0f;
        }

        /// <summary>
        /// Advances animation state by time delta and requests a repaint.
        /// </summary>
        private void TickAnimation()
        {
            if (edgeControl == null)
            {
                StopAnimation();
                return;
            }

            float now = Time.realtimeSinceStartup;
            float deltaSec = Mathf.Max(0f, now - _lastTickTime);
            _lastTickTime = now;

            // Phase offset repeats with period PulseSpacingPx
            _phaseOffsetPx = Mathf.Repeat(_phaseOffsetPx + deltaSec * _flowSpeedPx, Mathf.Max(PulseSpacingPx, 1f));
            _elapsedTimeSec += deltaSec;

            // Stop when total duration elapses
            if (_elapsedTimeSec >= _animationDurationSec)
            {
                StopAnimation();
                edgeControl.MarkDirtyRepaint();
                return;
            }

            edgeControl.MarkDirtyRepaint();
        }

        /// <summary>
        /// Renders pulse markers along the actual polyline drawn by <see cref="EdgeControl"/>.
        /// </summary>
        private void OnEdgeGenerate(MeshGenerationContext ctx)
        {
            if (!_isAnimating || edgeControl == null)
                return;

            if (!TryGetPolyline(out var points) || points.Length < 2)
                return;

            float totalLength = ComputeLength(points);
            float radius = _pulseDiameterPx * 0.5f;

            for (float dist = _phaseOffsetPx; dist < totalLength; dist += _pulseSpacingPx)
            {
                Vector2 pos = GetPointOnPolylineAtDistance(points, dist);
                DrawPulse(ctx, pos, radius, _pulseColor);
            }
        }

        /// <summary>
        /// Draws a single circular pulse marker using a triangle fan.
        /// </summary>
        private static void DrawPulse(MeshGenerationContext ctx, Vector2 center, float radius, Color color)
        {
            var mesh = ctx.Allocate(PulseCircleSegments + 1, PulseCircleSegments * 3);

            // Center vertex
            var centerV = new Vertex
            {
                position = new Vector3(center.x, center.y, Vertex.nearZ),
                tint = color
            };
            mesh.SetNextVertex(centerV);

            // Ring vertices
            for (int s = 0; s < PulseCircleSegments; s++)
            {
                float angle = (Mathf.PI * 2f * s) / PulseCircleSegments;
                float x = center.x + Mathf.Cos(angle) * radius;
                float y = center.y + Mathf.Sin(angle) * radius;

                mesh.SetNextVertex(new Vertex
                {
                    position = new Vector3(x, y, Vertex.nearZ),
                    tint = color
                });
            }

            // Triangle fan indices
            for (int s = 0; s < PulseCircleSegments; s++)
            {
                mesh.SetNextIndex(0);
                mesh.SetNextIndex((ushort)(s + 1));
                mesh.SetNextIndex((ushort)(((s + 1) % PulseCircleSegments) + 1));
            }
        }

        /// <summary>
        /// Attempts to get the actual edge render polyline (via reflection).
        /// If unavailable, builds a fallback approximation by sampling the cubic Bézier from controlPoints.
        /// Points are returned in EdgeControl local coordinates.
        /// </summary>
        private bool TryGetPolyline(out Vector2[] pts)
        {
            pts = null;
            try
            {
                var fi = s_RenderPointsField;
                if (fi != null)
                {
                    var obj = fi.GetValue(edgeControl);
                    if (obj is System.Collections.Generic.List<Vector2> list && list.Count >= 2)
                    {
                        pts = list.ToArray();
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore and use fallback path
            }

            var cps = edgeControl.controlPoints;
            if (cps != null && cps.Length == 4 && edgeControl.parent != null)
            {
                // controlPoints are in parent space — convert to EdgeControl local space
                Vector2 p0 = edgeControl.parent.ChangeCoordinatesTo(edgeControl, cps[0]);
                Vector2 p1 = edgeControl.parent.ChangeCoordinatesTo(edgeControl, cps[1]);
                Vector2 p2 = edgeControl.parent.ChangeCoordinatesTo(edgeControl, cps[2]);
                Vector2 p3 = edgeControl.parent.ChangeCoordinatesTo(edgeControl, cps[3]);

                const int samples = 32;
                var tmp = new Vector2[samples + 1];
                for (int i = 0; i <= samples; i++)
                {
                    float t = i / (float)samples;
                    tmp[i] = EvaluateCubicBezier(p0, p1, p2, p3, t);
                }
                pts = tmp;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Computes the total length of a polyline.
        /// </summary>
        private static float ComputeLength(Vector2[] points)
        {
            float len = 0f;
            for (int i = 1; i < points.Length; i++)
                len += Vector2.Distance(points[i - 1], points[i]);
            return len;
        }

        /// <summary>
        /// Returns a point along the polyline at the specified traveled distance.
        /// </summary>
        private static Vector2 GetPointOnPolylineAtDistance(Vector2[] points, float distance)
        {
            if (distance <= 0f) return points[0];

            float accum = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                float segLen = Vector2.Distance(points[i - 1], points[i]);
                if (accum + segLen >= distance)
                {
                    float t = (distance - accum) / Mathf.Max(Epsilon, segLen);
                    return Vector2.Lerp(points[i - 1], points[i], t);
                }
                accum += segLen;
            }

            return points[points.Length - 1];
        }

        /// <summary>
        /// Evaluates a cubic Bézier at t in [0,1].
        /// </summary>
        private static Vector2 EvaluateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0
                 + 3f * u * u * t * p1
                 + 3f * u * t * t * p2
                 + t * t * t * p3;
        }
    }
}