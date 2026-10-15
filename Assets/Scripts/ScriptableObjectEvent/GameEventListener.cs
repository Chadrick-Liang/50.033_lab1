
using UnityEngine;
using UnityEngine.Events;

// if attached to an object that might be disabled, callback will not work
// attach it on a parent object that wont be disabled
public class GameEventListener<T> : MonoBehaviour
{
    public GameEvent<T> Event;

    public UnityEvent<T> Response;

    private void OnEnable()
    {
        Event.RegisterListener(this);
    }

    // This is also called when the object is destroyed and can be used for any cleanup code. When scripts are reloaded after compilation has finished, OnDisable will be called, followed by an OnEnable after the script has been loaded.
    private void OnDisable()
    {
        Event.UnregisterListener(this);
    }

    public void OnEventRaised(T data)
    {
        Response.Invoke(data);
    }
}
