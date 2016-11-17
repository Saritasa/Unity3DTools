using System;
using System.Collections.Generic;

/// <summary>
/// Auxiliary class which allows to use shared resource by several clients regardless 
/// to state of this resource: not loaded, loading, loaded.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class RemoteResource<T>
{
    private T resource;

    private readonly Queue<Action<T>> listeners = new Queue<Action<T>>();

    public RemoteResource(Action<T> listener)
    {
        listeners.Enqueue(listener);
    }

    /// <summary>
    /// Register resource listener. Calls listener callback immediately if resource is loaded.
    /// Otherwise registers listener in queue
    /// </summary>
    /// <param name="listener"></param>
    public void WaitResource(Action<T> listener)
    {
        if (resource != null)
        {
            listener.Invoke(resource);
        }
        else
        {
            listeners.Enqueue(listener);
        }
    }

    /// <summary>
    /// It is assumed that resource loading is triggered by some external code.
    /// But this function must be called once resource loading is finished.
    /// </summary>
    /// <param name="recievedResource"></param>
    public void OnResourceLoaded(T recievedResource)
    {
        resource = recievedResource;
        while (listeners.Count > 0)
        {
            var listener = listeners.Dequeue();
            listener.Invoke(resource);
        }
    }
}
