using System;
using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     A <see cref="MouseInputManager" /> with an injected camera, a collider under the screen centre and
    ///     <c>subscribers</c> handlers on every Press/Hold/Release/Click event, both the <c>Action</c> and the
    ///     <c>in</c> flavour. Drives <see cref="MouseInputManager.ProcessMouseState" /> synchronously, so the measured
    ///     path is dispatch plus picking without the per-frame poll.
    /// </summary>
    internal sealed class InputMouseManagerRig : IDisposable
    {
        private readonly List<GameObject> _objects = new();
        private readonly bool _savedAutoCreate;
        private readonly GameObject _target;

        // WHY: RaycastHit.collider resolves the managed wrapper by instance id; keeping it strongly referenced
        // means a collection between warm-up and a measured window cannot make that lookup recreate it.
        private readonly BoxCollider _collider;
        private readonly Vector2 _screenPosition;
        private bool _disposed;

        internal InputMouseManagerRig(int subscribers, bool enableHold)
        {
            _savedAutoCreate = MouseInputManager.CreateInstance;
            MouseInputManager.CreateInstance = false;
            try
            {
                Camera camera = Create("Audit Input Rig Camera").AddComponent<Camera>();
                camera.transform.position = new Vector3(23456f, 23456f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                _screenPosition = camera.pixelRect.center;

                Manager = Create("Audit Input Rig Manager").AddComponent<MouseInputManager>();
                JsonUtility.FromJsonOverwrite("{\"useMainCameraFallback\":false}", Manager);
                Manager.SetTargetCamera(camera);
                Manager.enableHold = enableHold;

                _target = Create("Audit Input Rig Target");
                _target.transform.position = new Vector3(23456f, 23456f, 0f);
                _collider = _target.AddComponent<BoxCollider>();
                Physics.SyncTransforms();

                Subscribers = subscribers;
                for (int i = 0; i < subscribers; i++)
                {
                    Manager.OnPress += OnPress;
                    Manager.OnHold += OnHold;
                    Manager.OnRelease += OnRelease;
                    Manager.OnClick += OnClick;
                    Manager.OnPressIn += OnPressIn;
                    Manager.OnHoldIn += OnHoldIn;
                    Manager.OnReleaseIn += OnReleaseIn;
                    Manager.OnClickIn += OnClickIn;
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal MouseInputManager Manager { get; }

        internal int Subscribers { get; }

        internal int Presses { get; private set; }

        internal int Holds { get; private set; }

        internal int Releases { get; private set; }

        internal int Clicks { get; private set; }

        /// <summary>Events whose hit object was not the target collider.</summary>
        internal int Misses { get; private set; }

        internal void Press()
        {
            Manager.ProcessMouseState(_screenPosition, true, true, false);
        }

        /// <summary>One held frame; returns whether every subscriber observed exactly one Hold on the target.</summary>
        internal bool Hold()
        {
            int holds = Holds;
            int misses = Misses;
            Manager.ProcessMouseState(_screenPosition, false, true, false);
            return Holds - holds == 2 * Subscribers && Misses == misses;
        }

        /// <summary>
        ///     Press frame, held frame, release frame: one Press, two Holds, one Release and one Click per handler
        ///     when Hold is enabled. Returns whether every handler observed exactly that on the target.
        /// </summary>
        internal bool Cycle()
        {
            int presses = Presses;
            int holds = Holds;
            int releases = Releases;
            int clicks = Clicks;
            int misses = Misses;
            Manager.ProcessMouseState(_screenPosition, true, true, false);
            Manager.ProcessMouseState(_screenPosition, false, true, false);
            Manager.ProcessMouseState(_screenPosition, false, false, true);
            int perEvent = 2 * Subscribers;
            int expectedHolds = Manager.enableHold ? 2 * perEvent : 0;
            return Presses - presses == perEvent && Holds - holds == expectedHolds &&
                   Releases - releases == perEvent && Clicks - clicks == perEvent && Misses == misses;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            GC.KeepAlive(_collider);
            try
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }
            }
            finally
            {
                _objects.Clear();
                MouseInputManager.CreateInstance = _savedAutoCreate;
                MouseInputManager.ResetSubsystemPollingState();
            }
        }

        private GameObject Create(string objectName)
        {
            GameObject created = new(objectName);
            _objects.Add(created);
            return created;
        }

        private void Track(in MouseInputManager.MouseEventData data)
        {
            if (data.HitObject != _target)
            {
                Misses++;
            }
        }

        private void OnPress(MouseInputManager.MouseEventData data)
        {
            Presses++;
            Track(in data);
        }

        private void OnHold(MouseInputManager.MouseEventData data)
        {
            Holds++;
            Track(in data);
        }

        private void OnRelease(MouseInputManager.MouseEventData data)
        {
            Releases++;
            Track(in data);
        }

        private void OnClick(MouseInputManager.MouseEventData data)
        {
            Clicks++;
            Track(in data);
        }

        private void OnPressIn(in MouseInputManager.MouseEventData data)
        {
            Presses++;
            Track(in data);
        }

        private void OnHoldIn(in MouseInputManager.MouseEventData data)
        {
            Holds++;
            Track(in data);
        }

        private void OnReleaseIn(in MouseInputManager.MouseEventData data)
        {
            Releases++;
            Track(in data);
        }

        private void OnClickIn(in MouseInputManager.MouseEventData data)
        {
            Clicks++;
            Track(in data);
        }
    }
}
