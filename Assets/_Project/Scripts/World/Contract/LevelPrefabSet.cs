using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Owner: C. Environment prefabs used by ProceduralLevelGenerator (asset: Resources/Level/LevelPrefabSet).
    /// Conventions (measured in C0.1, see docs/04_Asset_Eslestirme.md): floor pivot = cell centre (top at y=0.05),
    /// wall/doorway/gated pivot = segment centre at y=0, length along local X, thickness along Z; pillar pivot = vertex centre.
    /// </summary>
    public sealed class LevelPrefabSet : ScriptableObject
    {
        public const string ResourcePath = "Level/LevelPrefabSet";

        [SerializeField] private GameObject[] floors;
        [SerializeField] private GameObject[] walls;
        [SerializeField] private GameObject wallDoorway;
        [SerializeField] private GameObject wallGated;
        [SerializeField] private GameObject pillar;

        public GameObject[] Floors => floors;
        public GameObject[] Walls => walls;
        public GameObject WallDoorway => wallDoorway;
        public GameObject WallGated => wallGated;
        public GameObject Pillar => pillar;

        public bool IsComplete => floors != null && floors.Length > 0 && floors[0] != null
                                  && walls != null && walls.Length > 0 && walls[0] != null
                                  && wallDoorway != null && wallGated != null && pillar != null;

        public static LevelPrefabSet Load() => Resources.Load<LevelPrefabSet>(ResourcePath);
    }
}
