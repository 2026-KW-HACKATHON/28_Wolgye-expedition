using UnityEngine;
using UnityEngine.AI;

public class AgentController : MonoBehaviour
{
    public float updateInterval = 3f; // 목표 위치를 갱신할 시간 간격 (초)
    private NavMeshAgent agent; // NavMeshAgent를 저장할 변수
    private float timeSinceLastUpdate; // 마지막으로 목표 위치를 갱신했던 시간

    void Start()
    {
        agent = GetComponent<NavMeshAgent>(); // NavMeshAgent 컴포넌트를 가져옵니다.
        timeSinceLastUpdate = updateInterval; // 초기에 목표 위치를 설정하기 위해 시간 값을 설정합니다.
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime; // 시간 값을 갱신합니다.

        if (timeSinceLastUpdate >= updateInterval) // 설정한 시간 간격이 지났는지 확인합니다.
        {
            Vector3 randomPosition = GetRandomPositionOnNavMesh(); // NavMesh 위의 랜덤한 위치를 가져옵니다.
            agent.SetDestination(randomPosition); // NavMeshAgent의 목표 위치를 랜덤 위치로 설정합니다.
            timeSinceLastUpdate = 0f; // 시간 값을 초기화합니다.
        }
    }

    Vector3 GetRandomPositionOnNavMesh()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 20f; // 원하는 범위 내의 랜덤한 방향 벡터를 생성합니다.
        randomDirection += transform.position; // 랜덤 방향 벡터를 현재 위치에 더합니다.

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 20f, NavMesh.AllAreas)) // 랜덤 위치가 NavMesh 위에 있는지 확인합니다.
        {
            return hit.position; // NavMesh 위의 랜덤 위치를 반환합니다.
        }
        else
        {
            return transform.position; // NavMesh 위의 랜덤 위치를 찾지 못한 경우 현재 위치를 반환합니다.
        }
    }
}