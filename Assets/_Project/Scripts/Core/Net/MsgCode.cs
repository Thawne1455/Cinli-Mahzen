namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Message codes (Teknik §4.3, v2.1). Values are the wire format — once online (M5) never renumber, only append inside a range.
    /// Every message gets an encode/decode helper + EditMode round-trip test in the task that first sends it.
    /// </summary>
    public enum MsgCode : byte
    {
        // --- 1-29 Match ---
        MatchStateChanged = 1, RoundSetup = 2, RoundTimeAdded = 3, RoundEnded = 4, RolesAssigned = 5, ScoreChanged = 6,

        // --- 30-59 Human ---
        ReqInteract = 30, InteractResult = 31, HumanDamaged = 32, HumanFainted = 33, HumanRevived = 34,
        ReqKick = 35, KickResult = 36, StatusApplied = 37, ReqLanternPulse = 38, LanternPulsed = 39,
        BlessedOilCollected = 40, InventoryChanged = 41, ReqDropItem = 42, ItemDropped = 43, ReqPush = 44, PushResult = 45,

        // --- 60-99 Jinn & Possession ---
        ReqPossess = 60, PossessBegan = 61, PossessCompleted = 62, PossessEnded = 63, PossessDenied = 64,
        ReqAction = 65, ActionTelegraph = 66, ActionResolved = 67, ActionDenied = 68,
        EnergyChanged = 69, JinnStunned = 70, ProjectileSpawned = 71, ProjectileImpact = 72, SpiritBlockedProjectile = 73,
        ReqExorcise = 74, ExorciseProgress = 75, ExorciseResult = 76, ReqPing = 77, PingPlaced = 78,
        RageStarted = 79, PossessedMove = 80, ObjectStateChanged = 81, ReqLampLight = 82, ReqLampRepair = 83,
        ReqFocusReference = 84, ReferenceFocusChanged = 85, ReqSpiritBump = 86, SpiritBumped = 87, CologneReviveTriggered = 88,

        // --- 100-139 World & Puzzles ---
        HouseReady = 100, ReqPuzzleOp = 101, PuzzleStateChanged = 102, ReqPuzzleScramble = 103, PuzzleScrambled = 104,
        ReqPuzzleConfirm = 105, PuzzleConfirmResult = 106, PuzzleCompleted = 107, StageCompleted = 108,
        GateOpened = 109, ItemSpawned = 110, ItemPickedUp = 111, LightsChanged = 112, RadioStateChanged = 113,
        KeyFound = 114, DigProgress = 115, DigCheckpointReached = 116, TreasureDug = 117, HouseHash = 118,

        // --- 200-254 Debug ---
        DebugCommand = 200,
    }
}
