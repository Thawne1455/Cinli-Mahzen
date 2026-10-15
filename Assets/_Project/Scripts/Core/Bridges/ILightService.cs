namespace CinliMahzen.Core
{
    /// <summary>Lamp control (Teknik §4.8). Implemented by World; room light state is derived from its lamps (IRoomService.LightOf).</summary>
    public interface ILightService
    {
        /// <summary>Authority only.</summary>
        void SetLampAuthority(NetId lamp, LampState state);
    }
}
