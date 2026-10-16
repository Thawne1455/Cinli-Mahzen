namespace CinliMahzen.Core
{
    /// <summary>What the local camera is currently doing (Teknik §2 "CameraRig"). Values are stable — only append.</summary>
    public enum CameraMode : byte
    {
        /// <summary>No anchor yet: the rig leaves the camera where the scene put it.</summary>
        None = 0,
        /// <summary>Locked to a pawn's head (human, spirit form).</summary>
        Fps = 1,
        /// <summary>Third-person orbit around a possessed object, shortened by walls.</summary>
        Orbit = 2,
        /// <summary>Looking down at a fainted body from above.</summary>
        Faint = 3,
    }
}
