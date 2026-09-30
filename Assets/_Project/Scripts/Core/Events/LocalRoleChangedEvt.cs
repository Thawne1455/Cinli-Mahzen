// Owner: A
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Core when the local player's role changes (hotseat F1-F4, role assignment).</summary>
    public readonly struct LocalRoleChangedEvt
    {
        public readonly Role Role;

        public LocalRoleChangedEvt(Role role)
        {
            Role = role;
        }
    }
}
