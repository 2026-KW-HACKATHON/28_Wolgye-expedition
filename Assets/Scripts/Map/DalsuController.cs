using UnityEngine;

public class DalsuController : MonoBehaviour
{
    private bool _canCatch;

    public bool CanCatch => _canCatch;

    public void SetCanCatch(bool canCatch)
    {
        _canCatch = canCatch;
    }

    public void Catch()
    {
        if (!_canCatch)
            return;

        Debug.Log($"달수 선택: {gameObject.name}");

        // TODO
        // 잡기 처리
    }
}