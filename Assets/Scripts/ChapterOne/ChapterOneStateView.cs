using System;
using System.Collections.Generic;
using UnityEngine;

// Visual state is derived from the same IDs that SaveLoadManager persists.
public class ChapterOneStateView : MonoBehaviour
{
    [Serializable]
    public class Binding
    {
        public GameObject target;
        public string[] required = Array.Empty<string>();
        public string[] excluded = Array.Empty<string>();
    }

    [SerializeField] private List<Binding> bindings = new List<Binding>();

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
            if (binding.target == null) continue;
            bool visible = true;
            foreach (string id in binding.required)
                visible &= GameProgressState.IsPuzzleCompleted(id);
            foreach (string id in binding.excluded)
                visible &= !GameProgressState.IsPuzzleCompleted(id);
            if (binding.target.activeSelf != visible)
                binding.target.SetActive(visible);
        }
    }
}
