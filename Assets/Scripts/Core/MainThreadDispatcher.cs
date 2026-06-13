using System;
using System.Collections.Generic;
using UnityEngine;

namespace YaeSakura
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        public static UnityMainThreadDispatcher Instance { get; private set; }

        private Queue<Action> _queue = new Queue<Action>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            lock (_queue)
            {
                while (_queue.Count > 0)
                    _queue.Dequeue()?.Invoke();
            }
        }

        public void Enqueue(Action action)
        {
            lock (_queue) { _queue.Enqueue(action); }
        }
    }
}
