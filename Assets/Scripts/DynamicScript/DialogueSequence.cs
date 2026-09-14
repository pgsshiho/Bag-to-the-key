using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Narrative/Dialogue Sequence")]
public sealed class DialogueSequence : ScriptableObject
{
    [SerializeField, TextArea(2, 5)]
    private List<string> lines = new();

    public IReadOnlyList<string> Lines => lines;
}
