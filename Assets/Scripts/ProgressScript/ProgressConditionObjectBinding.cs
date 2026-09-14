using System;
using System.Collections.Generic;
using UnityEngine;

// Visual state is derived from the same IDs that SaveLoadManager persists.
public class ProgressConditionObjectBinding : MonoBehaviour
{
    [Serializable]
    public class Binding
    {
        public GameObject target;
        public OnlyOneUnityString[] required = Array.Empty<OnlyOneUnityString>();
        public OnlyOneUnityString[] excluded = Array.Empty<OnlyOneUnityString>();
    }

    [SerializeField]
    private List<Binding> bindings = new List<Binding>();

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += Refresh;
        Refresh();
    }

    private void Start() => Refresh();

    private void OnDisable() => GameProgressState.ProgressChanged -= Refresh;

    public void Refresh()
    {
        foreach (Binding binding in bindings)
        {
            if (binding.target == null)
                continue;
            bool visible = true;
            foreach (OnlyOneUnityString id in binding.required)
                visible &= GameProgressState.IsPuzzleCompleted(id);
            foreach (OnlyOneUnityString id in binding.excluded)
                visible &= !GameProgressState.IsPuzzleCompleted(id);
            if (binding.target.activeSelf != visible)
                binding.target.SetActive(visible);
        }
    }
}
