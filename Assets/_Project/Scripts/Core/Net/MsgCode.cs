namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Message codes (Teknik §4.3). Each range belongs to its agent; adding inside your own range is free,
    /// touching another range is a contract change. Values are wire format — never renumber.
    /// </summary>
    public enum MsgCode : byte
    {
        // --- 1-29 Match (A) ---
        MatchStateChanged = 1, RoundSetup = 2, RoundEnded = 3, RolesAssigned = 4, ScoreChanged = 5,

        // --- 30-59 Human (A) ---
        ReqInteract = 30, InteractResult = 31, HumanDamaged = 32, HumanDied = 33,
        ReqUseItem = 34, ItemUsed = 35, ReqKick = 36, KickResult = 37, StatusApplied = 38,
        ReqLanternPulse = 39, LanternPulsed = 40, InventoryChanged = 41, HumanNoise = 42,

        // --- 60-99 Possession & Jinn (B) ---
        ReqPossess = 60, PossessBegan = 61, PossessCompleted = 62, PossessEnded = 63, PossessDenied = 64,
        ReqAction = 65, ActionTelegraph = 66, ActionResolved = 67, ActionDenied = 68,
        EnergyChanged = 69, JinnStunned = 70, ProjectileSpawned = 71, ProjectileImpact = 72,
        ReqExorcise = 73, ExorciseProgress = 74, ExorciseResult = 75,
        ReqPing = 76, PingPlaced = 77, ReqBless = 78, BlessApplied = 79, RageStarted = 80,
        PossessedMove = 81, ObjectStateChanged = 82,

        // --- 100-139 World & Objectives (C) ---
        LevelBuilt = 100, ContainerSearched = 101, KeyFragmentCollected = 102,
        RunePressed = 103, RuneResult = 104, DigProgress = 105, VaultOpened = 106,
        GoldPickedUp = 107, GoldDropped = 108, PhaseChanged = 109, LightsChanged = 110, LevelHash = 111,

        // --- 200-254 Debug ---
        DebugCommand = 200,
    }
}
