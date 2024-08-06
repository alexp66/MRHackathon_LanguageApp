using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.IO;


public class _ParseTest : SerializedMonoBehaviour
{

    public Dictionary<string, string> es_en_translations = new Dictionary<string, string>();

    public string englishTestWord;
    public string spanishTestWord;

    private void Start()
    {

        ReadCSVIfNecessary();
       // byte[] rawBytes = File.ReadAllBytes(Application.persistentDataPath + "/" + "es_en" + ".bin");
        //es_en_translations = Sirenix.Serialization.SerializationUtility.DeserializeValue<Dictionary<string, string>>(rawBytes, DataFormat.Binary);
    }


    [Button]
    public void TestTranslation()
    {
        if (englishTestWord != null)
        {
            spanishTestWord = es_en_translations[englishTestWord];
        }
    }

    void ReadCSVIfNecessary()
    {
        StreamReader strReader = new StreamReader(Application.persistentDataPath + "/" + "es_en" + ".csv");
        bool endOfFile = false;
        while (!endOfFile)
        {
            string data_String = strReader.ReadLine();
            if(data_String == null)
            {
                endOfFile = true;
                byte[] storageBytes = Sirenix.Serialization.SerializationUtility.SerializeValue<Dictionary<string, string>>(es_en_translations, DataFormat.Binary);
                File.WriteAllBytes(Application.persistentDataPath + "/" + "es_en" + ".bin", storageBytes);
                break;
            }

            var data_values = data_String.Split(',');
            if(es_en_translations.ContainsKey(data_values[1]) == false)
            {
                es_en_translations.Add(data_values[1], data_values[0]);

            }
        }
    }
}
