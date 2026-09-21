using UnityEngine;

public class DalsuController : MonoBehaviour
{
    private string _id;
    private DalsuSpawner _spawner;

    private bool _canCatch;

    public bool CanCatch => _canCatch;

    public void Initialize(
        string id,
        DalsuSpawner spawner)
    {
        _id = id;
        _spawner = spawner;

        Debug.Log(
            $"[DalsuController] Initialize / " +
            $"id = {_id}"
        );
    }

    public void SetCanCatch(bool canCatch)
    {
        _canCatch = canCatch;

        Debug.Log(
            $"[DalsuController] CanCatch 변경 / " +
            $"id = {_id} / " +
            $"CanCatch = {_canCatch}"
        );
    }

    public void Catch()
    {
        Debug.Log(
            $"[DalsuController] Catch 호출 / " +
            $"id = {_id} / " +
            $"CanCatch = {_canCatch}"
        );

        if (!_canCatch)
        {
            Debug.Log(
                $"[DalsuController] 아직 잡을 수 없습니다."
            );

            return;
        }

        Debug.Log(
            $"[DalsuController] 달수 잡기 성공: {gameObject.name}"
        );

        _spawner.OnDalsuCaught(_id);
    }
}