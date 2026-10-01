using UnityEngine;

public class DalsooLookController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform hip;
    [SerializeField] private Animator animator;

    [Header("Whole Body Turn")]
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float stopWalkingAngle = 8f;

    [Tooltip("모델 정면이 반대면 180")]
    [SerializeField] private float modelRotationOffsetY = 0f;

    [Header("Hip Look Up / Down")]
    [SerializeField] private float hipLookSpeed = 6f;
    [SerializeField] private float maxLookUp = 20f;
    [SerializeField] private float maxLookDown = 15f;

    [Header("Vertical Look")]
    [SerializeField] private float lookUpThreshold = 30f;
    [SerializeField] private float lookDownThreshold = 15f;

    [Tooltip("임계각을 넘은 뒤 몇 도 안에 최대 회전까지 갈지")]
    [SerializeField] private float lookTransitionRange = 3f;

    private float currentHipPitch = 0f;

    private Quaternion lastHipAnimationRotation;

    private bool lookAtCameraEnabled = true;

    public void SetLookAtCamera(bool enabled)
    {
        lookAtCameraEnabled = enabled;
    }

    private void Start()
    {
        if (arCamera == null)
            arCamera = Camera.main;


    }

    public void BindModel(
    Animator newAnimator,
    Transform newHip)
    {
        animator = newAnimator;
        hip = newHip;

        Debug.Log(
            $"LookController 모델 연결 완료: {newAnimator.name}"
        );
    }
    private void Update()
    {
        if (!lookAtCameraEnabled)
            return;

        RotateWholeBody();
    }

    private void LateUpdate()
    {
        if (!lookAtCameraEnabled)
            return;

        RotateHipTowardCamera();
    }

    private void RotateWholeBody()
    {
        if (arCamera == null)
            return;

        Vector3 direction =
            arCamera.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction) *
            Quaternion.Euler(0f, modelRotationOffsetY, 0f);

        float angle =
            Quaternion.Angle(transform.rotation, targetRotation);

        bool isTurning = angle > stopWalkingAngle;

        if (animator != null)
            animator.SetBool("Walk", isTurning);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
    }

    private void RotateHipTowardCamera()
    {
        if (hip == null || arCamera == null)
            return;

        // Animator가 만든 현재 Hip 자세
        Quaternion animatedRotation = hip.rotation;

        Vector3 toCamera =
            arCamera.transform.position - hip.position;

        float horizontalDistance =
            new Vector2(toCamera.x, toCamera.z).magnitude;

        // 실제 카메라와 달수 사이의 상하 각도
        float rawPitch =
            Mathf.Atan2(
                toCamera.y,
                horizontalDistance
            ) * Mathf.Rad2Deg;


        float targetHipPitch = 0f;

        if (rawPitch > lookUpThreshold)
        {
            // 임계각을 넘으면 실제 카메라 각도를 그대로 사용
            targetHipPitch = rawPitch;

            targetHipPitch = Mathf.Min(
                targetHipPitch,
                maxLookUp
            );
        }
        else if (rawPitch < -lookDownThreshold)
        {
            targetHipPitch = rawPitch;

            targetHipPitch = Mathf.Max(
                targetHipPitch,
                -maxLookDown
            );
        }


        // 평범한 각도에서는 targetHipPitch = 0
        // → 정면 유지


        // 부드럽게 따라가기
        currentHipPitch =
            Mathf.Lerp(
                currentHipPitch,
                targetHipPitch,
                hipLookSpeed * Time.deltaTime
            );


        // 달수의 실제 얼굴 방향
        Vector3 faceDirection = transform.right;

        faceDirection.y = 0f;
        faceDirection.Normalize();


        // 현재 잘 맞았던 위/아래 회전축
        Vector3 pitchAxis =
            Vector3.Cross(
                Vector3.up,
                faceDirection
            ).normalized;


        Quaternion pitchRotation =
            Quaternion.AngleAxis(
                currentHipPitch,
                pitchAxis
            );


        hip.rotation =
            pitchRotation * animatedRotation;
    }
}
