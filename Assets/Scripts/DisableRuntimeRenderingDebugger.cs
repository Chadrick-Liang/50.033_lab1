using UnityEngine;
using UnityEngine.Rendering;

public static class DisableRuntimeRenderingDebugger
{
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Disable()
    {
        DebugManager.instance.enableRuntimeUI = false;
    }
}