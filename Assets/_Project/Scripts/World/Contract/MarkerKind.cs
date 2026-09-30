namespace CinliMahzen.World
{
    /// <summary>Unity-free mirror of the marker components in Spec 8.2; used by the planner and the validator rules.</summary>
    public enum MarkerKind
    {
        HumanSpawn,
        JinnSpawn,
        Exit,
        Vault,
        GoldSpawn,
        PossessableSocket,
        ContainerSocket,
        PuzzleSocket,
        LightSocket,
        DecorSocket,
    }
}
