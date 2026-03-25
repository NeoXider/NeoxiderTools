namespace Neo.StateMachine
{
    /// <summary>
    ///     Contract for states used with StateMachine.
    ///     Defines enter / tick / exit lifecycle hooks.
    /// </summary>
    /// <remarks>
    ///     All concrete states must implement this interface.
    ///     OnFixedUpdate and OnLateUpdate may be no-ops when unused.
    /// </remarks>
    /// <example>
    ///     <code>
    /// public class IdleState : IState
    /// {
    ///     public void OnEnter()
    ///     {
    ///         Debug.Log("Entered Idle State");
    ///     }
    ///     
    ///     public void OnUpdate()
    ///     {
    ///         // Per-frame state logic
    ///     }
    ///     
    ///     public void OnExit()
    ///     {
    ///         Debug.Log("Exited Idle State");
    ///     }
    ///     
    ///     public void OnFixedUpdate()
    ///     {
    ///         // Physics (optional)
    ///     }
    ///     
    ///     public void OnLateUpdate()
    ///     {
    ///         // Late update (optional)
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IState
    {
        /// <summary>
        ///     Called once when becoming the active state.
        ///     Use for setup, animations, enabling components, etc.
        /// </summary>
        /// <remarks>
        ///     Runs before the first OnUpdate after a transition.
        /// </remarks>
        void OnEnter();

        /// <summary>
        ///     Called every frame while this state is active.
        /// </summary>
        /// <remarks>
        ///     Driven by StateMachine.Update(); not called if the machine is not updated.
        /// </remarks>
        void OnUpdate();

        /// <summary>
        ///     Called once when leaving this state.
        /// </summary>
        /// <remarks>
        ///     Runs after the last OnUpdate of the outgoing state.
        /// </remarks>
        void OnExit();

        /// <summary>
        ///     Called every FixedUpdate tick while active (physics timestep).
        /// </summary>
        /// <remarks>
        ///     Optional; leave empty if unused. Driven by StateMachine.FixedUpdate().
        /// </remarks>
        void OnFixedUpdate();

        /// <summary>
        ///     Called every frame after all Updates while active.
        /// </summary>
        /// <remarks>
        ///     Optional; leave empty if unused. Driven by StateMachine.LateUpdate().
        /// </remarks>
        void OnLateUpdate();
    }
}
