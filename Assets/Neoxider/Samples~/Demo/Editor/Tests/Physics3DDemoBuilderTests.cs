using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Edit-mode contract of the Physics 3D demo builder on an empty scene: the three stations (MagneticField,
    ///     ImpulseZone trigger pad, manual ExplosiveForce) stand on dynamic Rigidbodies, the perspective camera and the
    ///     light are authored, the controls call the components through persistent listeners, and a second build
    ///     changes nothing. Physics types are reached by name, so this assembly needs no extra module references.
    /// </summary>
    public sealed class Physics3DDemoBuilderTests
    {
        private const string BuilderId = "Physics3DDemoBuilder.Build";

        // WHY: 8 steel balls around the magnet, 3 balls above the pad and a 6-crate pyramid.
        private const int ExpectedBodies = 17;
        private const int PadBallCount = 3;
        private const string PadBallPrefix = "Ball · ";

        // WHY: mirrors Physics3DDemoBuilder.MegaBlastForce, which is internal to Neo.Demo.Editor.
        private const float MegaBlastForce = 18f;

        private string _folder;
        private Scene _scene;
        private Scene _previousActive;

        [SetUp]
        public void SetUp()
        {
            _previousActive = SceneManager.GetActiveScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            if (_previousActive.IsValid() && _previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActive);
            }

            if (!string.IsNullOrEmpty(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
                _folder = null;
            }
        }

        [Test]
        public void Builder_AuthorsThreeStationsOnDynamicBodies()
        {
            BuildEmptyScene();

            Component pad = SingleByTypeName("ImpulseZone");
            BoxCollider trigger = pad.GetComponent<BoxCollider>();
            Assert.That(trigger, Is.Not.Null, "ImpulseZone needs a collider on its own object.");
            Assert.That(trigger.isTrigger, Is.True);
            SerializedObject padData = new(pad);
            Assert.That(padData.FindProperty("impulseForce").floatValue, Is.GreaterThan(0f));
            Assert.That(padData.FindProperty("direction").enumNames[padData.FindProperty("direction").enumValueIndex],
                Is.EqualTo("Custom"));

            SerializedObject mine = new(SingleByTypeName("ExplosiveForce"));
            Assert.That(mine.FindProperty("activationMode").enumNames[mine.FindProperty("activationMode").enumValueIndex],
                Is.EqualTo("Manual"));
            Assert.That(mine.FindProperty("destroyAfterExplosion").boolValue, Is.False);

            SerializedObject magnet = new(SingleByTypeName("MagneticField"));
            Assert.That(magnet.FindProperty("_toggle").boolValue, Is.True, "The magnet pulses through Toggle phases.");
            Assert.That(magnet.FindProperty("radius").floatValue, Is.GreaterThan(0f));

            List<Rigidbody> bodies = All<Rigidbody>();
            Assert.That(bodies.Count, Is.EqualTo(ExpectedBodies));
            foreach (Rigidbody body in bodies)
            {
                Assert.That(body.isKinematic, Is.False, body.name);
                Assert.That(body.useGravity, Is.True, body.name);
                Assert.That(body.GetComponent<Collider>(), Is.Not.Null, body.name);
            }

            Camera camera = Single<Camera>();
            Assert.That(camera.orthographic, Is.False, "The arena is shown through a perspective camera.");
            Assert.That(All<Light>().Exists(light => light.type == LightType.Directional), Is.True);

            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.ModuleName, Does.Contain("Physics"));
        }

        [Test]
        public void Builder_KeepsThePadBallsAbleToReenterTheTrigger()
        {
            BuildEmptyScene();

            BoxCollider trigger = SingleByTypeName("ImpulseZone").GetComponent<BoxCollider>();
            BoxCollider plate = Named<BoxCollider>("Bounce Plate");
            Assert.That(plate.isTrigger, Is.False);
            PhysicsMaterial bounce = plate.sharedMaterial;
            Assert.That(bounce, Is.Not.Null, "The bounce plate needs its elastic physics material.");
            Assert.That(bounce.bounciness, Is.GreaterThanOrEqualTo(0.99f),
                "Only a fully elastic plate returns a ball to the field it fell from.");
            Assert.That(bounce.bounceCombine, Is.EqualTo(PhysicsMaterialCombine.Maximum),
                "Maximum outranks the balls' default material, so the contact stays elastic.");

            // WHY: ImpulseZone pushes only on OnTriggerEnter: a ball resting on the plate must lie fully below the
            // trigger, and a fall from the trigger must hit the plate faster than the bounce threshold to bounce back.
            float gravity = Physics.gravity.magnitude;
            float minGap = Physics.bounceThreshold * Physics.bounceThreshold / (2f * gravity);
            float plateTop = plate.transform.TransformPoint(plate.center + Vector3.up * (plate.size.y * 0.5f)).y;
            float fieldBottom = trigger.transform.TransformPoint(trigger.center - Vector3.up * (trigger.size.y * 0.5f)).y;
            float fieldTop = trigger.transform.TransformPoint(trigger.center + Vector3.up * (trigger.size.y * 0.5f)).y;

            List<Rigidbody> padBalls = All<Rigidbody>().FindAll(body => body.name.StartsWith(PadBallPrefix,
                StringComparison.Ordinal));
            Assert.That(padBalls.Count, Is.EqualTo(PadBallCount));
            foreach (Rigidbody ball in padBalls)
            {
                SphereCollider sphere = ball.GetComponent<SphereCollider>();
                Assert.That(sphere, Is.Not.Null, ball.name);
                Vector3 scale = ball.transform.lossyScale;
                float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                Assert.That(fieldBottom - plateTop - 2f * radius, Is.GreaterThan(minGap), ball.name);
                Assert.That(ball.transform.position.y - radius, Is.GreaterThan(fieldTop),
                    ball.name + " starts above the field, so its first contact is a trigger enter.");
                Assert.That(ball.linearDamping, Is.Zero, ball.name + " keeps its energy between bounces.");
            }
        }

        [Test]
        public void Builder_WiresTheControlsToTheComponents()
        {
            BuildEmptyScene();

            Component pad = SingleByTypeName("ImpulseZone");
            Component mine = SingleByTypeName("ExplosiveForce");
            Component magnet = SingleByTypeName("MagneticField");

            Button detonate = FindButton("Btn_Detonate");
            Button mega = FindButton("Btn_Mega blast");
            Assert.That(DemoWiring.Describe(detonate.onClick), Is.EqualTo("ExplosiveForce.Explode"));
            Assert.That(DemoWiring.Describe(mega.onClick), Is.EqualTo("ExplosiveForce.Explode"));
            Assert.That(FirstCall(detonate).FindPropertyRelative("m_Mode").intValue,
                Is.EqualTo((int)PersistentListenerMode.Void), "Detonate calls Explode() with the base force.");
            SerializedProperty megaCall = FirstCall(mega);
            Assert.That(megaCall.FindPropertyRelative("m_Mode").intValue, Is.EqualTo((int)PersistentListenerMode.Float),
                "Mega blast calls the Explode(float) overload.");
            Assert.That(megaCall.FindPropertyRelative("m_Arguments.m_FloatArgument").floatValue,
                Is.EqualTo(MegaBlastForce));
            Assert.That(DemoWiring.Describe(Event(mine, "OnExplode")), Is.EqualTo("DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(Event(mine, "OnObjectAffected")), Is.EqualTo("DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(Event(pad, "OnImpulseApplied")), Is.EqualTo("DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(Event(magnet, "OnModeChanged")), Is.EqualTo("DemoValueReadout.SetBool"));

            Button kick = FindButton("Btn_Kick the balls");
            Assert.That(DemoWiring.Describe(kick.onClick),
                Is.EqualTo("ImpulseZone.ApplyImpulseToObject; ImpulseZone.ApplyImpulseToObject; " +
                           "ImpulseZone.ApplyImpulseToObject; DemoLogFeed.Log"));
            SerializedProperty calls = new SerializedObject(kick).FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            HashSet<Object> kicked = new();
            for (int i = 0; i < 3; i++)
            {
                Object target = calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_ObjectArgument")
                    .objectReferenceValue;
                GameObject ball = target as GameObject;
                Assert.That(ball, Is.Not.Null, "Kick call " + i + " must pass a ball GameObject.");
                Assert.That(ball.GetComponent<Rigidbody>(), Is.Not.Null, ball.name);
                kicked.Add(ball);
            }

            Assert.That(kicked.Count, Is.EqualTo(3), "Each kick call targets a different ball.");

            Toggle field = Single<Toggle>();
            Assert.That(DemoWiring.Describe(field.onValueChanged), Is.EqualTo("MagneticField.set_enabled"));
            Assert.That(field.isOn, Is.True);

            List<string> sliders = new();
            foreach (Slider slider in All<Slider>())
            {
                sliders.Add(DemoWiring.Describe(slider.onValueChanged));
            }

            string described = string.Join(" | ", sliders);
            Assert.That(sliders.Exists(line => line.Contains("MagneticField.SetStrength")), Is.True, described);
            Assert.That(sliders.Exists(line => line.Contains("ImpulseZone.SetImpulseForce")), Is.True, described);
        }

        [Test]
        public void Builder_RebuildsWithoutChanges()
        {
            DemoSceneResult first = BuildEmptyScene();
            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { Builder() });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(All<Rigidbody>().Count, Is.EqualTo(ExpectedBodies));
        }

        private DemoSceneResult BuildEmptyScene()
        {
            DemoSceneBuilderInfo builder = Builder();
            _scene = DemoTestScenes.OpenEmpty("__NeoPhysics3DDemoTests", out _folder);
            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(result.Success, Is.True, result.Summary());
            return result;
        }

        private static DemoSceneBuilderInfo Builder()
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(BuilderId, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, BuilderId + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private static UnityEventBase Event(Component owner, string member)
        {
            FieldInfo field = owner.GetType().GetField(member, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, owner.GetType().Name + "." + member);
            return (UnityEventBase)field.GetValue(owner);
        }

        private static SerializedProperty FirstCall(Button button)
        {
            SerializedProperty calls = new SerializedObject(button).FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            Assert.That(calls.arraySize, Is.GreaterThan(0), button.name);
            return calls.GetArrayElementAtIndex(0);
        }

        private T Named<T>(string name) where T : Component
        {
            List<T> found = All<T>().FindAll(component => component.name == name);
            Assert.That(found.Count, Is.EqualTo(1), typeof(T).Name + " '" + name + "'");
            return found[0];
        }

        private Button FindButton(string name)
        {
            foreach (Button button in All<Button>())
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail("Button '" + name + "' not found.");
            return null;
        }

        private List<T> All<T>() where T : Component
        {
            List<T> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return found;
        }

        private T Single<T>() where T : Component
        {
            List<T> found = All<T>();
            Assert.That(found.Count, Is.EqualTo(1), typeof(T).Name);
            return found[0];
        }

        private Component SingleByTypeName(string typeName)
        {
            List<Component> found = new();
            foreach (MonoBehaviour behaviour in All<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().Name == typeName)
                {
                    found.Add(behaviour);
                }
            }

            Assert.That(found.Count, Is.EqualTo(1), typeName);
            return found[0];
        }
    }
}
