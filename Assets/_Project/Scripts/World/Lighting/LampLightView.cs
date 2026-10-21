using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// The visible light of one LampMarker (TODO 3.4): a shadowless point light (Teknik §13) that is on while the lamp is On. Created at runtime by
    /// <see cref="HouseService"/> as a child of every LampMarker; driven by <see cref="LightService.LampChanged"/>. Cosmetic only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LampLightView : MonoBehaviour
    {
        public const float DefaultRange = 8f;
        public const float DefaultIntensity = 2f;

        private Light _light;

        public LampState Shown { get; private set; }

        /// <summary>Adds the view (and its Light) under <paramref name="marker"/>.</summary>
        public static LampLightView Create(Transform marker, float range)
        {
            var go = new GameObject("LampLight");
            go.transform.SetParent(marker, false);
            var view = go.AddComponent<LampLightView>();
            view._light = go.AddComponent<Light>();
            view._light.type = LightType.Point;
            view._light.range = range;
            view._light.intensity = DefaultIntensity;
            view._light.color = new Color(1f, 0.85f, 0.6f);
            view._light.shadows = LightShadows.None;
            view.Show(LampState.Off);
            return view;
        }

        public void Show(LampState state)
        {
            Shown = state;
            if (_light != null)
                _light.enabled = state == LampState.On;
        }
    }
}
