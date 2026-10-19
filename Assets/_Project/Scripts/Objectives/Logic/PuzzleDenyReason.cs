namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Why the authority refused (or penalised) a puzzle request (Teknik §7.3). <see cref="None"/> means the request was accepted.
    /// PuzzleRules returns the rule-based reasons; PuzzleSystemCore adds <see cref="UnknownSlot"/>, <see cref="PawnMissing"/>,
    /// <see cref="InvalidOp"/> and <see cref="WrongAnswer"/>. Debug/log value only (never sent over the network), but kept explicit
    /// and stable: append new values at the end.
    /// </summary>
    public enum PuzzleDenyReason : byte
    {
        /// <summary>Accepted.</summary>
        None = 0,
        /// <summary>No puzzle is registered in that slot.</summary>
        UnknownSlot = 1,
        /// <summary>Op / confirm needs the human; scramble needs an evil jinn.</summary>
        WrongRole = 2,
        /// <summary>Evil jinns cannot act before JinnWakeDelay is over.</summary>
        JinnsAsleep = 3,
        /// <summary>The human is fainted.</summary>
        HumanFainted = 4,
        /// <summary>The puzzle is confirmed (locked); nothing changes it anymore.</summary>
        Locked = 5,
        /// <summary>The requester has no pawn in the world, so no range can be checked.</summary>
        PawnMissing = 6,
        /// <summary>Beyond the range limit (plus NetRangeTolerance).</summary>
        OutOfRange = 7,
        /// <summary>The puzzle needs an item (paint, fuse key) the human does not carry.</summary>
        MissingItem = 8,
        /// <summary>PuzzleState.TryApply refused the op (kind not allowed, part/value out of range, ...).</summary>
        InvalidOp = 9,
        /// <summary>The puzzle's room is lit: scrambling is impossible in light.</summary>
        LitRoom = 10,
        /// <summary>The human's strong lantern covers the puzzle.</summary>
        LanternProtected = 11,
        /// <summary>The puzzle was scrambled less than ScramblePuzzleCooldown ago.</summary>
        ScrambleCooldown = 12,
        /// <summary>The jinn cannot pay ScrambleEnergyCost.</summary>
        InsufficientEnergy = 13,
        /// <summary>The confirm lever is locked after a wrong confirmation (ConfirmFailCooldown).</summary>
        ConfirmCooldown = 14,
        /// <summary>Not a refusal: the confirm was processed but the puzzle is not solved (damage + cooldown applied, PuzzleConfirmResult fail sent).</summary>
        WrongAnswer = 15,
    }
}
