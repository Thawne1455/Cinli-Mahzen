namespace CinliMahzen.Core
{
    /// <summary>Implemented by B (Possession, mimic). Returns true when the interaction was consumed.</summary>
    public interface IInteractInterceptor
    {
        bool TryInterceptAuthority(NetId target, PlayerId who);
    }
}
