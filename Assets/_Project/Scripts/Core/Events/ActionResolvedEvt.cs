// Owner: B (drafted by A in A0.5)
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Possession when an action resolved on the authority.</summary>
    public readonly struct ActionResolvedEvt
    {
        public readonly NetId Obj;
        public readonly int ActionIdx;
        public readonly bool HitHuman;

        public ActionResolvedEvt(NetId obj, int actionIdx, bool hitHuman)
        {
            Obj = obj;
            ActionIdx = actionIdx;
            HitHuman = hitHuman;
        }
    }
}
