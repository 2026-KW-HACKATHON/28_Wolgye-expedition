using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARRandomSpawner : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Camera arCamera;

    [Header("Dalsoo")]
    [SerializeField] private GameObject spawnPrefab;

    [Tooltip("달수 모델의 발 위치 보정")]
    [SerializeField] private float visualYOffset = 0f;

    [Header("Spawn Area")]
    [Tooltip("카메라와 최소 거리")]
    [SerializeField] private float minSpawnDistance = 1.0f;

    [Tooltip("카메라와 최대 거리")]
    [SerializeField] private float maxSpawnDistance = 4.0f;

    [Tooltip("휴대폰보다 이 정도 이상 아래에 있는 Plane만 바닥으로 취급")]
    [SerializeField] private float minimumBelowCamera = 0.5f;

    [Header("Hidden Spawn")]
    [Tooltip("캐릭터의 대략적인 높이. 화면 밖인지 검사할 때 사용")]
    [SerializeField] private float visibilityCheckHeight = 0.5f;

    [Tooltip("화면 가장자리 바로 옆에 생기는 것을 막는 여유값")]
    [SerializeField] private float screenPadding = 0.05f;

    [SerializeField] private int samplesPerPlane = 30;

    [SerializeField] private float retryInterval = 0.25f;

    private GameObject spawnedObject;
    private GameObject anchorRoot;

    private IEnumerator Start()
    {
        // AR Tracking 시작 기다리기
        while (ARSession.state != ARSessionState.SessionTracking)
        {
            yield return null;
        }

        Debug.Log("AR Tracking 시작");

        yield return new WaitForSeconds(1f);

        // 화면 밖의 안전한 바닥을 찾을 때까지 계속 탐색
        while (spawnedObject == null)
        {
            if (TryFindHiddenFloorPosition(out Vector3 spawnPosition))
            {
                SpawnDalsoo(spawnPosition);
                yield break;
            }

            Debug.Log("화면 밖의 인식된 바닥을 찾는 중...");
            yield return new WaitForSeconds(retryInterval);
        }
    }

    private bool TryFindHiddenFloorPosition(out Vector3 result)
    {
        result = Vector3.zero;

        if (planeManager == null || arCamera == null)
            return false;

        foreach (ARPlane plane in planeManager.trackables)
        {
            // 다른 Plane에 흡수된 Plane은 제외
            if (plane.subsumedBy != null)
                continue;

            // 위를 향하는 수평 Plane만 사용
            // = 바닥/테이블 후보
            if (plane.alignment != PlaneAlignment.HorizontalUp)
                continue;

            NativeArray<Vector2> boundary = plane.boundary;

            if (!boundary.IsCreated || boundary.Length < 3)
                continue;

            // Plane polygon의 사각 범위 계산
            Vector2 min = boundary[0];
            Vector2 max = boundary[0];

            for (int i = 1; i < boundary.Length; i++)
            {
                min = Vector2.Min(min, boundary[i]);
                max = Vector2.Max(max, boundary[i]);
            }

            // Plane 내부를 여러 번 랜덤 샘플링
            for (int attempt = 0; attempt < samplesPerPlane; attempt++)
            {
                Vector2 localPoint = new Vector2(
                    Random.Range(min.x, max.x),
                    Random.Range(min.y, max.y)
                );

                // 실제 Plane polygon 내부가 아니면 버림
                if (!IsPointInsidePolygon(localPoint, boundary))
                    continue;

                Vector3 worldPoint = plane.transform.TransformPoint(
                    new Vector3(localPoint.x, 0f, localPoint.y)
                );

                // 테이블 같은 높은 평면을 어느 정도 제외
                float belowCamera =
                    arCamera.transform.position.y - worldPoint.y;

                if (belowCamera < minimumBelowCamera)
                    continue;

                // 너무 가까이 / 너무 멀리 제외
                Vector3 horizontalDifference =
                    worldPoint - arCamera.transform.position;

                horizontalDifference.y = 0f;

                float distance = horizontalDifference.magnitude;

                if (distance < minSpawnDistance ||
                    distance > maxSpawnDistance)
                {
                    continue;
                }

                // 캐릭터 발 + 몸통이 현재 화면 안에 있으면 제외
                if (IsCurrentlyVisible(worldPoint))
                    continue;

                result = worldPoint;

                Debug.Log(
                    $"화면 밖 Spawn 위치 발견! 거리: {distance:F2}m"
                );

                return true;
            }
        }

        return false;
    }

    private bool IsCurrentlyVisible(Vector3 floorPosition)
    {
        // 발 위치
        Vector3 bottomViewport =
            arCamera.WorldToViewportPoint(floorPosition);

        // 캐릭터 몸 가운데 정도
        Vector3 centerPosition =
            floorPosition + Vector3.up * visibilityCheckHeight;

        Vector3 centerViewport =
            arCamera.WorldToViewportPoint(centerPosition);

        bool bottomVisible = IsViewportPointVisible(bottomViewport);
        bool centerVisible = IsViewportPointVisible(centerViewport);

        // 조금이라도 화면에 보일 가능성이 있으면 Spawn하지 않음
        return bottomVisible || centerVisible;
    }

    private bool IsViewportPointVisible(Vector3 viewport)
    {
        // 카메라 뒤쪽이면 화면 밖
        if (viewport.z <= 0f)
            return false;

        return viewport.x >= -screenPadding &&
               viewport.x <= 1f + screenPadding &&
               viewport.y >= -screenPadding &&
               viewport.y <= 1f + screenPadding;
    }

    private void SpawnDalsoo(Vector3 position)
    {
        if (spawnedObject != null)
            return;

        // Anchor와 달수를 분리한다.
        anchorRoot = new GameObject("DalsooAnchor");

        anchorRoot.transform.SetPositionAndRotation(
            position,
            Quaternion.identity
        );

        // 이 Transform은 이후 직접 움직이거나 회전시키지 않는다.
        anchorRoot.AddComponent<ARAnchor>();

        // 달수는 Anchor의 자식
        spawnedObject = Instantiate(
            spawnPrefab,
            anchorRoot.transform
        );

        spawnedObject.transform.localPosition =
            new Vector3(0f, visualYOffset, 0f);

        spawnedObject.transform.localRotation =
            Quaternion.identity;

        Debug.Log("달수 몰래 생성 완료!");
    }

    private bool IsPointInsidePolygon(
        Vector2 point,
        NativeArray<Vector2> polygon)
    {
        bool inside = false;

        int j = polygon.Length - 1;

        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            bool crosses =
                ((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x <
                 (pj.x - pi.x) *
                 (point.y - pi.y) /
                 (pj.y - pi.y) +
                 pi.x);

            if (crosses)
                inside = !inside;

            j = i;
        }

        return inside;
    }
}