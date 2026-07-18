using UnityEngine;

public class SFXs : MonoBehaviour
{
    private static SFXs _instance;
    public static SFXs Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }
}
