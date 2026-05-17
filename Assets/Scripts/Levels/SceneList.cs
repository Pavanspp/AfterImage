using UnityEngine;

// ScriptableObject holding the ordered list of level scene names.
// Create one instance via Assets > Create > Afterimage > Scene List.
// Add scene names in order — Level1, Level2, etc.
// All scenes must be added to File > Build Settings.
[CreateAssetMenu(fileName = "SceneList", menuName = "Afterimage/Scene List")]
public class SceneList : ScriptableObject
{
    [Tooltip("Scene names in order. Must match names in Build Settings exactly.")]
    public string[] levels;
}