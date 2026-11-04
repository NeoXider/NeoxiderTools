using System;
using System.Reflection;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    [TestFixture]
    public class StateMachineTests
    {
        #region Test States

        private class IdleState : IState
        {
            public bool Entered;
            public bool Exited;
            public int UpdateCount;
            public int FixedUpdateCount;
            public int LateUpdateCount;

            public void OnEnter()
            {
                Entered = true;
            }

            public void OnExit()
            {
                Exited = true;
            }

            public void OnUpdate()
            {
                UpdateCount++;
            }

            public void OnFixedUpdate()
            {
                FixedUpdateCount++;
            }

            public void OnLateUpdate()
            {
                LateUpdateCount++;
            }
        }

        private class RunState : IState
        {
            public bool Entered;
            public bool Exited;

            public void OnEnter()
            {
                Entered = true;
            }

            public void OnExit()
            {
                Exited = true;
            }

            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }

        private class AttackState : IState
        {
            public bool Entered;

            public void OnEnter()
            {
                Entered = true;
            }

            public void OnExit() { }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }

        private class ExitCountingState : IState
        {
            public int ExitCount;

            public void OnEnter() { }

            public void OnExit()
            {
                ExitCount++;
            }

            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }

        #endregion

        [Test]
        public void ChangeState_SetsCurrentAndPrevious()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IdleState idle = new IdleState();
            RunState run = new RunState();

            fsm.ChangeState(idle);
            Assert.AreEqual(idle, fsm.CurrentState);
            Assert.IsNull(fsm.PreviousState);

            fsm.ChangeState(run);
            Assert.AreEqual(run, fsm.CurrentState);
            Assert.AreEqual(idle, fsm.PreviousState);
        }

        [Test]
        public void ChangeState_CallsEnterAndExit()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IdleState idle = new IdleState();
            RunState run = new RunState();

            fsm.ChangeState(idle);
            Assert.IsTrue(idle.Entered);

            fsm.ChangeState(run);
            Assert.IsTrue(idle.Exited, "Previous state should have OnExit called.");
            Assert.IsTrue(run.Entered, "New state should have OnEnter called.");
        }

        [Test]
        public void ChangeState_SameState_DoesNothing()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IdleState idle = new IdleState();

            fsm.ChangeState(idle);
            idle.Entered = false;
            fsm.ChangeState(idle);
            Assert.IsFalse(idle.Entered, "OnEnter should NOT be called for same state.");
        }

        [Test]
        public void ChangeState_Null_DoesNotThrow()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            fsm.ChangeState(null);
            Assert.IsNull(fsm.CurrentState);
        }

        [Test]
        public void Stop_ExitsCurrentStateAndClearsCurrent()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            ExitCountingState state = new ExitCountingState();

            fsm.ChangeState(state);
            fsm.Stop();
            fsm.Stop();

            Assert.That(state.ExitCount, Is.EqualTo(1));
            Assert.That(fsm.CurrentState, Is.Null);
            Assert.That(fsm.PreviousState, Is.SameAs(state));
        }

        [Test]
        public void ChangeState_Generic_CreatesAndCaches()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            fsm.ChangeState<IdleState>();
            Assert.IsNotNull(fsm.CurrentState);
            Assert.IsInstanceOf<IdleState>(fsm.CurrentState);
        }

        [Test]
        public void GetOrCreateState_CachesInstance()
        {
            StateMachine<IState> fsm = new StateMachine<IState>(true);
            IdleState state1 = fsm.GetOrCreateState<IdleState>();
            IdleState state2 = fsm.GetOrCreateState<IdleState>();
            Assert.AreSame(state1, state2, "Cached state should return same instance.");
        }

        [Test]
        public void GetOrCreateState_NoCaching_CreatesFresh()
        {
            StateMachine<IState> fsm = new StateMachine<IState>(false);
            IdleState state1 = fsm.GetOrCreateState<IdleState>();
            IdleState state2 = fsm.GetOrCreateState<IdleState>();
            Assert.AreNotSame(state1, state2, "Without caching, should create new instances.");
        }

        [Test]
        public void Update_CallsCurrentStateOnUpdate()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IdleState idle = new IdleState();
            fsm.ChangeState(idle);

            fsm.Update();
            fsm.Update();
            fsm.Update();
            Assert.AreEqual(3, idle.UpdateCount);
        }

        [Test]
        public void FixedUpdate_CallsCurrentState()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IdleState idle = new IdleState();
            fsm.ChangeState(idle);

            fsm.FixedUpdate();
            Assert.AreEqual(1, idle.FixedUpdateCount);
        }

        [Test]
        public void LateUpdate_CallsCurrentState()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IdleState idle = new IdleState();
            fsm.ChangeState(idle);

            fsm.LateUpdate();
            Assert.AreEqual(1, idle.LateUpdateCount);
        }

        [Test]
        public void RegisterTransition_And_CanTransition()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            StateTransition transition = new StateTransition
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState),
                IsEnabled = true
            };

            fsm.RegisterTransition(transition);
            fsm.ChangeState<IdleState>();

            Assert.IsTrue(fsm.CanTransitionTo<RunState>());
            Assert.IsFalse(fsm.CanTransitionTo<AttackState>(), "No transition to AttackState registered.");
        }

        [Test]
        public void TryChangeState_Success()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            StateTransition transition = new StateTransition
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState),
                IsEnabled = true
            };

            fsm.RegisterTransition(transition);
            fsm.ChangeState<IdleState>();

            bool changed = fsm.TryChangeState<RunState>();
            Assert.IsTrue(changed);
            Assert.IsInstanceOf<RunState>(fsm.CurrentState);
        }

        [Test]
        public void TryChangeState_Failure_NoTransition()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            fsm.ChangeState<IdleState>();

            bool changed = fsm.TryChangeState<AttackState>();
            Assert.IsFalse(changed);
            Assert.IsInstanceOf<IdleState>(fsm.CurrentState);
        }

        [Test]
        public void UnregisterTransition_RemovesTransition()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            StateTransition transition = new StateTransition
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState),
                IsEnabled = true
            };

            fsm.RegisterTransition(transition);
            fsm.ChangeState<IdleState>();
            Assert.IsTrue(fsm.CanTransitionTo<RunState>());

            fsm.UnregisterTransition(transition);
            Assert.IsFalse(fsm.CanTransitionTo<RunState>());
        }

        [Test]
        public void EvaluateTransitions_ExecutesMatchingTransition()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            StateTransition transition = new StateTransition
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState),
                IsEnabled = true
            };

            fsm.RegisterTransition(transition);
            fsm.ChangeState<IdleState>();

            fsm.EvaluateTransitions();
            Assert.IsInstanceOf<RunState>(fsm.CurrentState, "Transition should have fired.");
        }

        [Test]
        public void EvaluateTransitions_DisabledTransition_DoesNotFire()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            StateTransition transition = new StateTransition
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState),
                IsEnabled = false
            };

            fsm.RegisterTransition(transition);
            fsm.ChangeState<IdleState>();

            fsm.EvaluateTransitions();
            Assert.IsInstanceOf<IdleState>(fsm.CurrentState, "Disabled transition should not fire.");
        }

        [Test]
        public void TransitionPriority_HigherPriorityFirst()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();

            StateTransition lowPriority = new StateTransition
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState),
                IsEnabled = true,
                Priority = 0
            };

            StateTransition highPriority = new StateTransition
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(AttackState),
                IsEnabled = true,
                Priority = 10
            };

            fsm.RegisterTransition(lowPriority);
            fsm.RegisterTransition(highPriority);
            fsm.ChangeState<IdleState>();

            fsm.EvaluateTransitions();
            Assert.IsInstanceOf<AttackState>(fsm.CurrentState, "Higher priority transition should fire first.");
        }

        [Test]
        public void ClearStateCache_ResetsCachedInstances()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IdleState first = fsm.GetOrCreateState<IdleState>();
            fsm.ClearStateCache();
            IdleState second = fsm.GetOrCreateState<IdleState>();

            Assert.AreNotSame(first, second, "After ClearStateCache, new instance should be created.");
        }

        [Test]
        public void OnStateChanged_Event_Fires()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IState eventFrom = null, eventTo = null;
            fsm.OnStateChanged.AddListener((from, to) =>
            {
                eventFrom = from;
                eventTo = to;
            });

            IdleState idle = new IdleState();
            RunState run = new RunState();
            fsm.ChangeState(idle);
            fsm.ChangeState(run);

            Assert.AreEqual(idle, eventFrom);
            Assert.AreEqual(run, eventTo);
        }

        [Test]
        public void OnStateEntered_Event_Fires()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IState lastEntered = null;
            fsm.OnStateEntered.AddListener(s => lastEntered = s);

            fsm.ChangeState<IdleState>();
            Assert.IsNotNull(lastEntered);
            Assert.IsInstanceOf<IdleState>(lastEntered);
        }

        [Test]
        public void OnStateExited_Event_Fires()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            IState lastExited = null;
            fsm.OnStateExited.AddListener(s => lastExited = s);

            IdleState idle = new IdleState();
            fsm.ChangeState(idle);
            fsm.ChangeState<RunState>();

            Assert.AreEqual(idle, lastExited);
        }

        [Test]
        public void GlobalTransition_FiresFromAnyState()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            StateTransition globalTransition = new StateTransition
            {
                FromStateType = null, // WHY: null FromStateType means the transition is global
                ToStateType = typeof(AttackState),
                IsEnabled = true
            };

            fsm.RegisterTransition(globalTransition);
            fsm.ChangeState<IdleState>();

            fsm.EvaluateTransitions();
            Assert.IsInstanceOf<AttackState>(fsm.CurrentState, "Global transition should fire from any state.");
        }

        [Test]
        public void TypedTransition_FiresWhenTransitionCachingDisabled()
        {
            // WHY: with caching off, RegisterTransition must not drop typed transitions.
            StateMachine<IState> fsm = new StateMachine<IState>(true, false);
            StateTransition transition = new StateTransition
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState),
                IsEnabled = true
            };

            fsm.RegisterTransition(transition);
            fsm.ChangeState<IdleState>();

            Assert.IsTrue(fsm.CanTransitionTo<RunState>(), "Typed transition must be visible with caching disabled.");

            fsm.EvaluateTransitions();
            Assert.IsInstanceOf<RunState>(fsm.CurrentState, "Typed transition should fire with caching disabled.");
        }

        [Test]
        public void CanTransitionTo_CachingDisabled_RespectsFromState()
        {
            // WHY: uncached typed transitions live in the global list; CanTransitionTo must still honor FromStateType.
            StateMachine<IState> fsm = new StateMachine<IState>(true, false);
            StateTransition transition = new StateTransition
            {
                FromStateType = typeof(RunState),
                ToStateType = typeof(AttackState),
                IsEnabled = true
            };

            fsm.RegisterTransition(transition);
            fsm.ChangeState<IdleState>();

            Assert.IsFalse(fsm.CanTransitionTo<AttackState>(),
                "Transition from RunState must not be reported while current state is IdleState.");
        }

        [Test]
        public void CanTransitionTo_NoCurrentState_ReturnsTrue()
        {
            StateMachine<IState> fsm = new StateMachine<IState>();
            Assert.IsTrue(fsm.CanTransitionTo<IdleState>(), "With no current state, any transition should be allowed.");
        }

        [Test]
        public void Behaviour_OnDisable_StopsCurrentState()
        {
            GameObject go = new GameObject("StateMachineHost");
            try
            {
                StateMachineBehaviourBase behaviour = go.AddComponent<StateMachineBehaviourBase>();
                behaviour.ChangeState<ExitCountingState>();

                ExitCountingState state = (ExitCountingState)behaviour.CurrentState;
                InvokePrivate(behaviour, "OnDisable");

                Assert.That(state.ExitCount, Is.EqualTo(1));
                Assert.That(behaviour.CurrentState, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ContextAction_UsesSceneObjectProvidedByComponentSlot()
        {
            GameObject target = new GameObject("StateMachineActionTarget");
            try
            {
                SetContextGameObjectActiveAction action = new SetContextGameObjectActiveAction
                {
                    ContextSlot = ConditionContextSlot.Owner,
                    SetActive = false
                };

                action.Execute(target);

                Assert.That(target.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }
    }
}
