using UnityEngine;
using UnityEngine.AI;
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

    [Tooltip("방 꾸미기가 끝났을 때, 가구가 없는 빈 칸을 찾기 위해 참조하는 그리드")]
    [SerializeField] private GridManager grid;

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

    // 방 꾸미기 화면이 열리면 달수 캐릭터를 통째로 비활성화한다 (NavMeshAgent도 함께 꺼지므로,
    // 가구를 배치하는 동안 달수와 가구가 겹쳐서 위치가 튀는 문제가 아예 생기지 않는다).
    // 방 꾸미기가 끝나면(active=true) 가구가 없는 빈 칸을 찾아 그 자리로 옮긴 뒤 다시 켠다.
    public void SetDalsusActive(bool active)
    {
        if (active)
            RespawnDalsusAtFreeCells();

        foreach (GameObject dalsu in spawnedDalsus)
        {
            if (dalsu != null)
                dalsu.SetActive(active);
        }
    }

    // 가구가 없는 칸들 중에서 무작위로 골라, 숨겨뒀던 달수들을 그 자리로 옮겨 놓는다.
    // (아직 비활성 상태인 동안 위치를 먼저 옮기고 나서 켜기 때문에, 켜지는 순간부터 바로 안전한 자리에 있게 된다.)
    private void RespawnDalsusAtFreeCells()
    {
        if (grid == null) return;

        List<Vector2Int> freeCells = new List<Vector2Int>();
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (grid.GetFurnitureAt(cell) == null)
                    freeCells.Add(cell);
            }

        foreach (GameObject dalsu in spawnedDalsus)
        {
            if (dalsu == null) continue;

            Vector3 pos = dalsu.transform.position;

            if (freeCells.Count > 0)
            {
                int index = Random.Range(0, freeCells.Count);
                pos = grid.GetCellCenter(freeCells[index]);
                freeCells.RemoveAt(index); // 되도록 달수끼리 같은 칸에 겹치지 않게 한다.
            }
            else
            {
                Debug.Log("빈 칸을 찾지 못해 원래 위치로 되돌립니다.");
            }

            // 그리드 칸 중심이 NavMesh 표면과 정확히 같은 높이가 아닐 수 있으므로, 가장 가까운 NavMesh 지점으로 보정한다.
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                pos = hit.position;

            dalsu.transform.position = pos;
        }
    }
}
