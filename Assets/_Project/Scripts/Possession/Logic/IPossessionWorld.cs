using CinliMahzen.Core;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Everything <see cref="PossessionArbiterCore"/> needs to know about the outside world.
    /// The MonoBehaviour side (PossessionSystem) implements it on top of GameServices
    /// (IPlayerRegistry, IMatchInfo, IPossessionBlockerRegistry, jinn stun state, entity positions);
    /// EditMode tests implement it with a fake.
    /// </summary>
    public interface IPossessionWorld
    {
        /// <summary>Rule 1.</summary>
        Role GetRole(PlayerId player);

        /// <summary>Rule 1: stunned (kick / exorcise) or possess-locked (ExorcisePossessLock).</summary>
        bool IsPossessionLocked(PlayerId player, double now);

        /// <summary>Rule 2: match state is Playing.</summary>
        bool IsPlaying { get; }

        /// <summary>Rule 2: <c>IMatchInfo.JinnsAwake</c> (JinnWakeDelay elapsed).</summary>
        bool JinnsAwake { get; }

        /// <summary>Rule 3: an <c>IPossessionBlocker</c> (salt) covers the object.</summary>
        bool IsObjectBlocked(NetId obj);

        /// <summary>Rule 4: distance from the jinn to the object (closest point); false if either is unknown.</summary>
        bool TryGetDistance(PlayerId player, NetId obj, out float distance);
    }
}
