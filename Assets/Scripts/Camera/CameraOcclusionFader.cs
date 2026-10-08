using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 카메라와 따라가는 대상 사이를 가리는 오브젝트를 디더링으로 반투명하게 만든다.
/// Main Camera(CinemachineBrain이 붙은 카메라)에 붙여서 사용한다.
///
/// 동작 방식
/// - 장애물 감지는 콜라이더가 아니라 렌더러의 바운드(로컬 박스)로 한다.
///   (가구 프리팹에는 활성화된 콜라이더가 없어서 Raycast로는 감지되지 않는다)
/// - obstacleMask 레이어에 있고 _Dither/_UseDither 프로퍼티가 있는 머티리얼(Toon 셰이더)을 쓰는 렌더러를
///   주기적으로 모아두고, 매 프레임 "카메라 → 대상" 선분이 그 박스를 지나는지 검사한다.
/// - 가리는 렌더러는 MaterialPropertyBlock으로 _UseDither = 1, _Dither 값을 1 → fadedDither로 서서히 낮춘다.
///   (Toon 셰이더: Alpha = _UseDither ? Dither(_Dither) : _Alpha, Alpha Clip Threshold = 0
///    → _Dither 1 = 완전히 보임, 0 = 완전히 사라짐)
/// - 더 이상 가리지 않으면 _Dither를 1까지 되돌린 뒤 프로퍼티 블록을 원래대로 복원한다.
/// - 머티리얼 에셋 자체는 건드리지 않으므로 같은 머티리얼을 쓰는 다른 오브젝트에는 영향이 없다.
///
/// 주의
/// - 머티리얼의 Alpha Clipping이 켜져 있어야 디더링이 보인다. (꺼져 있으면 경고 로그를 한 번 출력)
/// </summary>
[DefaultExecutionOrder(10000)] // CinemachineBrain(LateUpdate)이 카메라를 옮긴 뒤에 검사한다.
public class CameraOcclusionFader : MonoBehaviour
{
    [Header("References")]
    [Tooltip("비워두면 Camera.main")]
    [SerializeField] private Camera cam;

    [Tooltip("대상을 관리하는 CameraTargetSwitcher. 이 스위처의 CurrentTarget을 대상으로 쓰고,\n" +
             "스위처가 조종하는 시네머신 카메라가 활성(Live)일 때만 동작한다.")]
    [SerializeField] private CameraTargetSwitcher targetSwitcher;

    [Header("Detection")]
    [Tooltip("디더링 대상 레이어. 기본값은 Furniture. (Dalsu 레이어는 넣지 말 것 - 다른 달수도 Toon 셰이더를 쓴다)")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("대상의 피벗(보통 발밑)에서 얼마나 위를 조준할지")]
    [SerializeField] private float targetHeightOffset = 0.5f;

    [Tooltip("시선 선분의 두께(반지름). 클수록 대상 주변을 넓게 비워준다.")]
    [SerializeField] private float castRadius = 0.25f;

    [Tooltip("장애물 후보 렌더러 목록을 다시 모으는 간격(초). 가구가 새로 배치되면 이 간격 안에 반영된다.")]
    [SerializeField] private float rescanInterval = 0.5f;

    [Header("Dither")]
    [Tooltip("가리고 있을 때의 _Dither 값 (1 = 완전히 보임, 0 = 완전히 사라짐)")]
    [Range(0f, 1f)]
    [SerializeField] private float fadedDither = 0.3f;

    [Tooltip("초당 _Dither 변화량. 클수록 빠르게 투명해지고 빠르게 돌아온다.")]
    [SerializeField] private float fadeSpeed = 4f;

    private static readonly int DitherId = Shader.PropertyToID("_Dither");
    private static readonly int UseDitherId = Shader.PropertyToID("_UseDither");
    private const string AlphaClipKeyword = "_ALPHATEST_ON";

    private class FadeState
    {
        public Renderer renderer;
        public bool hadPropertyBlock; // 원래 프로퍼티 블록이 있었는지 (복원 방법이 달라진다)
        public float dither = 1f;
        public bool occluding;
    }

