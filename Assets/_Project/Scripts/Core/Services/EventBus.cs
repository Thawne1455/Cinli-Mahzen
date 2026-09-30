using System;
using System.Collections.Generic;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Local, network-free event bus for struct events (Teknik §4.6). Publish does not allocate.
    /// Subscribing/unsubscribing inside a handler is safe; new subscribers start with the next publish.
    /// </summary>
    public static class EventBus
    {
        private static readonly List<Action> Clearers = new List<Action>();

        public static void Subscribe<T>(Action<T> h) where T : struct
        {
            Channel<T>.Subscribe(h);
        }

        public static void Unsubscribe<T>(Action<T> h) where T : struct
        {
            Channel<T>.Unsubscribe(h);
        }

        public static void Publish<T>(in T evt) where T : struct
        {
            Channel<T>.Publish(evt);
        }

        /// <summary>Number of live subscribers for <typeparamref name="T"/> (debug/tests).</summary>
        public static int SubscriberCount<T>() where T : struct
        {
            return Channel<T>.Count;
        }

        /// <summary>Drops every subscriber of every event type (scene change, tests).</summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            for (int i = 0; i < Clearers.Count; i++)
                Clearers[i]();
        }

        private static class Channel<T> where T : struct
        {
            private static readonly List<Action<T>> Handlers = new List<Action<T>>();
            private static int _depth;
            private static bool _dirty;

            static Channel()
            {
                Clearers.Add(Clear);
            }

            public static int Count
            {
                get
                {
                    int n = 0;
                    for (int i = 0; i < Handlers.Count; i++)
                    {
                        if (Handlers[i] != null)
                            n++;
                    }
                    return n;
                }
            }

            public static void Subscribe(Action<T> h)
            {
                if (h != null && !Handlers.Contains(h))
                    Handlers.Add(h);
            }

            public static void Unsubscribe(Action<T> h)
            {
                int i = Handlers.IndexOf(h);
                if (i < 0)
                    return;
                if (_depth > 0)
                {
                    Handlers[i] = null;
                    _dirty = true;
                }
                else
                {
                    Handlers.RemoveAt(i);
                }
            }

            public static void Publish(in T evt)
            {
                _depth++;
                int count = Handlers.Count;
                for (int i = 0; i < count; i++)
                {
                    Action<T> h = Handlers[i];
                    if (h == null)
                        continue;
                    try
                    {
                        h(evt);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
                _depth--;

                if (_depth == 0 && _dirty)
                {
                    Handlers.RemoveAll(x => x == null);
                    _dirty = false;
                }
            }

            private static void Clear()
            {
                if (_depth > 0)
                {
                    for (int i = 0; i < Handlers.Count; i++)
                        Handlers[i] = null;
                    _dirty = true;
                }
                else
                {
                    Handlers.Clear();
                }
            }
        }
    }
}
