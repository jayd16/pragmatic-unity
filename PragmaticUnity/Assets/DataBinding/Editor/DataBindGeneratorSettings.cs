using UnityEngine;

[CreateAssetMenu]
public class DataBindGeneratorSettings : ScriptableObject
{
    [SerializeField] [HideInInspector] private DataBindGeneratorSettings Default;
    [SerializeField] private string _generatedFilesPath;
    [SerializeField] private bool _generateOnRecompile;

    private void Awake()
    {
        if (!Default) Default = CreateInstance<DataBindGeneratorSettings>();
    }

    public string GeneratedFilesPath => Default._generatedFilesPath;

    public bool GenerateOnRecompile => Default._generateOnRecompile;
}