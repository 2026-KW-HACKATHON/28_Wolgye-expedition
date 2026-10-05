using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DalsuDatabase",
    menuName = "Dalsu/Dalsu Database"
)]
public class DalsuDatabase : ScriptableObject
{
    [SerializeField]
    private List<DalsuData> dalsuList = new List<DalsuData>();


    public DalsuData GetById(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[DALSU] ID가 비어있습니다.");
            return null;
        }


        foreach (DalsuData data in dalsuList)
        {
            if (data == null)
                continue;

            if (data.id == id)
                return data;
        }


        Debug.LogError(
            $"[DALSU] DalsuData를 찾을 수 없습니다. ID = {id}"
        );

        return null;
    }
}