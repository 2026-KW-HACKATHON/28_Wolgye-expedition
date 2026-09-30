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
    [SerializeField] private ReturnListManager rlm;
 
    [SerializeField] private GameObject aiParentPrefab;

    [Tooltip("방 꾸미기 중 달수 캐릭터를 얼마나 흐리게 보일지 (0=완전 투명, 1=원래대로)")]
    [SerializeField, Range(0f, 1f)] private float decorateFadeAlpha = 0.35f;

    // 스폰된 모든 달수 캐릭터. 방 꾸미기 화면 등에서 통째로 숨기거나 다시 보여줄 때 사용한다.
    private readonly List<GameObject> spawnedDalsus = new List<GameObject>();

    // 가구 배치 시 달수와 겹치는지 확인하는 용도 등으로, 밖에서 읽기 전용으로 참조한다.
    public IReadOnlyList<GameObject> SpawnedDalsus => spawnedDalsus;

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

        if (ownDalsus == null) ownDalsus = new OwnDalsus();
        if (ownDalsus.characters == null) ownDalsus.characters = new List<OwnDalsu>();
    }

    // data.json을 현재 ownDalsus 상태로 다시 저장합니다.
    private void SaveOwnDalsuJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "data.json");
        string jsonString = JsonConvert.SerializeObject(ownDalsus, Formatting.Indented);
        File.WriteAllText(path, jsonString);
    }

    // 돌려보내기 등에서 특정 소유 달수 레코드를 data.json에서 제거합니다.
    public void RemoveOwnDalsu(OwnDalsu dalsu)
    {
        if (dalsu == null || ownDalsus == null || ownDalsus.characters == null) return;

        if (ownDalsus.characters.Remove(dalsu))
            SaveOwnDalsuJson();
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
            odd.ownData = dalsu;
            cts.targets.Add(parentPrefab.transform);
            odd.listIndex = cts.targets.Count - 1;

            AgentController ac = parentPrefab.GetComponent<AgentController>();
            ac.animator = prefab.GetComponent<Animator>();

            dlm.AddDalsuList(odd);
            rlm.AddReturnList(odd);

            spawnedDalsus.Add(parentPrefab);
        }
    }

    // 방 꾸미기 화면 등에서 달수 캐릭터를 없애지 않고, 반투명하게 보이거나 다시 원래대로 보이게 한다.
    // (배치 작업을 방해하지 않으면서도 자리는 그대로 눈에 보이므로, 그 위에 가구를 놓으려 할 때 왜 막히는지 알 수 있다.)
    public void SetDalsusFaded(bool faded)
    {
        float alpha = faded ? decorateFadeAlpha : 1f;

        foreach (GameObject dalsu in spawnedDalsus)
        {
            if (dalsu != null)
                SetRenderersFaded(dalsu, alpha);
        }
    }

    private static void SetRenderersFaded(GameObject root, float alpha)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            // renderer.materials는 접근하는 순간 공유 에셋이 아닌, 이 렌더러만의 머티리얼 인스턴스를 만들어준다.
            // (다른 달수나 원본 에셋의 머티리얼에는 영향을 주지 않는다.)
            foreach (Material material in renderer.materials)
                SetMaterialAlpha(material, alpha);
        }
    }

    // 머티리얼은 이미 Transparent로 설정돼 있다는 전제 하에, 알파값만 바꾼다.
    private static void SetMaterialAlpha(Material material, float alpha)
    {
        if (material.HasProperty("_Alpha"))
        {
            material.SetFloat("_Alpha", alpha);
        }
    }
}
