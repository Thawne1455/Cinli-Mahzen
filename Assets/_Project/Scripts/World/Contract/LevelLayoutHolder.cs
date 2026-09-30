using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Lives on the level root; keeps the serialized room data so a level can be re-validated later (editor menu).</summary>
    public class LevelLayoutHolder : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private Bounds bounds;
        [SerializeField] private List<RoomInfo> rooms = new List<RoomInfo>();

        public LevelLayout Layout
        {
            get
            {
                var layout = new LevelLayout { Seed = seed, Bounds = bounds, Rooms = rooms };
                layout.Markers.AddRange(GetComponentsInChildren<LevelMarker>());
                return layout;
            }
        }

        public void Store(LevelLayout layout)
        {
            seed = layout.Seed;
            bounds = layout.Bounds;
            rooms = layout.Rooms;
        }
    }
}
