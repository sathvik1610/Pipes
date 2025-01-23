using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllLevels", menuName = "Level/Level Collection")]
public class LevelCollection : ScriptableObject
{
    public List<LevelData> levels = new List<LevelData>();
}
