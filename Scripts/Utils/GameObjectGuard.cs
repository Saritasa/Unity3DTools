using System;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Auxiliary class which allows to create temporary objects in scope
    /// </summary>
    public class GameObjectGuard : IDisposable
    {
        public GameObject Obj { get; private set; }

        public GameObjectGuard(GameObject gameObject)
        {
            Obj = gameObject;
        }

        public void Dispose()
        {
            if (Obj == null)
                return;

            GameObject.Destroy(Obj);
            Obj = null;
        }
    }
}