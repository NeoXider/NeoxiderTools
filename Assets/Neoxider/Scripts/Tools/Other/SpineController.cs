#if SPINE_UNITY
using System.Collections.Generic;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class SpineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, FormerlySerializedAs("_skeletonAnim")] private SkeletonAnimation skeletonAnimation;
    [SerializeField, FormerlySerializedAs("_spineData")] private SkeletonDataAsset skeletonDataAsset;

    [Header("Animations")]
    [SerializeField] private bool autoPopulateAnimations = true;
    [SerializeField, FormerlySerializedAs("_animationName")] private List<string> animationNames = new();
    [SerializeField, Tooltip("Имя анимации, которая должна играть в режиме ожидания.")]
    private string defaultAnimationName = string.Empty;
    [SerializeField, FormerlySerializedAs("_idleId"), Tooltip("Индекс анимации по умолчанию в списке (используется, если имя не задано)."), Min(0)]
    private int defaultAnimationIndex;
    [SerializeField, Tooltip("Проигрывать анимацию по умолчанию при включении компонента.")]
    private bool playDefaultOnEnable = true;
    [SerializeField, Tooltip("Автоматически возвращаться к анимации по умолчанию после завершения нецикличной анимации.")]
    private bool queueDefaultAfterNonLooping = true;

    [Header("Skins")]
    [SerializeField] private bool autoPopulateSkins = true;
    [SerializeField, FormerlySerializedAs("_skinNames")] private List<string> skinNames = new();
    [SerializeField, FormerlySerializedAs("startSkin"), Tooltip("Индекс скина по умолчанию в списке.")]
    private int defaultSkinIndex;
    [SerializeField, Tooltip("Сохранять выбранный скин в PlayerPrefs.")]
    private bool persistSkinSelection = true;
    [SerializeField, FormerlySerializedAs("_keySaveSkin")] private string skinPrefsKey = "SkinChanger";
    [SerializeField, Tooltip("Дополнительный сдвиг индекса (полезно, если первый скин в списке служебный)."), FormerlySerializedAs("skinIndexOffset")] private int skinIndexOffset;
    [SerializeField, FormerlySerializedAs("addId"), Tooltip("Устаревший переключатель для смещения индекса. Используйте 'Skin Index Offset'.")]
    private bool legacyAddIndex;

    [Header("Events")]
    public UnityEvent OnSwapSkin;

    public IReadOnlyList<string> Animations => animationNames;
    public IReadOnlyList<string> Skins => skinNames;
    public SkeletonAnimation SkeletonAnimation => skeletonAnimation;
    public string CurrentAnimationName => currentEntry?.Animation?.Name ?? defaultAnimationName;
    public int CurrentSkinIndex { get; private set; } = -1;
    public string CurrentSkinName { get; private set; }

    private bool initialized;
    private TrackEntry currentEntry;

    private void Reset()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        skeletonDataAsset = skeletonAnimation != null ? skeletonAnimation.SkeletonDataAsset : null;
        autoPopulateAnimations = true;
        autoPopulateSkins = true;
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();

        if (playDefaultOnEnable && !string.IsNullOrEmpty(DefaultAnimation))
        {
            Play(DefaultAnimation, true, 0f, false);
        }
    }

    private void Start()
    {
        EnsureInitialized();
        ApplyInitialSkin();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEntryEvents();
    }

    private void OnValidate()
    {
        skeletonAnimation ??= GetComponent<SkeletonAnimation>();
        skeletonDataAsset ??= skeletonAnimation != null ? skeletonAnimation.SkeletonDataAsset : skeletonDataAsset;

        if (!Application.isPlaying)
        {
            PopulateLookups();
            NormalizeDefaultAnimation();
        }
    }

    #region Animation API

    public TrackEntry Play(int animationIndex, bool loop = false, float mixDuration = 0f, bool queueDefault = true)
    {
        if (!IsValidIndex(animationIndex, animationNames.Count))
        {
            Debug.LogWarning($"[{nameof(SpineController)}] Animation index '{animationIndex}' is out of range.", this);
            return null;
        }

        return Play(animationNames[animationIndex], loop, mixDuration, queueDefault);
    }

    public void PlayAnimationByIndex(int animationIndex)
    {
        Play(animationIndex, false);
    }

    public void PlayAnimationLoopByIndex(int animationIndex)
    {
        Play(animationIndex, true);
    }

    public void PlayAnimationByName(string animationName)
    {
        Play(animationName, false);
    }

    public void PlayAnimationLoopByName(string animationName)
    {
        Play(animationName, true);
    }

    public TrackEntry Play(string animationName, bool loop = false, float mixDuration = 0f, bool queueDefault = true)
    {
        EnsureInitialized();
        if (skeletonAnimation == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"[{nameof(SpineController)}] Animation name is empty.", this);
            return null;
        }

        var state = skeletonAnimation.AnimationState;
        if (state == null)
        {
            Debug.LogWarning($"[{nameof(SpineController)}] Animation state is missing.", this);
            return null;
        }

        UnsubscribeFromEntryEvents();

        currentEntry = state.SetAnimation(0, animationName, loop);
        if (currentEntry != null && mixDuration > 0f)
        {
            currentEntry.MixDuration = mixDuration;
        }

        if (currentEntry != null && !loop && queueDefault && queueDefaultAfterNonLooping && !string.IsNullOrEmpty(DefaultAnimation))
        {
            currentEntry.Complete += HandleTrackComplete;
        }

        return currentEntry;
    }

    public void PlayDefault(bool forceRestart = false)
    {
        if (string.IsNullOrEmpty(DefaultAnimation))
        {
            return;
        }

        if (!forceRestart && string.Equals(CurrentAnimationName, DefaultAnimation, StringComparison.Ordinal))
        {
            return;
        }

        Play(DefaultAnimation, true, 0f, false);
    }

    public void PlayDefault()
    {
        PlayDefault(false);
    }

    public void PlayDefaultForced()
    {
        PlayDefault(true);
    }

    public void SetDefaultAnimationByIndex(int animationIndex, bool playImmediately = true)
    {
        if (!IsValidIndex(animationIndex, animationNames.Count))
        {
            Debug.LogWarning($"[{nameof(SpineController)}] Cannot set idle animation by index '{animationIndex}'.", this);
            return;
        }

        defaultAnimationIndex = animationIndex;
        defaultAnimationName = animationNames[animationIndex];

        if (playImmediately)
        {
            PlayDefault(true);
        }
    }

    public void SetDefaultAnimationByIndex(int animationIndex)
    {
        SetDefaultAnimationByIndex(animationIndex, true);
    }

    public void SetDefaultAnimation(string animationName, bool playImmediately = true)
    {
        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"[{nameof(SpineController)}] Default animation name is empty.", this);
            return;
        }

        defaultAnimationName = animationName;
        defaultAnimationIndex = animationNames.IndexOf(animationName);

        if (playImmediately)
        {
            PlayDefault(true);
        }
    }

    public void SetDefaultAnimation(string animationName)
    {
        SetDefaultAnimation(animationName, true);
    }

    public void Stop()
    {
        if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
        {
            return;
        }

        UnsubscribeFromEntryEvents();
        skeletonAnimation.AnimationState.ClearTracks();
    }

    #endregion

    #region Skin API

    public void SetSkinByIndex(int skinIndex)
    {
        SetSkinByIndex(skinIndex, persistSkinSelection);
    }

    public void SetSkinByIndex(int skinIndex, bool persist)
    {
        if (!EnsureSkinAvailable())
        {
            return;
        }

        int logicalCount = GetLogicalSkinCount();
        if (logicalCount == 0)
        {
            return;
        }

        skinIndex = Mathf.Clamp(skinIndex, 0, logicalCount - 1);
        int actualIndex = Mathf.Clamp(skinIndex + GetIndexOffset(), 0, skinNames.Count - 1);
        string skinName = skinNames[actualIndex];

        ApplySkinInternal(skinName);
        CurrentSkinIndex = skinIndex;
        CurrentSkinName = skinName;

        if (persist && persistSkinSelection)
        {
            PlayerPrefs.SetInt(skinPrefsKey, skinIndex);
        }

        OnSwapSkin?.Invoke();
    }

    public void SetSkin(string skinName)
    {
        SetSkin(skinName, persistSkinSelection);
    }

    public void SetSkin(string skinName, bool persist)
    {
        if (!EnsureSkinAvailable())
        {
            return;
        }

        int logicalIndex = GetLogicalSkinIndex(skinName);
        if (logicalIndex < 0)
        {
            Debug.LogWarning($"[{nameof(SpineController)}] Skin '{skinName}' not found.", this);
            return;
        }

        SetSkinByIndex(logicalIndex, persist);
    }

    public void NextSkin()
    {
        if (!EnsureSkinAvailable()) return;
        int logicalCount = GetLogicalSkinCount();
        if (logicalCount == 0) return;
        int nextIndex = (CurrentSkinIndex + 1 + logicalCount) % logicalCount;
        SetSkinByIndex(nextIndex);
    }

    public void PreviousSkin()
    {
        if (!EnsureSkinAvailable()) return;
        int logicalCount = GetLogicalSkinCount();
        if (logicalCount == 0) return;
        int previousIndex = (CurrentSkinIndex - 1 + logicalCount) % logicalCount;
        SetSkinByIndex(previousIndex);
    }

    #endregion

    #region Helpers

    private void ApplyInitialSkin()
    {
        if (!EnsureSkinAvailable())
        {
            return;
        }

        int skinIndex =
 persistSkinSelection ? PlayerPrefs.GetInt(skinPrefsKey, GetDefaultSkinIndex()) : GetDefaultSkinIndex();
        SetSkinByIndex(skinIndex, false);
    }

    private void ApplySkinInternal(string skinName)
    {
        EnsureInitialized();
        if (skeletonAnimation == null)
        {
            return;
        }

        var skeleton = skeletonAnimation.Skeleton;
        if (skeleton == null)
        {
            skeletonAnimation.Initialize(true);
            skeleton = skeletonAnimation.Skeleton;
        }

        var skeletonData = skeleton?.Data ?? skeletonDataAsset?.GetSkeletonData(true);
        if (skeletonData == null)
        {
            Debug.LogWarning($"[{nameof(SpineController)}] Unable to access skeleton data.", this);
            return;
        }

        var skin = skeletonData.FindSkin(skinName);
        if (skin == null)
        {
            Debug.LogWarning($"[{nameof(SpineController)}] Skin '{skinName}' not found in skeleton data.", this);
            return;
        }

        skeleton.SetSkin(skin);
        skeleton.SetSlotsToSetupPose();
        skeletonAnimation.AnimationState.Apply(skeleton);
    }

    private void HandleTrackComplete(TrackEntry trackEntry)
    {
        trackEntry.Complete -= HandleTrackComplete;
        if (!queueDefaultAfterNonLooping || string.IsNullOrEmpty(DefaultAnimation))
        {
            return;
        }
        Play(DefaultAnimation, true, 0f, false);
    }

    private void UnsubscribeFromEntryEvents()
    {
        if (currentEntry != null)
        {
            currentEntry.Complete -= HandleTrackComplete;
            currentEntry = null;
        }
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        if (skeletonAnimation == null)
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        if (skeletonAnimation == null)
        {
            Debug.LogWarning($"[{nameof(SpineController)}] {nameof(SkeletonAnimation)} reference is missing.", this);
            return;
        }

        skeletonDataAsset ??= skeletonAnimation.SkeletonDataAsset;

        if (skeletonAnimation.Skeleton == null)
        {
            skeletonAnimation.Initialize(true);
        }

        PopulateLookups();
        NormalizeDefaultAnimation();

        initialized = true;
    }

    private void PopulateLookups()
    {
        var data = skeletonAnimation != null && skeletonAnimation.Skeleton != null
            ? skeletonAnimation.Skeleton.Data
            : skeletonDataAsset != null ? skeletonDataAsset.GetSkeletonData(false) : null;

        if (data == null)
        {
            return;
        }

        if (autoPopulateAnimations)
        {
            animationNames = data.Animations.Select(a => a.Name).Distinct().ToList();
        }

        if (autoPopulateSkins)
        {
            skinNames = data.Skins.Select(s => s.Name).Distinct().ToList();
        }
    }

    private void NormalizeDefaultAnimation()
    {
        if (animationNames == null || animationNames.Count == 0)
        {
            defaultAnimationName = string.Empty;
            defaultAnimationIndex = 0;
            return;
        }

        if (string.IsNullOrEmpty(defaultAnimationName) || !animationNames.Contains(defaultAnimationName))
        {
            defaultAnimationIndex = Mathf.Clamp(defaultAnimationIndex, 0, animationNames.Count - 1);
            defaultAnimationName = animationNames[defaultAnimationIndex];
        }
        else
        {
            defaultAnimationIndex = animationNames.IndexOf(defaultAnimationName);
        }
    }

    private string DefaultAnimation => defaultAnimationName;

    private int GetDefaultSkinIndex()
    {
        int logicalCount = GetLogicalSkinCount();
        if (logicalCount == 0)
        {
            return 0;
        }

        return Mathf.Clamp(defaultSkinIndex, 0, logicalCount - 1);
    }

    private int GetLogicalSkinCount()
    {
        if (skinNames == null)
        {
            return 0;
        }

        return Mathf.Max(0, skinNames.Count - GetIndexOffset());
    }

    private int GetIndexOffset()
    {
        return skinIndexOffset + (legacyAddIndex ? 1 : 0);
    }

    private int GetLogicalSkinIndex(string skinName)
    {
        if (skinNames == null)
        {
            return -1;
        }

        int actualIndex = skinNames.IndexOf(skinName);
        if (actualIndex < 0)
        {
            return -1;
        }

        return actualIndex - GetIndexOffset();
    }

    private bool EnsureSkinAvailable()
    {
        EnsureInitialized();
        if (skinNames == null || skinNames.Count == 0)
        {
            Debug.LogWarning($"[{nameof(SpineController)}] No skins available for skeleton.", this);
            return false;
        }
        return true;
    }

    private static bool IsValidIndex(int index, int collectionCount)
    {
        return index >= 0 && index < collectionCount;
    }

    #endregion
}
#else
using UnityEngine;

public sealed class SpineController : MonoBehaviour
{
    [SerializeField]
    private string message = "Spine package is missing. Install Spine Unity Runtime to use SpineController.";

    private void Awake()
    {
        Debug.LogWarning(message, this);
        enabled = false;
    }
}
#endif