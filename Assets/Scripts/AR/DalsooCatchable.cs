using UnityEngine;

public class DalsooCatchable : MonoBehaviour
{
    public static DalsooCatchable Current { get; private set; }

    private void Awake()
    {
        Current = this;
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    public void Catch()
    {
        Debug.Log("달수 잡기 성공!");

        // 나중에 여기서
        // 도감 등록
        // 잡은 개수 +1
        // 효과음
        // 파티클
        // UI 표시
        // 등을 넣으면 됨.

        Destroy(gameObject);
    }
}