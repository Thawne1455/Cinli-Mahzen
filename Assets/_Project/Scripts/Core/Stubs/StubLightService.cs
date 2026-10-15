namespace CinliMahzen.Core
{
    /// <summary>Placeholder until World's LightService: lamp commands are ignored.</summary>
    public sealed class StubLightService : ILightService
    {
        public void SetLampAuthority(NetId lamp, LampState state)
        {
        }
    }
}
