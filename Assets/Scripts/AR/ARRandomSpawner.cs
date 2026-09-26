using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARRandomSpawner : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private Camera arCamera;

    [Header("Spawn")]
    [SerializeField] private GameObject spawnPrefab;

    [SerializeField] private float retryInterval = 0.25f;
    [SerializeField] private float searchTimeout = 5f;

    private GameObject spawnedObject;

    private static readonly List<ARRaycastHit> hits =
        new List<ARRaycastHit>();


    private IEnumerator Start()
    {
        // ARCore가 Tracking 상태가 될 때까지 기다림
        while (ARSession.state != ARSessionState.SessionTracking)
        {
            yield return null;
        }

        // 시작 직후 너무 불안정한 상태를 피함
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FindSurfaceAndSpawn());
    }


    private IEnumerator FindSurfaceAndSpawn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < searchTimeout)
        {
            if (TrySpawnOnRealSurface())
            {
                yield break;
            }

            elapsedTime += retryInterval;

            yield return new WaitForSeconds(retryInterval);
        }

        Debug.Log(
            "아직 안정적인 표면을 찾지 못했습니다."
        );
    }


    private bool TrySpawnOnRealSurface()
    {
        // 화면 아래쪽 여러 지점을 검사
        Vector2[] samplePoints =
        {
            new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.25f),

            new Vector2(
                Screen.width * 0.35f,
                Screen.height * 0.30f),

            new Vector2(
                Screen.width * 0.65f,
                Screen.height * 0.30f),

            new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.40f)
        };


        foreach (Vector2 point in samplePoints)
        {
            hits.Clear();

            bool hit =
                raycastManager.Raycast(
                    point,
                    hits,
                    TrackableType.PlaneWithinPolygon
                );

            if (!hit)
                continue;


            Pose pose = hits[0].pose;

            SpawnAnchoredObject(pose);

            return true;
        }

        return false;
    }


    private void SpawnAnchoredObject(Pose pose)
    {
        if (spawnedObject != null)
            return;


        spawnedObject =
            Instantiate(
                spawnPrefab,
                pose.position,
                Quaternion.identity
            );


        // 현실 공간에 고정
        if (spawnedObject.GetComponent<ARAnchor>() == null)
        {
            spawnedObject.AddComponent<ARAnchor>();
        }


        FaceCamera();

        Debug.Log(
            "안정적인 Plane에 Cube 생성 + Anchor 완료"
        );
    }


    private void FaceCamera()
    {
        if (spawnedObject == null)
            return;


        Vector3 direction =
            arCamera.transform.position
            - spawnedObject.transform.position;

        direction.y = 0f;


        if (direction.sqrMagnitude > 0.001f)
        {
            spawnedObject.transform.rotation =
                Quaternion.LookRotation(direction);
        }
    }
}