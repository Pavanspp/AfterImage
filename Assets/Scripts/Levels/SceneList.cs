using UnityEngine;

[CreateAssetMenu(fileName = "SceneList", menuName = "Afterimage/Scene List")]
public class SceneList : ScriptableObject
{
    [Tooltip("Must match names in Build Settings.")]
    public string[] levels;
}