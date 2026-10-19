using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// What <see cref="ObjectiveFlowCore"/> needs from the scene (TODO 2.7): ground item pickups, the stage gate doors and the human's position.
    /// Implemented by <c>RoundSetupApplier</c>; EditMode tests use a fake.
    /// </summary>
    public interface IObjectiveScene
    {
        /// <summary>Every machine: show a pickup for <paramref name="itemId"/> as entity <paramref name="item"/> at <paramref name="position"/>.</summary>
        void SpawnItem(NetId item, string itemId, Vector3 position);

        /// <summary>Every machine: remove the pickup of <paramref name="item"/> (picked up, round over).</summary>
        void DespawnItem(NetId item);

        /// <summary>Every machine: open the door(s) of the gate that ends <paramref name="stage"/>.</summary>
        void OpenGate(int stage);

        /// <summary>Every machine (TODO 3.6): the house key was found at <paramref name="spot"/> (its glow goes out).</summary>
        void OnKeyFound(NetId spot);

        /// <summary>Every machine (TODO 3.6): the tool shed item at <paramref name="point"/> was taken (its visual goes away).</summary>
        void OnToolShedItemTaken(NetId point);

        /// <summary>Authority: where <paramref name="player"/>'s pawn stands (a dropped item lands there), or null.</summary>
        Vector3? PawnPosition(PlayerId player);
    }
}
