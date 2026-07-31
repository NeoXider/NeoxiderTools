using Neo.Animations;
using Neo.Samples.Survivor;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Bright, self-contained demo for the <b>Neo.Animations</b> animator components. A single world-space disc is
    ///     driven live by three animators at once — <see cref="Vector3Animator" /> pulses its scale,
    ///     <see cref="ColorAnimator" /> shifts its colour, and <see cref="FloatAnimator" /> spins it — all reading from the
    ///     shared <see cref="AnimationType" />. Buttons cycle the animation type and Play/Pause/Stop every animator in sync;
    ///     a slider sets their speed. Live readouts show the current float value and active type. Robust in an empty scene.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Animators Demo")]
    public sealed class AnimatorsDemoController : MonoBehaviour
    {
        private static readonly AnimationType[] Types =
        {
            AnimationType.Pulsing, AnimationType.SinWave, AnimationType.PerlinNoise,
            AnimationType.SmoothTransition, AnimationType.BounceEase, AnimationType.ElasticEase
        };

        private NeoDemoShell.Context _shell;
        private SpriteRenderer _sprite;
        private Vector3Animator _scale;
        private ColorAnimator _color;
        private FloatAnimator _spin;
        private TMP_Text _valueLabel;
        private TMP_Text _typeLabel;
        private int _typeIndex;

        private void Start()
        {
            _shell = NeoDemoShell.Build("Neo.Animations", new Color(0.55f, 0.45f, 0.95f));
            HideBackdrop();

            NeoDemoShell.ShowInfoCardOnce(
                "Neo.Animations",
                "One disc driven live by three animators sharing the same animation type.",
                "Vector3Animator pulses scale · ColorAnimator shifts colour · FloatAnimator spins it",
                "Next type cycles the shared AnimationType; Play/Pause/Stop drive all three",
                "Speed slider feeds every animator; the float readout is the live spin value");

            BuildTarget();

            _shell.AddSlider("Speed", 0f, 8f, 2f, SetSpeed);
            _shell.AddButtonRow(
                ("Next type", NextType),
                ("Play", PlayAll),
                ("Pause", PauseAll),
                ("Stop", StopAll));

            _typeLabel = _shell.AddValueLabel("Animation type");
            _valueLabel = _shell.AddValueLabel("Spin value");

            SetSpeed(2f);
            ApplyType();
            _shell.Log("Three animators bound to one disc — no imported art, no Animator asset.");
        }

        private void Update()
        {
            if (_sprite == null)
            {
                return;
            }

            // WHY: pull each animator's live output every frame so the demo works whether or not the
            // OnChanged UnityEvents are wired in the scene.
            _sprite.transform.localScale = _scale.CurrentVector;
            _sprite.color = _color.CurrentColor;
            _sprite.transform.localRotation = Quaternion.Euler(0f, 0f, _spin.CurrentValue);

            if (_valueLabel != null)
            {
                _valueLabel.text = _spin.CurrentValue.ToString("0.0");
            }
        }

        private void BuildTarget()
        {
            var go = new GameObject("Animated Disc");
            go.transform.SetParent(transform, false);
            go.transform.position = Vector3.zero;

            _sprite = go.AddComponent<SpriteRenderer>();
            _sprite.sprite = SurvivorArt.Disc;
            _sprite.color = new Color(0.55f, 0.45f, 0.95f);

            _scale = go.AddComponent<Vector3Animator>();
            _scale.playOnStart = true;
            _scale.startVector = new Vector3(1.4f, 1.4f, 1f);
            _scale.endVector = new Vector3(3.2f, 3.2f, 1f);

            _color = go.AddComponent<ColorAnimator>();
            _color.playOnStart = true;
            _color.startColor = new Color(0.55f, 0.45f, 0.95f);
            _color.endColor = new Color(1f, 0.55f, 0.30f);

            _spin = go.AddComponent<FloatAnimator>();
            _spin.playOnStart = true;
            _spin.minValue = 0f;
            _spin.maxValue = 360f;
        }

        private void SetSpeed(float v)
        {
            _scale.AnimationSpeed = v;
            _color.AnimationSpeed = v;
            _spin.AnimationSpeed = v;
        }

        private void NextType()
        {
            _typeIndex = (_typeIndex + 1) % Types.Length;
            ApplyType();
        }

        private void ApplyType()
        {
            AnimationType type = Types[_typeIndex];
            _scale.AnimationType = type;
            _color.AnimationType = type;
            _spin.AnimationType = type;
            if (_typeLabel != null)
            {
                _typeLabel.text = type.ToString();
            }

            _shell.Log($"Animation type → {type}");
        }

        private void PlayAll()
        {
            _scale.Play();
            _color.Play();
            _spin.Play();
            _shell.Log("Play all");
        }

        private void PauseAll()
        {
            _scale.Pause();
            _color.Pause();
            _spin.Pause();
            _shell.Log("Pause all");
        }

        private void StopAll()
        {
            _scale.Stop();
            _color.Stop();
            _spin.Stop();
            _shell.Log("Stop all");
        }

        private void HideBackdrop()
        {
            // WHY: the animated world-space disc lives behind the screen-space shell canvas, so hide the opaque
            // backdrop and let the camera clear colour read through (same trick as the Parallax demo).
            Transform backdrop = _shell.Canvas != null ? _shell.Canvas.transform.Find("Backdrop") : null;
            if (backdrop != null && backdrop.TryGetComponent(out UnityEngine.UI.Image img))
            {
                img.enabled = false;
            }
        }
    }
}
