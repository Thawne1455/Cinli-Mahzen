namespace CinliMahzen.Possession
{
    /// <summary>
    /// The per-object facts <see cref="PossessionArbiterCore"/> needs besides its state machine
    /// (Tech §6.2 rules 1 and 4). Copied from <see cref="PossessableDefinition"/> when the object registers.
    /// </summary>
    public readonly struct PossessableTraits
    {
        /// <summary>Lamps can be entered even in a lit room (the only exception to the light rule).</summary>
        public readonly bool IsLamp;

        /// <summary>The good jinn may enter this object (slide, harmless carry / fling, cologne revive).</summary>
        public readonly bool AllowGoodJinn;

        public PossessableTraits(bool isLamp, bool allowGoodJinn)
        {
            IsLamp = isLamp;
            AllowGoodJinn = allowGoodJinn;
        }

        public static PossessableTraits From(PossessableDefinition definition)
        {
            return definition == null
                ? default
                : new PossessableTraits(definition.IsLamp, definition.AllowGoodJinn);
        }
    }
}
