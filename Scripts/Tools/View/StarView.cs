using Neo.Reactive;
using Neo.UI;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Shows a star count (from <see cref="ScoreManager.CountStars" /> or set directly) by switching ordered
    ///     GameObjects and/or <see cref="VisualToggle" /> slots: the first <c>count</c> slots on, the rest off.
    /// </summary>
    [NeoDoc("Tools/View/StarView.md")]
    [CreateFromMenu("Neoxider/Tools/View/StarView")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(StarView))]
    public class StarView : MonoBehaviour
    {
        [Header("GameObjects")]
        [Tooltip("Ordered star objects switched with SetActive; slot i is on while i < count.")]
        public GameObject[] starObjects;

        [Tooltip("Score source whose CountStars is shown. While empty, the inspector fills it with the scene's " +
                 "first ScoreManager (FindInScene, when attribute search is on). ShowStars works either way, but a " +
                 "bound manager overrides it on its next CountStars change; to drive the view only through " +
                 "ShowStars, call Bind(null) at runtime.")]
        [FindInScene]
        public ScoreManager scoreManager;

        [Space]
        [Header("VisualToggle")]
        [Tooltip("Ordered VisualToggle slots, switched through their SetActive(bool).")]
        public VisualToggle[] stars;

        private ReactivePropertyInt _subscribedStars;

        /// <summary>
        ///     Binds to <paramref name="manager" /> (replacing any earlier binding) and shows its current count.
        ///     Null only unbinds and leaves the visuals as they are.
        /// </summary>
        public void Bind(ScoreManager manager)
        {
            scoreManager = manager;
            Awake();
        }

        /// <summary>Shows <paramref name="count" /> stars directly, for views that are not bound to a score.</summary>
        public void ShowStars(int count)
        {
            OnStarChange(count);
        }

        /// <summary>Binds to <see cref="scoreManager" />; public so an active view can be rebound after the field changes.</summary>
        public void Awake()
        {
            _subscribedStars?.RemoveListener(OnStarChange);
            _subscribedStars = null;
            if (scoreManager == null)
            {
                return;
            }

            _subscribedStars = scoreManager.CountStarsReactive;
            _subscribedStars?.AddListener(OnStarChange);
            OnStarChange(scoreManager.CountStars);
        }

        private void OnDestroy()
        {
            _subscribedStars?.RemoveListener(OnStarChange);
            _subscribedStars = null;
        }

        [Button]
        private void OnStarChange(int count)
        {
            if (stars != null)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i] != null)
                    {
                        stars[i].SetActive(i < count);
                    }
                }
            }

            if (starObjects != null)
            {
                for (int i = 0; i < starObjects.Length; i++)
                {
                    if (starObjects[i] != null)
                    {
                        starObjects[i].SetActive(i < count);
                    }
                }
            }
        }
    }
}
