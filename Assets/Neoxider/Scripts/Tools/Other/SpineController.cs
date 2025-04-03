// using Spine.Unity;
// using System.Linq;
// using UnityEngine;
// using UnityEngine.Events;
//
// public class SpineController : MonoBehaviour
// {
//     [SerializeField] private SkeletonDataAsset _spineData;
//     [SerializeField] private SkeletonAnimation _skeletonAnim;
//     [SerializeField] private string[] _animationName;
//     [SerializeField] private bool _setAnimationName = true;
//     [SerializeField] private string[] _skinNames;
//     [SerializeField] private bool _setSkinNames = true;
//     [SerializeField] private int _idleId = 0;
//     [SerializeField] private string _keySaveSkin = "SkinChanger";
//
//     public int currentSkinIndex = 0;
//     public int startSkin = 0;
//     public bool addId = false;
//
//     public int idleAnim = 0;
//     public UnityEvent OnSwapSkin;
//
//     private float _currTime;
//     private bool _animation;
//     private Spine.TrackEntry _entry;
//     private bool _idleChange = false;
//
//     public int EquipSkin
//     {
//         get => PlayerPrefs.GetInt(_keySaveSkin, startSkin);
//         set
//         {
//             PlayerPrefs.SetInt(_keySaveSkin, value);
//             SetSkinId(value);
//             OnSwapSkin?.Invoke();
//         }
//     }
//
//     private void Start()
//     {
//         SetSkinId(EquipSkin);
//     }
//
//     public void Update()
//     {
//         if (_animation == false)
//         {
//             return;
//         }
//
//         _currTime += Time.deltaTime;
//         if (!_idleChange)
//         {
//             if (_entry != null)
//             {
//                 if (_currTime < _entry.AnimationEnd)
//                 {
//                     return;
//                 }
//             }
//         }
//         else
//             _idleChange = false;
//
//         _animation = false;
//         SetAnimation(_animationName[_idleId], true);
//     }
//
//     public void SetAnimation(string aniName, bool loop = false)
//     {
//         _currTime = 0;
//         _animation = true;
//         _idleChange = false;
//         _skeletonAnim.state.ClearTracks();
//         _skeletonAnim.skeleton.SetToSetupPose();
//         _entry = _skeletonAnim.state.SetAnimation(0, aniName, loop);
//     }
//
//     public void SetIdleAnimationId(int id)
//     {
//         _idleId = id;
//         _idleChange = true;
//         _animation = true;
//     }
//
//     public void SetAnimation(int id)
//     {
//         SetAnimation(_animationName[id], false);
//     }
//
//     public void SetAnimation(int id, bool loop = false)
//     {
//         SetAnimation(_animationName[id], loop);
//     }
//
//     public void SetAnimationLoop(int id)
//     {
//         SetAnimation(_animationName[id], true);
//     }
//
//     public void ChangeSkin(string skinName)
//     {
//         _skeletonAnim.skeleton.SetSkin(skinName);
//         _skeletonAnim.skeleton.SetToSetupPose();
//     }
//
//     public void SetSkinId(int id)
//     {
//         currentSkinIndex = id;
//         SetSkin(_skinNames[id + (addId ? 1 : 0)]);
//     }
//
//     public void SetSkin(string skinName)
//     {
//         if (_skeletonAnim.Skeleton != null)
//         {
//             ChangeSkin(skinName);
//         }
//     }
//
//     private void OnValidate()
//     {
//         _skeletonAnim ??= GetComponent<SkeletonAnimation>();
//
//         if (_setAnimationName)
//             _animationName = _spineData.GetSkeletonData(true).Animations.Select(s => s.Name).ToArray();
//
//         if (_setSkinNames)
//             _skinNames = _spineData.GetSkeletonData(true).Skins.Select(s => s.Name).ToArray();
//
//         SetSkinId(currentSkinIndex);
//     }
// }

