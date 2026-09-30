namespace CinliMahzen.Core
{
    public interface IStatusReceiver
    {
        /// <summary>Authority only.</summary>
        void ApplyStatusAuthority(StatusType t, float duration, PlayerId source);
    }
}
