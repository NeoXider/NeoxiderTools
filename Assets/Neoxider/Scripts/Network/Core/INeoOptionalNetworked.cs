namespace Neo.Network
{
    /// <summary>
    ///     Marker for Neo components whose networking is optional — they can run locally with
    ///     <c>isNetworked = false</c> even when Mirror is installed and the GameObject carries
    ///     a <c>NetworkIdentity</c> (required by Mirror's <c>NetworkBehaviour</c> inheritance).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Mirror's <c>NetworkScenePostProcess</c> force-disables every scene
    ///         <c>NetworkIdentity</c> on load so <c>NetworkServer.SpawnObjects()</c> can manage
    ///         their lifecycle. In a pure offline scene that handler never runs, so these
    ///         components would stay disabled forever. <see cref="NeoMirrorSceneReactivator"/>
    ///         re-enables them when no Mirror session is active, using this interface to decide
    ///         which scene objects opted in.
    ///     </para>
    ///     <para>
    ///         Implemented by <c>NeoNetworkComponent</c> (covers all subclasses), <c>Money</c>,
    ///         and the <c>PlayerController{2D,3D}Physics</c> controllers. Custom components that
    ///         also need offline-with-Mirror behaviour can implement it directly.
    ///     </para>
    /// </remarks>
    public interface INeoOptionalNetworked
    {
        /// <summary>
        ///     <see langword="true"/> when the component is configured to participate in network
        ///     synchronization. Returning <see langword="false"/> tells the reactivator to keep
        ///     the GameObject alive in offline scenes.
        /// </summary>
        bool IsNetworked { get; }
    }
}
