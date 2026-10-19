namespace CinliMahzen.Core
{
    /// <summary>
    /// The machine's live input as a service (Teknik §5.1, TODO 1.3). CM.Jinn cannot see CM.Player, where <c>LocalInputSource</c> and
    /// <c>NullInput</c> live, yet a spirit pawn must receive "the local player's input" while it is the played pawn and an idle input
    /// otherwise. <c>LocalInputSource</c> implements this and registers itself with <c>GameServices.Register&lt;ILocalInputProvider&gt;</c>;
    /// <c>PawnBase.ApplyInput</c> is the only caller.
    /// </summary>
    public interface ILocalInputProvider
    {
        /// <summary>Input for pawns this machine does not play: everything idle (<c>NullInput.Instance</c>). Also implements the role's input interfaces.</summary>
        IPawnInput Idle { get; }

        /// <summary>
        /// Enables the gameplay action map matching <paramref name="role"/> (Human, or Spirit for the jinn roles) and returns the live
        /// source, which implements IHumanInput / ISpiritInput / IPossessedInput. Repeating it for the same role is a no-op.
        /// </summary>
        IPawnInput AcquireLocal(Role role);
    }
}