    private readonly Dictionary<Renderer, FadeState> states = new Dictionary<Renderer, FadeState>();
    private readonly List<FadeState> stateList = new List<FadeState>();
    private readonly List<Renderer> candidates = new List<Renderer>();
    private readonly HashSet<Material> warnedMaterials = new HashSet<Material>();
    private MaterialPropertyBlock block;
    private float nextRescanTime;

    // 컴포넌트를 처음 붙이거나 Reset 했을 때 기본 레이어를 Furniture로 맞춘다.
    private void Reset()
    {
        obstacleMask = LayerMask.GetMask("Furniture");
    }

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        // 레이어가 하나도 지정되지 않았으면 Furniture를 기본으로 쓴다.
        if (obstacleMask.value == 0) obstacleMask = LayerMask.GetMask("Furniture");
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;

        for (int i = 0; i < stateList.Count; i++)
            stateList[i].occluding = false;

        Transform t = ResolveTarget();
        CinemachineCamera switcherCamera = targetSwitcher != null ? targetSwitcher.TargetCamera : null;
        bool active = t != null && cam != null
                      && (switcherCamera == null || switcherCamera.IsLive); // 오버뷰/꾸미기 카메라일 때는 동작 안 함

        if (active)
        {
            if (Time.unscaledTime >= nextRescanTime)
            {
                nextRescanTime = Time.unscaledTime + rescanInterval;
                RescanCandidates();
            }

            DetectOccluders(t);
        }

