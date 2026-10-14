using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Navigation;
using Neo.HtmlToUitk.Selectors;

namespace Neo.HtmlToUitk.Animation
{
    public readonly struct UitkAnimationEvent
    {
        public UitkAnimationEvent(VisualElement element, string animationId, int iteration)
        {
            Element = element;
            AnimationId = animationId;
            Iteration = iteration;
        }

        public VisualElement Element { get; }

        public string AnimationId { get; }

        public int Iteration { get; }
    }

    public sealed class UitkAnimator : IDisposable
    {
        private sealed class VisualElementNode : ISelectNode
        {
            private readonly VisualElement _element;

            public VisualElementNode(VisualElement element)
            {
                _element = element;
            }

            public string TypeName => _element.GetType().Name;

            public string Name => _element.name;

            public ISelectNode Parent => _element.parent != null ? new VisualElementNode(_element.parent) : null;

            public bool HasClass(string className) => _element.ClassListContains(className);
        }

        private sealed class Running
        {
            public VisualElement Element;
            public string AnimationId;
            public AnimInstance Instance;
            public PreparedAnim Prepared;
            public Dictionary<string, object> Saved = new Dictionary<string, object>(StringComparer.Ordinal);
            public IVisualElementScheduledItem Item;
            public double LastTime;
            public bool Cancelled;
            public ReducedMotionMode Mode;
            public bool StartedReduced;
        }

        private readonly Dictionary<AnimDef, PreparedAnim> _prepared = new Dictionary<AnimDef, PreparedAnim>();
        private readonly List<Running> _running = new List<Running>();
        private readonly HashSet<string> _warned = new HashSet<string>(StringComparer.Ordinal);
        private VisualElement _root;
        private AnimSpec _spec;
        private bool _disposed;
        private UitkRouter _router;
        private Action<string> _onShow;
        private Action<string> _onHide;

        public UitkAnimator(VisualElement root, AnimSpec spec)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        }

        public static Func<bool> SystemReducedMotion { get; set; }

        public bool ReducedMotion { get; set; }

        public AnimSpec Spec => _spec;

        public int ActiveCount => _running.Count;

        public event Action<UitkAnimationEvent> Started;

        public event Action<UitkAnimationEvent> Iterated;

        public event Action<UitkAnimationEvent> Finished;

        public event Action<UitkAnimationEvent> Cancelled;

        public bool IsReducedMotion => ReducedMotion || (SystemReducedMotion != null && SystemReducedMotion());

