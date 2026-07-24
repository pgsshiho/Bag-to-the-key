using System.Collections.Generic;
using UnityEngine;

public static class WorldInteractionGate
{
    private static readonly HashSet<object> blockers = new();
    private static readonly List<object> destroyedBlockers = new();

    public static bool IsBlocked
    {
        get
        {
            RemoveDestroyedUnityObjects();
            return blockers.Count > 0;
        }
    }

    public static void Block(object owner)
    {
        if (owner != null)
            blockers.Add(owner);
    }

    public static void Unblock(object owner)
    {
        if (owner != null)
            blockers.Remove(owner);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        blockers.Clear();
        destroyedBlockers.Clear();
    }

    private static void RemoveDestroyedUnityObjects()
    {
        destroyedBlockers.Clear();

        foreach (object blocker in blockers)
        {
            if (blocker is Object unityObject && unityObject == null)
                destroyedBlockers.Add(blocker);
        }

        foreach (object blocker in destroyedBlockers)
            blockers.Remove(blocker);
    }
}
