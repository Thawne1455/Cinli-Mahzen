using UnityEngine;

namespace CinliMahzen.World
{
    public interface ILevelGenerator
    {
        /// <summary>Deterministic: same seed + settings -> identical hierarchy and positions. No UnityEngine.Random.</summary>
        LevelLayout Generate(int seed, LevelGenSettings settings, Transform root);
    }
}
