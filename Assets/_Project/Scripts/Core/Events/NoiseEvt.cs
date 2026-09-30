// Owner: A
using UnityEngine;
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Player/Objectives; Visibility draws the evil jinn's noise ring.</summary>
    public readonly struct NoiseEvt
    {
        public readonly Vector3 Pos;
        public readonly float Loudness;

        public NoiseEvt(Vector3 pos, float loudness)
        {
            Pos = pos;
            Loudness = loudness;
        }
    }
}
