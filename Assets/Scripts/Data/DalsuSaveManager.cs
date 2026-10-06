using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class DalsuSaveManager
{
    private const string CollectedPrefix = "Dalsu_Collected_";
    private const string CountPrefix = "Dalsu_Count_";

    private const string SaveFileName = "data.json";


    // ==================================================
    // 달수 획득
    // ==================================================

    public static void AcquireDalsu(
        DalsuData dalsu)
    {
        if (dalsu == null)
        {
            Debug.LogError(
                "[DalsuSaveManager] DalsuData가 null입니다."
            );

            return;
        }


        // ==============================================
        // 1. 도감 등록
        // ==============================================

        PlayerPrefs.SetInt(
            CollectedPrefix + dalsu.id,
            1
        );


        // ==============================================
        // 2. 현재 보유 개수 +1
        // ==============================================

        int currentCount =
            GetOwnedCount(dalsu);

        PlayerPrefs.SetInt(
            CountPrefix + dalsu.id,
            currentCount + 1
        );

        PlayerPrefs.Save();


        // ==============================================
        // 3. data.json 저장
        // ==============================================

        SaveDalsuToJson(dalsu);


        Debug.Log(
            $"[DalsuSaveManager] " +
            $"{dalsu.dalsuName} 획득! " +
            $"현재 {currentCount + 1}개 보유"
        );
    }


    // ==================================================
    // data.json에 달수 저장
    // ==================================================

    private static void SaveDalsuToJson(
        DalsuData dalsu)
    {
        string path =
            Path.Combine(
                Application.persistentDataPath,
                SaveFileName
            );


        OwnDalsus ownDalsus;


        // ==============================================
        // 기존 data.json 읽기
        // ==============================================

        if (File.Exists(path))
        {
            try
            {
                string json =
                    File.ReadAllText(path);

                ownDalsus =
                    JsonConvert.DeserializeObject<OwnDalsus>(
                        json
                    );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[DalsuSaveManager] " +
                    $"data.json 읽기 실패: {e.Message}"
                );

                ownDalsus =
                    new OwnDalsus();
            }
        }
        else
        {
            Debug.Log(
                "[DalsuSaveManager] " +
                "data.json이 없어 새로 생성합니다."
            );

            ownDalsus =
                new OwnDalsus();
        }


        // ==============================================
        // characters 초기화
        // ==============================================

        if (ownDalsus.characters == null)
        {
            ownDalsus.characters =
                new List<OwnDalsu>();
        }


        // ==============================================
        // 새로운 달수 데이터 생성
        // ==============================================

        OwnDalsu newDalsu =
            new OwnDalsu
            {
                id = dalsu.id,
                name = dalsu.dalsuName,
                date = DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss"
                )
            };


        ownDalsus.characters.Add(
            newDalsu
        );


        // ==============================================
        // JSON 직렬화
        // ==============================================

        string outputJson =
            JsonConvert.SerializeObject(
                ownDalsus,
                Formatting.Indented
            );


        // ==============================================
        // 파일 저장
        // ==============================================

        File.WriteAllText(
            path,
            outputJson
        );


        Debug.Log(
            $"[DalsuSaveManager] " +
            $"data.json 저장 완료: {path}"
        );
    }


    // ==================================================
    // 한 번이라도 획득했는지
    // ==================================================

    public static bool IsCollected(
        DalsuData dalsu)
    {
        return PlayerPrefs.GetInt(
            CollectedPrefix + dalsu.id,
            0
        ) == 1;
    }


    // ==================================================
    // 현재 몇 개 보유 중인지
    // ==================================================

    public static int GetOwnedCount(
        DalsuData dalsu)
    {
        return PlayerPrefs.GetInt(
            CountPrefix + dalsu.id,
            0
        );
    }


    // ==================================================
    // 현재 보유 여부
    // ==================================================

    public static bool IsOwned(
        DalsuData dalsu)
    {
        return GetOwnedCount(dalsu) > 0;
    }


    // ==================================================
    // 달수 사용
    // ==================================================

    public static bool UseDalsu(
        DalsuData dalsu)
    {
        int count =
            GetOwnedCount(dalsu);

        if (count <= 0)
        {
            return false;
        }


        PlayerPrefs.SetInt(
            CountPrefix + dalsu.id,
            count - 1
        );

        PlayerPrefs.Save();


        return true;
    }
}