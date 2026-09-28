using UnityEngine;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using System.IO;
using Newtonsoft.Json;
using Unity.VisualScripting;

public class OwnDalsus
{
    public List<OwnDalsu> characters;
}
public class OwnDalsu
{
    public string id;
    public string name;
    public string date;
}

public class DalsuSpawnManager : MonoBehaviour
{
    [SerializedDictionary("id", "DalsuData")]
    public SerializedDictionary<string, DalsuData> dalsuDatas;
    private OwnDalsus ownDalsus;

    [SerializeField] private CameraTargetSwitcher cts;
    [SerializeField] private DalsuListManager dlm;

    private void Start()
    {
        ReadOwnDalsuJson();
        SpawnDalsus();
    }

    private void ReadOwnDalsuJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "data.json");

        if (File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            ownDalsus = JsonConvert.DeserializeObject<OwnDalsus>(jsonString);
        }
        else
        {
            Debug.Log("JSON 파일이 존재하지 않습니다.");
        }
    }
    private void SpawnDalsus()
    {
        Vector3 pos, rot;
        foreach (OwnDalsu dalsu in ownDalsus.characters)
        {
            pos = new Vector3(Random.Range(-18f, 22f), 1.22f, Random.Range(-37f, 3f));
            rot = new Vector3(0, Random.Range(0, 180f), 0);
            GameObject prefab = Instantiate(dalsuDatas[dalsu.id].prefab, pos, Quaternion.Euler(rot));

            OwnDalsuData odd = prefab.GetComponent<OwnDalsuData>();
            odd.dalsuData = dalsuDatas[dalsu.id];
            cts.targets.Add(prefab.transform);
            odd.listIndex = cts.targets.Count - 1;

            dlm.AddDalsuList(odd);
        }
    }
}
