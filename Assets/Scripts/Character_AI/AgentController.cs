using UnityEngine;
using UnityEngine.AI;

public enum MoveState
{
    IDLE,
    JUMP,
    WALK,
}

public class AgentController : MonoBehaviour
{
    public float updateInterval = 3.5f; // 목표 위치를 갱신할 시간 간격 (초)
    public float minUpdateInterval = 3f;
    public float maxUpdateInterval = 5f;

    private NavMeshAgent agent; // NavMeshAgent를 저장할 변수
    private float timeSinceLastUpdate; // 마지막으로 목표 위치를 갱신했던 시간

    [SerializeField] private Animator animator;
    [SerializeField] private MoveState moveState = MoveState.IDLE;

    void Start()
    {
        timeSinceLastUpdate = updateInterval; // 초기에 목표 위치를 설정하기 위해 시간 값을 설정합니다.
        updateInterval = Random.Range(minUpdateInterval, maxUpdateInterval);
        agent = GetComponent<NavMeshAgent>(); // NavMeshAgent 컴포넌트를 가져옵니다.
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime; // 시간 값을 갱신합니다.

        if (timeSinceLastUpdate >= updateInterval) // 설정한 시간 간격이 지났는지 확인합니다.
        {
            float state = Random.Range(0f, 1f);

            if (state < 0.1f)
            {
                moveState = MoveState.IDLE;
                animator.SetBool("Walk", false);
                agent.ResetPath();
            }
            else if (state < 0.4f)
            {
                moveState = MoveState.JUMP;
                agent.ResetPath();
                animator.SetTrigger("Jump");
                timeSinceLastUpdate = updateInterval / 2f;
            }
            else
            {
                moveState = MoveState.WALK;
                Vector3 randomPosition = GetRandomPositionOnNavMesh(); // NavMesh 위의 랜덤한 위치를 가져옵니다.
                agent.SetDestination(randomPosition); // NavMeshAgent의 목표 위치를 랜덤 위치로 설정합니다.
                animator.SetBool("Walk", true);
                timeSinceLastUpdate = 0f; // 시간 값을 초기화합니다.
            }
        }
        else if (IsAgentArrived())
        {
            animator.SetBool("Walk", false);
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

    bool IsAgentArrived()
    {
        // 경로를 계산 중이지 않고
        if (!agent.pathPending)
        {
            // 남은 거리가 정지 거리 이하이며
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                // 속도가 거의 없거나 경로가 존재하지 않는다면 도착으로 판단
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }
}