using System;
using UnityEngine;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Animation;

namespace Neo.HtmlToUitk.Navigation
{
    public sealed class ElementTransition : IPageTransitionHandle
    {
        public const string DefaultEasing = "ease-out-cubic";

        private readonly VisualElement _element;
        private readonly TransitionKind _kind;
        private readonly bool _entering;
        private readonly TransitionDirection _direction;
        private readonly double _duration;
        private readonly double _startTime;
        private readonly Action _onDone;
        private readonly StyleFloat _opacity;
        private readonly StyleScale _scale;
        private readonly StyleTranslate _translate;
        private IVisualElementScheduledItem _item;
        private bool _done;

        private ElementTransition(VisualElement element, TransitionKind kind, bool entering, TransitionDirection direction, int ms, Action onDone)
        {
            _element = element;
            _kind = kind;
            _entering = entering;
            _direction = direction;
            _duration = Math.Max(1, ms) / 1000.0;
            _onDone = onDone;
            _startTime = Time.realtimeSinceStartupAsDouble;
            _opacity = element.style.opacity;
            _scale = element.style.scale;
            _translate = element.style.translate;
        }

        public bool IsDone => _done;

        public static IPageTransitionHandle Start(VisualElement element, TransitionKind kind, bool entering, TransitionDirection direction, int ms, Action onDone)
        {
            if (element == null || kind == TransitionKind.None || ms <= 0 || element.panel == null)
            {
                onDone?.Invoke();
                return CompletedTransition.Instance;
            }

            ElementTransition transition = new ElementTransition(element, kind, entering, direction, ms, onDone);
            transition.Apply(0f);
            transition._item = element.schedule.Execute(transition.Tick).Every(16);
            return transition;
        }

        public void Complete()
        {
            if (_done)
                return;
            _done = true;
            _item?.Pause();
            Restore();
            _onDone?.Invoke();
        }

        private void Tick()
        {
            if (_done)
                return;
            double t = (Time.realtimeSinceStartupAsDouble - _startTime) / _duration;
            if (t >= 1.0 || _element.panel == null)
            {
                Complete();
                return;
            }

            Apply((float)Easing.Evaluate(DefaultEasing, t));
        }

        private void Apply(float progress)
        {
            TransitionFrame frame = TransitionMath.Evaluate(_kind, _entering, _direction, progress);
            switch (_kind)
            {
                case TransitionKind.Fade:
                    _element.style.opacity = frame.Opacity;
                    break;
                case TransitionKind.Scale:
                    _element.style.opacity = frame.Opacity;
                    _element.style.scale = new Scale(new Vector3(frame.Scale, frame.Scale, 1f));
                    break;
                case TransitionKind.SlideLeft:
                case TransitionKind.SlideRight:
                case TransitionKind.SlideUp:
                    _element.style.translate = new Translate(Length.Percent(frame.TranslateXPercent), Length.Percent(frame.TranslateYPercent));
                    break;
            }
        }

        private void Restore()
        {
            switch (_kind)
            {
                case TransitionKind.Fade:
                    _element.style.opacity = _opacity;
                    break;
                case TransitionKind.Scale:
                    _element.style.opacity = _opacity;
                    _element.style.scale = _scale;
                    break;
                case TransitionKind.SlideLeft:
                case TransitionKind.SlideRight:
                case TransitionKind.SlideUp:
                    _element.style.translate = _translate;
                    break;
            }
        }
    }
}
