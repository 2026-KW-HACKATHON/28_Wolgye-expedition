using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 배치 전 가구 모양을 반투명하게 보여주는 고스트.
/// 선택된 가구가 바뀔 때만 다시 생성하고, 평소에는 위치/회전/색만 갱신합니다.
/// 이 오브젝트의 Scale은 (1,1,1)로 유지하세요.
/// </summary>
public class PlacementGhost : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    [Header("Motion")]
    [Tooltip("위치 추적 속도. 0이면 즉시 스냅.")]
    [SerializeField] private float followSpeed = 25f;
    [Tooltip("회전 추적 속도. 0이면 즉시 스냅.")]
    [SerializeField] private float rotateSpeed = 20f;

    private FurnitureData currentData;
    private GameObject ghost;
    private Renderer[] renderers;
    private bool? lastValid;
    private bool snapNext;

    public void Show(FurnitureData data, Vector3 position, Quaternion rotation, bool valid)
    {
        if (data == null || data.Prefab == null)
        {
            Hide();
            return;
        }

        if (data != currentData || ghost == null) Rebuild(data);

        if (!ghost.activeSelf)
        {
            ghost.SetActive(true);
            snapNext = true; // 다시 나타날 때는 이전 위치에서 날아오지 않도록
        }

        UpdateTransform(position, rotation);
        SetValid(valid);
    }

    public void Hide()
    {
        if (ghost != null) ghost.SetActive(false);
    }

    private void Rebuild(FurnitureData data)
    {
        if (ghost != null) Destroy(ghost);

        currentData = data;
        lastValid = null;
        snapNext = true;

        // 비활성 부모 아래에서 생성하면 가구 스크립트의 Awake/OnEnable이 실행되지 않음.
        // 그 상태에서 불필요한 컴포넌트를 제거한 뒤 활성 부모로 옮긴다.
        GameObject holder = new GameObject("GhostHolder");
        holder.SetActive(false);

        ghost = Instantiate(data.Prefab, holder.transform);
        ghost.name = $"Ghost_{data.DisplayName}";
        StripComponents(ghost);

        ghost.transform.SetParent(transform, false);
        Destroy(holder);

        renderers = ghost.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    /// <summary>스크립트, 물리, 콜라이더 제거. 고스트는 순수하게 보이기만 해야 함.</summary>
    private static void StripComponents(GameObject go)
    {
        foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            DestroyImmediate(mb);
        foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true))
            DestroyImmediate(rb);
        foreach (Collider col in go.GetComponentsInChildren<Collider>(true))
            DestroyImmediate(col);
    }

    private void UpdateTransform(Vector3 position, Quaternion rotation)
    {
        Transform t = ghost.transform;

        if (snapNext)
        {
            t.SetPositionAndRotation(position, rotation);
            snapNext = false;
            return;
        }

        // 프레임레이트와 무관하게 일정한 느낌을 주는 지수 감쇠 보간
        float dt = Time.deltaTime;
        t.position = followSpeed > 0f
            ? Vector3.Lerp(t.position, position, 1f - Mathf.Exp(-followSpeed * dt))
            : position;
        t.rotation = rotateSpeed > 0f
            ? Quaternion.Slerp(t.rotation, rotation, 1f - Mathf.Exp(-rotateSpeed * dt))
            : rotation;
    }

    private void SetValid(bool valid)
    {
        // 상태가 바뀔 때만 머티리얼 교체 (매 프레임 배열 할당 방지)
        if (lastValid == valid) return;
        lastValid = valid;

        Material mat = valid ? validMaterial : invalidMaterial;
        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }
}