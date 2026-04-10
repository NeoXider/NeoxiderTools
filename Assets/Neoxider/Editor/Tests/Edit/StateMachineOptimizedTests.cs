using System;
using Neo.StateMachine;
using NUnit.Framework;

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

        #endregion

        [Test]
        public void ChangeState_SetsCurrentAndPrevious()
        {
            var fsm = new StateMachine<IState>();
            var idle = new IdleState();
            var run = new RunState();

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
            var fsm = new StateMachine<IState>();
            var idle = new IdleState();
            var run = new RunState();

            fsm.ChangeState(idle);
            Assert.IsTrue(idle.Entered);

            fsm.ChangeState(run);
            Assert.IsTrue(idle.Exited, "Previous state should have OnExit called.");
            Assert.IsTrue(run.Entered, "New state should have OnEnter called.");
        }

        [Test]
        public void ChangeState_SameState_DoesNothing()
        {
            var fsm = new StateMachine<IState>();
            var idle = new IdleState();

            fsm.ChangeState(idle);
            idle.Entered = false; // Reset
            fsm.ChangeState(idle); // Same state
            Assert.IsFalse(idle.Entered, "OnEnter should NOT be called for same state.");
        }

        [Test]
        public void ChangeState_Null_DoesNotThrow()
        {
            var fsm = new StateMachine<IState>();
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,
                new System.Text.RegularExpressions.Regex("null state"));
            fsm.ChangeState(null);
            Assert.IsNull(fsm.CurrentState);
        }

        [Test]
        public void ChangeState_Generic_CreatesAndCaches()
        {
            var fsm = new StateMachine<IState>();
            fsm.ChangeState<IdleState>();
            Assert.IsNotNull(fsm.CurrentState);
            Assert.IsInstanceOf<IdleState>(fsm.CurrentState);
        }

        [Test]
        public void GetOrCreateState_CachesInstance()
        {
            var fsm = new StateMachine<IState>(true);
            IdleState state1 = fsm.GetOrCreateState<IdleState>();
            IdleState state2 = fsm.GetOrCreateState<IdleState>();
            Assert.AreSame(state1, state2, "Cached state should return same instance.");
        }

        [Test]
        public void GetOrCreateState_NoCaching_CreatesFresh()
        {
            var fsm = new StateMachine<IState>(false);
            IdleState state1 = fsm.GetOrCreateState<IdleState>();
            IdleState state2 = fsm.GetOrCreateState<IdleState>();
            Assert.AreNotSame(state1, state2, "Without caching, should create new instances.");
        }

        [Test]
        public void Update_CallsCurrentStateOnUpdate()
        {
            var fsm = new StateMachine<IState>();
            var idle = new IdleState();
            fsm.ChangeState(idle);

            fsm.Update();
            fsm.Update();
            fsm.Update();
            Assert.AreEqual(3, idle.UpdateCount);
        }

        [Test]
        public void FixedUpdate_CallsCurrentState()
        {
            var fsm = new StateMachine<IState>();
            var idle = new IdleState();
            fsm.ChangeState(idle);

            fsm.FixedUpdate();
            Assert.AreEqual(1, idle.FixedUpdateCount);
        }

        [Test]
        public void LateUpdate_CallsCurrentState()
        {
            var fsm = new StateMachine<IState>();
            var idle = new IdleState();
            fsm.ChangeState(idle);

            fsm.LateUpdate();
            Assert.AreEqual(1, idle.LateUpdateCount);
        }

        [Test]
        public void RegisterTransition_And_CanTransition()
        {
            var fsm = new StateMachine<IState>();
            var transition = new StateTransition
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
            var fsm = new StateMachine<IState>();
            var transition = new StateTransition
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
            var fsm = new StateMachine<IState>();
            fsm.ChangeState<IdleState>();

            bool changed = fsm.TryChangeState<AttackState>();
            Assert.IsFalse(changed);
            Assert.IsInstanceOf<IdleState>(fsm.CurrentState);
        }

        [Test]
        public void UnregisterTransition_RemovesTransition()
        {
            var fsm = new StateMachine<IState>();
            var transition = new StateTransition
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
            var fsm = new StateMachine<IState>();
            var transition = new StateTransition
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
            var fsm = new StateMachine<IState>();
            var transition = new StateTransition
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
            var fsm = new StateMachine<IState>();

            var lowPriority = new StateTransition
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState),
                IsEnabled = true,
                Priority = 0
            };

            var highPriority = new StateTransition
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
            var fsm = new StateMachine<IState>();
            IdleState first = fsm.GetOrCreateState<IdleState>();
            fsm.ClearStateCache();
            IdleState second = fsm.GetOrCreateState<IdleState>();

            Assert.AreNotSame(first, second, "After ClearStateCache, new instance should be created.");
        }

        [Test]
        public void OnStateChanged_Event_Fires()
        {
            var fsm = new StateMachine<IState>();
            IState eventFrom = null, eventTo = null;
            fsm.OnStateChanged.AddListener((from, to) =>
            {
                eventFrom = from;
                eventTo = to;
            });

            var idle = new IdleState();
            var run = new RunState();
            fsm.ChangeState(idle);
            fsm.ChangeState(run);

            Assert.AreEqual(idle, eventFrom);
            Assert.AreEqual(run, eventTo);
        }

        [Test]
        public void OnStateEntered_Event_Fires()
        {
            var fsm = new StateMachine<IState>();
            IState lastEntered = null;
            fsm.OnStateEntered.AddListener(s => lastEntered = s);

            fsm.ChangeState<IdleState>();
            Assert.IsNotNull(lastEntered);
            Assert.IsInstanceOf<IdleState>(lastEntered);
        }

        [Test]
        public void OnStateExited_Event_Fires()
        {
            var fsm = new StateMachine<IState>();
            IState lastExited = null;
            fsm.OnStateExited.AddListener(s => lastExited = s);

            var idle = new IdleState();
            fsm.ChangeState(idle);
            fsm.ChangeState<RunState>();

            Assert.AreEqual(idle, lastExited);
        }

        [Test]
        public void GlobalTransition_FiresFromAnyState()
        {
            var fsm = new StateMachine<IState>();
            var globalTransition = new StateTransition
            {
                FromStateType = null, // global
                ToStateType = typeof(AttackState),
                IsEnabled = true
            };

            fsm.RegisterTransition(globalTransition);
            fsm.ChangeState<IdleState>();

            fsm.EvaluateTransitions();
            Assert.IsInstanceOf<AttackState>(fsm.CurrentState, "Global transition should fire from any state.");
        }

        [Test]
        public void CanTransitionTo_NoCurrentState_ReturnsTrue()
        {
            var fsm = new StateMachine<IState>();
            Assert.IsTrue(fsm.CanTransitionTo<IdleState>(), "With no current state, any transition should be allowed.");
        }
    }
}