        UpdateFades();
    }

    private void OnDisable()
    {
        // 비활성화/씬 전환 시 전부 원래대로 돌려놓는다.
        for (int i = 0; i < stateList.Count; i++)
            Restore(stateList[i]);

        stateList.Clear();
        states.Clear();
        candidates.Clear();
        nextRescanTime = 0f;
    }

    private Transform ResolveTarget()
    {
        return targetSwitcher != null ? targetSwitcher.CurrentTarget : null;
    }

    // obstacleMask 레이어에 있고, 디더 프로퍼티가 있는 머티리얼을 쓰는 렌더러를 모은다.
    private void RescanCandidates()
    {
        candidates.Clear();

        Renderer[] all = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Renderer r in all)
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if ((obstacleMask.value & (1 << r.gameObject.layer)) == 0) continue;
            if (!SupportsDither(r)) continue;

            candidates.Add(r);
        }
    }

    private void DetectOccluders(Transform t)
    {
        Vector3 from = cam.transform.position;
        Vector3 aim = t.position + Vector3.up * targetHeightOffset;
        Vector3 toTarget = aim - from;
        float distance = toTarget.magnitude;
        if (distance < 0.01f) return;

        // 대상 바로 앞까지만 검사 (대상 뒤나 대상이 서 있는 바닥까지 잡지 않도록)
        Vector3 to = from + toTarget * (Mathf.Max(0f, distance - castRadius) / distance);

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            Renderer r = candidates[i];
            if (r == null) { candidates.RemoveAt(i); continue; }   // 회수 등으로 파괴됨
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue; // 이동 중(숨김) 등
            if (r.transform.IsChildOf(t)) continue;                 // 대상 자신은 제외

            if (!IsBlocking(r, from, to, aim)) continue;

            FadeState state = GetOrCreateState(r);
            state.occluding = true;
        }
    }

    // 카메라 → 대상 선분이 렌더러의 박스를 지나는지.
    // MeshRenderer는 회전된 가구도 정확하도록 로컬 바운드(OBB)로, 그 외에는 월드 바운드(AABB)로 검사한다.
    private bool IsBlocking(Renderer r, Vector3 from, Vector3 to, Vector3 aim)
    {
        if (r is MeshRenderer)
        {
            Transform rt = r.transform;
            Bounds local = r.localBounds;

            // 대상이 이 가구 위/안에 있으면(침대에 누운 달수 등) 가리는 것으로 보지 않는다.
            if (local.Contains(rt.InverseTransformPoint(aim))) return false;

            Vector3 scale = rt.lossyScale;
            local.Expand(new Vector3(
                2f * castRadius / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
                2f * castRadius / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
                2f * castRadius / Mathf.Max(Mathf.Abs(scale.z), 0.0001f)));

            return SegmentIntersectsBox(rt.InverseTransformPoint(from), rt.InverseTransformPoint(to), local);
        }

        Bounds world = r.bounds;
        if (world.Contains(aim)) return false;
        world.Expand(2f * castRadius);
        return SegmentIntersectsBox(from, to, world);
    }

    // 선분 p0→p1이 박스와 겹치는지 (slab 방식)
    private static bool SegmentIntersectsBox(Vector3 p0, Vector3 p1, Bounds box)
    {
        Vector3 d = p1 - p0;
        Vector3 min = box.min;
        Vector3 max = box.max;
        float tMin = 0f, tMax = 1f;

        for (int axis = 0; axis < 3; axis++)
        {
            float o = p0[axis];
            float dir = d[axis];

            if (Mathf.Abs(dir) < 1e-6f)
            {
                if (o < min[axis] || o > max[axis]) return false;
                continue;
            }

            float inv = 1f / dir;
            float t1 = (min[axis] - o) * inv;
            float t2 = (max[axis] - o) * inv;
            if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }

            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            if (tMin > tMax) return false;
        }

        return true;
    }

    private FadeState GetOrCreateState(Renderer r)
    {
        if (states.TryGetValue(r, out FadeState existing)) return existing;

        FadeState state = new FadeState
        {
            renderer = r,
            hadPropertyBlock = r.HasPropertyBlock(),
        };

        states.Add(r, state);
        stateList.Add(state);
        return state;
    }

    // _Dither/_UseDither 프로퍼티가 있는 머티리얼(Toon 셰이더)을 하나라도 쓰는 렌더러만 처리한다.
    private bool SupportsDither(Renderer r)
    {
        bool supported = false;

        foreach (Material m in r.sharedMaterials)
        {
            if (m == null || !m.HasProperty(DitherId) || !m.HasProperty(UseDitherId)) continue;
            supported = true;

            // Alpha Clipping이 꺼져 있으면 알파(디더)가 무시되어 아무 변화가 없다.
            if (!m.IsKeywordEnabled(AlphaClipKeyword) && warnedMaterials.Add(m))
                Debug.LogWarning($"[CameraOcclusionFader] '{m.name}' 머티리얼의 Alpha Clipping이 꺼져 있어 디더링이 보이지 않습니다. " +
                                 "머티리얼 인스펙터에서 Alpha Clipping을 켜주세요.", m);
        }

        return supported;
    }

    private void UpdateFades()
    {
        float step = fadeSpeed * Time.deltaTime;

        for (int i = stateList.Count - 1; i >= 0; i--)
        {
            FadeState s = stateList[i];

            if (s.renderer == null)
            {
                // 가구가 회수되는 등으로 렌더러가 파괴됨: 목록에서만 뺀다.
                RemoveAt(i, s);
                continue;
            }

            float targetDither = s.occluding ? fadedDither : 1f;
            s.dither = Mathf.MoveTowards(s.dither, targetDither, step);

            if (!s.occluding && s.dither >= 1f)
            {
                Restore(s);
                RemoveAt(i, s);
                continue;
            }

            s.renderer.GetPropertyBlock(block);
            block.SetFloat(UseDitherId, 1f);
            block.SetFloat(DitherId, s.dither);
            s.renderer.SetPropertyBlock(block);
        }
    }

    private void RemoveAt(int index, FadeState s)
    {
        stateList.RemoveAt(index);
        states.Remove(s.renderer);
    }

    private void Restore(FadeState s)
    {
        Renderer r = s.renderer;
        if (r == null) return;

        if (!s.hadPropertyBlock)
        {
            // 원래 프로퍼티 블록이 없었으면 통째로 제거 → 머티리얼 값 그대로 (SRP Batcher도 다시 적용됨)
            r.SetPropertyBlock(null);
            return;
        }

        // 다른 스크립트가 쓰던 프로퍼티 블록이 있었으면, 우리가 바꾼 값만 머티리얼 원래 값으로 되돌린다.
        Material m = r.sharedMaterial;
        r.GetPropertyBlock(block);
        block.SetFloat(UseDitherId, m != null && m.HasProperty(UseDitherId) ? m.GetFloat(UseDitherId) : 0f);
        block.SetFloat(DitherId, m != null && m.HasProperty(DitherId) ? m.GetFloat(DitherId) : 1f);
        r.SetPropertyBlock(block);
    }
}
