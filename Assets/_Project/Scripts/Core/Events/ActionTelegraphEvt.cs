// Owner: B (drafted by A in A0.5)
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Possession when a possessed object starts telegraphing an action.</summary>
    public readonly struct ActionTelegraphEvt
    {
        public readonly NetId Obj;
        public readonly int ActionIdx;

        public ActionTelegraphEvt(NetId obj, int actionIdx)
        {
            Obj = obj;
            ActionIdx = actionIdx;
        }
    }
}
