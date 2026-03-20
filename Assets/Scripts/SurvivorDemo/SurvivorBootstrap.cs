using UnityEngine;

public static class SurvivorBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureDemoExists()
    {
        if (Object.FindFirstObjectByType<SurvivorGame>() != null)
        {
            return;
        }

        var root = new GameObject("SurvivorDemo");
        root.AddComponent<SurvivorGame>();
    }
}
