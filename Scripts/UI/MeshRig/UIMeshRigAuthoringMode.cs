namespace Neo.UI
{
    /// <summary>Defines whether rig handles edit the bind pose or deform the UI mesh.</summary>
    public enum UIMeshRigAuthoringMode
    {
        Setup = 0,
        Pose = 1
    }

    /// <summary>Scene-view transform tool used while posing a selected rig point.</summary>
    public enum UIMeshRigSceneTool
    {
        Move = 0,
        Rotate = 1,
        Scale = 2
    }
}
