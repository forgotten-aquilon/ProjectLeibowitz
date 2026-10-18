using UnityEngine;

public static class ModalWindowManager
{
    public static MonoBehaviour ActiveWindow { get; private set; }
    public static int ClosedFrame { get; private set; } = -1;
    public static bool CanOpen => !ActiveWindow && ClosedFrame != Time.frameCount;
    public static bool CanInteract => CanOpen && Time.timeScale > 0f;

    private static float previousTimeScale;

    // Reset static state even when Enter Play Mode has domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        ActiveWindow = null;
        ClosedFrame = -1;
        previousTimeScale = 1f;
    }

    public static bool TryOpen(MonoBehaviour window)
    {
        if (!window.isActiveAndEnabled || !CanOpen)
            return false;

        ActiveWindow = window;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        return true;
    }

    public static void Close(MonoBehaviour window)
    {
        if (ActiveWindow != window)
            return;

        ActiveWindow = null;
        ClosedFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
    }
}
