using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Word")]
public class SpokenWordScriptableObject : ScriptableObject
{
    //public Guid uniqueID;
    public string word_en;
    public string word_es;
    public List<AudioClip> audio_en = new List<AudioClip>();
    public List<AudioClip> audio_es = new List<AudioClip>();
    [HideInInspector] public List<AudioClip> userAudio_en = new List<AudioClip>();
    [HideInInspector] public List<AudioClip> userAudio_es = new List<AudioClip>();
    public GameObject model;
    public Vector3 defaultModelScale;

    [HideInInspector] public int audio_en_incrementor = 0;
    [HideInInspector] public int audio_es_incrementor = 0;
    [HideInInspector] public int userAudio_en_incrementor = 0;
    [HideInInspector] public int userAudio_es_incrementor = 0;

    
}
