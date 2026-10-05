using System;
using UnityEngine;

public class DalsooCatchable : MonoBehaviour
{
    public static DalsooCatchable Current { get; private set; }

    public event Action<DalsooCatchable> OnCaught;

    private DalsuActor actor;

    public DalsuActor Actor => actor;
    public DalsuData Data => actor != null ? actor.Data : null;


    private void Awake()
    {
        Current = this;

        actor =
            GetComponent<DalsuActor>();

        if (actor == null)
        {
            actor =
                GetComponentInParent<DalsuActor>();
        }
    }


    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }
    }


    public void Catch()
    {
        Debug.Log(
            Data != null
                ? $"[DALSU] 잡기 성공 / ID={Data.id}, Name={Data.dalsuName}"
                : "[DALSU] 잡기 성공"
        );

        OnCaught?.Invoke(this);

        Destroy(gameObject);
    }
}