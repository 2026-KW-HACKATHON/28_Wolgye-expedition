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

    [SerializeField] private GameObject aiParentPrefab;

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

            GameObject parentPrefab = Instantiate(aiParentPrefab, pos, Quaternion.Euler(rot));
            GameObject prefab = Instantiate(dalsuDatas[dalsu.id].prefab, parentPrefab.transform);
            prefab.transform.localPosition = new Vector3(0, -0.05f, 0);
            prefab.transform.localEulerAngles = new Vector3(0, 90, 0); 

            OwnDalsuData odd = parentPrefab.GetComponent<OwnDalsuData>();
            odd.dalsuData = dalsuDatas[dalsu.id];
            cts.targets.Add(parentPrefab.transform);
            odd.listIndex = cts.targets.Count - 1;

            AgentController ac = parentPrefab.GetComponent<AgentController>();
            ac.animator = prefab.GetComponent<Animator>();

            dlm.AddDalsuList(odd);
        }
    }
}
