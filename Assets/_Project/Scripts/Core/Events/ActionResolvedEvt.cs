namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Possession on every machine when an action resolved on the authority.</summary>
    public readonly struct ActionResolvedEvt
    {
        public readonly NetId Obj;
        public readonly int ActionIdx;
        public readonly bool HitHuman;
        /// <summary>TODO 4.3: the jinn inside the object when it resolved (None when unknown).</summary>
        public readonly PlayerId Possessor;
        /// <summary>TODO 4.3: the action's id (PossessableActionDef.Id, e.g. "burst"); empty when unknown.</summary>
        public readonly string ActionId;

        public ActionResolvedEvt(NetId obj, int actionIdx, bool hitHuman)
            : this(obj, actionIdx, hitHuman, PlayerId.None, string.Empty)
        {
        }

        public ActionResolvedEvt(NetId obj, int actionIdx, bool hitHuman, PlayerId possessor, string actionId)
        {
            Obj = obj;
            ActionIdx = actionIdx;
            HitHuman = hitHuman;
            Possessor = possessor;
            ActionId = actionId ?? string.Empty;
        }
    }
}