        public void Rebind(VisualElement root, AnimSpec spec = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UitkAnimator));
            StopAll(false);
            _root = root ?? throw new ArgumentNullException(nameof(root));
            if (spec != null)
                _spec = spec;
        }

        public void PlayOnLoad()
        {
            Play(AnimTrigger.OnLoad, _root);
        }

        public int Play(AnimTrigger trigger, VisualElement scope = null)
        {
            if (_disposed)
                return 0;
            VisualElement target = scope ?? _root;
            int started = 0;
            for (int b = 0; b < _spec.Bindings.Count; b++)
            {
                AnimBinding binding = _spec.Bindings[b];
                if (binding.Trigger != trigger)
                    continue;
                started += PlayBinding(binding, target);
            }

            return started;
        }

        public int PlayBinding(AnimBinding binding, VisualElement scope)
        {
            if (_disposed || binding == null || scope == null) return 0;
            AnimDef def = _spec.Find(binding.AnimationId);
            if (def == null)
            {
                Warn("Animation '" + binding.AnimationId + "' referenced by '" + binding.Selector + "' is not defined.");
                return 0;
            }

            SelectorList selector = SelectorList.Parse(binding.Selector);
            if (!selector.IsValid)
            {
                Warn("Selector '" + binding.Selector + "' is not supported: " + selector.Error);
                return 0;
            }

            List<VisualElement> matches = Collect(scope, selector);
            int count = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                if (Start(matches[i], def, binding.ReducedMotion, AnimTimeline.StaggerDelay(i, binding.Stagger)))
                    count++;
            }

            return count;
        }

        public bool PlayManual(string animationId, VisualElement element, ReducedMotionMode reducedMotion = ReducedMotionMode.Simplify, double extraDelay = 0.0)
        {
            if (_disposed) return false;
            AnimDef def = _spec.Find(animationId);
            if (def == null || element == null)
                return false;
            return Start(element, def, reducedMotion, extraDelay);
        }

        public void Stop(VisualElement element, bool jumpToEnd = false)
        {
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                if (_running[i].Element == element)
                    Finish(_running[i], jumpToEnd, true);
            }
        }

        public void StopAll(bool jumpToEnd)
        {
            for (int i = _running.Count - 1; i >= 0; i--)
                Finish(_running[i], jumpToEnd, true);
        }

        public void AttachRouter(UitkRouter router, Func<string, VisualElement> pageElement)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UitkAnimator));
            if (router == null) throw new ArgumentNullException(nameof(router));
            if (pageElement == null) throw new ArgumentNullException(nameof(pageElement));
            DetachRouter();
            _router = router;
            _onShow = pageId => PlayForPage(AnimTrigger.OnShow, pageElement(pageId));
            _onHide = pageId => PlayForPage(AnimTrigger.OnHide, pageElement(pageId));
            router.OnWillShow += _onShow;
            router.OnWillHide += _onHide;
        }

        private void DetachRouter()
        {
            if (_router != null)
            {
                _router.OnWillShow -= _onShow;
                _router.OnWillHide -= _onHide;
            }
            _router = null;
            _onShow = null;
            _onHide = null;
        }

        private void PlayForPage(AnimTrigger trigger, VisualElement page)
        {
            if (page != null)
                Play(trigger, page);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            DetachRouter();
            StopAll(false);
            _prepared.Clear();
        }

        private bool Start(VisualElement element, AnimDef def, ReducedMotionMode reducedMotion, double extraDelay)
        {
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                if (_running[i].Element == element && string.Equals(_running[i].AnimationId, def.Id, StringComparison.Ordinal))
                    Finish(_running[i], false, true);
            }

            MotionPlan plan = ReducedMotionPolicy.Apply(def, reducedMotion, IsReducedMotion);
            if (plan.Skip)
            {
                if (plan.JumpToEnd)
                    JumpToEnd(element, def);
                return false;
            }

            AnimDef effective = plan.Definition;
            PreparedAnim prepared;
            if (ReferenceEquals(effective, def))
            {
                if (!_prepared.TryGetValue(def, out prepared))
                {
                    prepared = PreparedAnim.Prepare(def);
                    _prepared[def] = prepared;
                }
            }
            else
            {
                prepared = PreparedAnim.Prepare(effective);
            }

            if (extraDelay > 1e-9 && effective.Fill == AnimFill.Forwards)
                effective = effective.WithFill(AnimFill.Both);

            Running run = new Running
            {
                Element = element,
                AnimationId = def.Id,
                Instance = new AnimInstance(effective, extraDelay),
                Prepared = prepared,
                LastTime = Time.realtimeSinceStartupAsDouble,
                Mode = reducedMotion,
                StartedReduced = reducedMotion != ReducedMotionMode.Off && IsReducedMotion
            };
            _running.Add(run);
            run.Item = element.schedule.Execute(() => Tick(run)).Every(16);
            Tick(run);
            return true;
        }

        private void Tick(Running run)
        {
            if (run.Cancelled || _disposed)
                return;
            if (run.Element.panel == null)
            {
                Finish(run, false, false);
                return;
            }

            if (run.Mode != ReducedMotionMode.Off && !run.StartedReduced && IsReducedMotion)
            {
                AnimDef definition = run.Instance.Definition;
                bool keepEnd = !definition.IsInfinite && (definition.Fill == AnimFill.Forwards || definition.Fill == AnimFill.Both);
                Finish(run, keepEnd, true);
                return;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            double dt = Math.Min(0.25, Math.Max(0.0, now - run.LastTime));
            run.LastTime = now;
            AnimFrame frame = run.Instance.Advance(dt);
            if (frame.JustStarted)
                Started?.Invoke(new UitkAnimationEvent(run.Element, run.AnimationId, 0));
            if (run.Cancelled || _disposed) return;
            if (frame.IterationCompleted)
                Iterated?.Invoke(new UitkAnimationEvent(run.Element, run.AnimationId, frame.Sample.Iteration));
            if (run.Cancelled || _disposed) return;
            if (frame.ApplyValues)
                ApplySample(run, frame.Sample.Progress);
            if (frame.JustFinished)
                Finish(run, false, false, frame.Revert);
        }

        private void ApplySample(Running run, double progress)
        {
            IStyle style = run.Element.style;
            VisualElement element = run.Element;
            Func<string, AnimValue> baseResolver = name => StyleApplier.TryGet(name, out StyleChannel c) ? c.Underlying(element) : null;
            for (int i = 0; i < run.Prepared.PropertyCount; i++)
            {
                string property = run.Prepared.PropertyAt(i);
                if (!StyleApplier.TryGet(property, out StyleChannel channel))
                {
                    Warn("Property '" + property + "' cannot be animated at runtime.");
                    continue;
                }

                if (!run.Saved.ContainsKey(property))
                    run.Saved[property] = channel.Capture(style);
                channel.Apply(style, run.Prepared.SampleProperty(i, progress, baseResolver));
            }
        }

        private void JumpToEnd(VisualElement element, AnimDef def)
        {
            PreparedAnim prepared = PreparedAnim.Prepare(def);
            double progress = AnimTimeline.DirectedProgress(def.Direction, Math.Max(0, (int)Math.Ceiling(def.Iterations) - 1), 1.0);
            for (int i = 0; i < prepared.PropertyCount; i++)
            {
                if (StyleApplier.TryGet(prepared.PropertyAt(i), out StyleChannel channel))
                    channel.Apply(element.style, prepared.SampleProperty(i, progress, null));
            }
        }

        private void Finish(Running run, bool jumpToEnd, bool cancelled, bool? revertOverride = null)
        {
            if (!_running.Remove(run))
                return;
            run.Cancelled = true;
            run.Item?.Pause();
            if (jumpToEnd)
                ApplySample(run, AnimTimeline.DirectedProgress(run.Instance.Definition.Direction, Math.Max(0, (int)Math.Ceiling(run.Instance.Definition.Iterations) - 1), 1.0));
            bool revert = revertOverride ?? (cancelled && !jumpToEnd);
            if (revert)
                Revert(run);
            UitkAnimationEvent evt = new UitkAnimationEvent(run.Element, run.AnimationId, 0);
            if (cancelled)
                Cancelled?.Invoke(evt);
            else
                Finished?.Invoke(evt);
        }

        private static void Revert(Running run)
        {
            IStyle style = run.Element.style;
            foreach (KeyValuePair<string, object> saved in run.Saved)
            {
                if (StyleApplier.TryGet(saved.Key, out StyleChannel channel))
                    channel.Restore(style, saved.Value);
            }
        }

        private static List<VisualElement> Collect(VisualElement scope, SelectorList selector)
        {
            List<VisualElement> result = new List<VisualElement>();
            Stack<VisualElement> stack = new Stack<VisualElement>();
            stack.Push(scope);
            while (stack.Count > 0)
            {
                VisualElement element = stack.Pop();
                if (selector.Matches(new VisualElementNode(element)))
                    result.Add(element);
                for (int i = element.childCount - 1; i >= 0; i--)
                    stack.Push(element[i]);
            }

            return result;
        }

        private void Warn(string message)
        {
            if (_warned.Add(message))
                Debug.LogWarning("[HtmlToUitk] " + message);
        }
    }
}
