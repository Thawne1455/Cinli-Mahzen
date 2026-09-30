using System;
using System.Collections.Generic;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Service locator (Teknik §4.7). Modules register their implementations; until they do,
    /// the bridge interfaces resolve to stubs (StubMatchInfo, StubLevelInfo, ...) so every module compiles and runs alone.
    /// Cache results outside Update — Get is a dictionary lookup.
    /// </summary>
    public static class GameServices
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        static GameServices()
        {
            ResetToStubs();
        }

        public static INetBridge Net => Get<INetBridge>();
        public static IPlayerRegistry Players => Get<IPlayerRegistry>();
        public static IEntityRegistry Entities => Get<IEntityRegistry>();
        public static GameBalanceConfig Config => Get<GameBalanceConfig>();
        /// <summary>Phase, remaining time, round.</summary>
        public static IMatchInfo Match => Get<IMatchInfo>();
        /// <summary>Active level data (C).</summary>
        public static ILevelInfo Level => Get<ILevelInfo>();

        /// <summary>Registers <paramref name="service"/> under <typeparamref name="T"/>, replacing any previous one (stub included).</summary>
        public static void Register<T>(T service)
        {
            if (service == null)
            {
                Services.Remove(typeof(T));
                return;
            }
            Services[typeof(T)] = service;
        }

        /// <summary>Removes the service only if <paramref name="service"/> is the registered instance. Bridge interfaces fall back to their stub.</summary>
        public static void Unregister<T>(T service)
        {
            if (!Services.TryGetValue(typeof(T), out object current) || !ReferenceEquals(current, service))
                return;
            Services.Remove(typeof(T));
            object stub = CreateStub(typeof(T));
            if (stub != null)
                Services[typeof(T)] = stub;
        }

        public static T Get<T>()
        {
            return Services.TryGetValue(typeof(T), out object s) ? (T)s : default;
        }

        public static bool TryGet<T>(out T service)
        {
            if (Services.TryGetValue(typeof(T), out object s))
            {
                service = (T)s;
                return true;
            }
            service = default;
            return false;
        }

        /// <summary>Clears every service and re-registers the stubs. Called on play-mode entry (domain reload may be off) and by tests.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetToStubs()
        {
            Services.Clear();
            Services[typeof(IMatchInfo)] = new StubMatchInfo();
            Services[typeof(ILevelInfo)] = new StubLevelInfo();
            Services[typeof(IObjectiveInfo)] = new StubObjectiveInfo();
            Services[typeof(ILightService)] = new StubLightService();
            Services[typeof(IPossessionQuery)] = new StubPossessionQuery();
            Services[typeof(IPossessionBlockerRegistry)] = new StubPossessionBlockerRegistry();
        }

        private static object CreateStub(Type t)
        {
            if (t == typeof(IMatchInfo)) return new StubMatchInfo();
            if (t == typeof(ILevelInfo)) return new StubLevelInfo();
            if (t == typeof(IObjectiveInfo)) return new StubObjectiveInfo();
            if (t == typeof(ILightService)) return new StubLightService();
            if (t == typeof(IPossessionQuery)) return new StubPossessionQuery();
            if (t == typeof(IPossessionBlockerRegistry)) return new StubPossessionBlockerRegistry();
            return null;
        }
    }
}
