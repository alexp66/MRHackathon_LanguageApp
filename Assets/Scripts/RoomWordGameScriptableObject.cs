using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Room")]
public class RoomWordGameScriptableObject : ScriptableObject
{
    public List<SpokenWordScriptableObject> wordsForRoom = new List<SpokenWordScriptableObject>();
}
