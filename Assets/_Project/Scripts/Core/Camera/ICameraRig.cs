using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// The local player's camera (Teknik §2, TODO 1.2). Lives in CM.Core so CM.Jinn / CM.Possession can drive the camera
    /// without seeing CM.Player, which implements it (<c>CameraRig</c>) and registers it with
    /// <c>GameServices.Register&lt;ICameraRig&gt;</c>. Resolve it with <c>GameServices.Get&lt;ICameraRig&gt;()</c> outside Update.
    /// Every mode change blends from the current view over the rig's blend time (0.3 s); the very first attach snaps.
    /// Calling a setter with the anchor already in use is a no-op, so a pawn may call it every time it becomes local.
    /// </summary>
    public interface ICameraRig
    {
        CameraMode Mode { get; }

        /// <summary>The managed camera.</summary>
        Camera Camera { get; }

        /// <summary>Locks the view to <paramref name="head"/> (position and rotation).</summary>
        void SetFps(Transform head);

        /// <summary>
        /// Orbits <paramref name="target"/> with the mouse (<c>IPossessedInput.Look</c>); walls on the Environment layer pull the
        /// camera in. <paramref name="distance"/> &lt;= 0 uses the rig's default distance.
        /// </summary>
        void SetOrbit(Transform target, float distance);

        /// <summary>Looks down at a fainted <paramref name="body"/> from above.</summary>
        void SetFaint(Transform body);

        /// <summary>
        /// Blurs and shakes the view for <paramref name="duration"/> seconds (Spirit Bump, <c>StatusType.VisionBlurred</c>).
        /// Triggering while a blur runs extends it to the later end; it never stacks.
        /// </summary>
        void TriggerBlur(float duration);
    }
}
