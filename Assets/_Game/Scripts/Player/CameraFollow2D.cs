using UnityEngine;

/// <summary>
/// Smoothly follows a target in 2D while preserving the camera's Z position.
/// Attach this component to the Main Camera and assign the player Transform.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float smoothTime = 0.15f;
    [SerializeField, Min(0f)] private float maxSpeed = 100f;
    [SerializeField] private bool followHorizontal = true;
    [SerializeField] private bool followVertical = true;

    private Vector3 followVelocity;
    private float cameraDepth;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SetupForCurrentScene()
    {
        Camera mainCamera = Camera.main;
        PlayerMovement2D player = FindFirstObjectByType<PlayerMovement2D>();

        if (mainCamera == null || player == null)
        {
            return;
        }

        CameraFollow2D cameraFollow = mainCamera.GetComponent<CameraFollow2D>();
        if (cameraFollow == null)
        {
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow2D>();
        }

        cameraFollow.SetTarget(player.transform);
    }

    private void Awake()
    {
        cameraDepth = transform.position.z;
    }

    private void OnEnable()
    {
        followVelocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 desiredPosition = new Vector3(
            followHorizontal ? target.position.x + offset.x : currentPosition.x,
            followVertical ? target.position.y + offset.y : currentPosition.y,
            cameraDepth);

        if (smoothTime <= 0f)
        {
            transform.position = desiredPosition;
            followVelocity = Vector3.zero;
            return;
        }

        transform.position = Vector3.SmoothDamp(
            currentPosition,
            desiredPosition,
            ref followVelocity,
            smoothTime,
            maxSpeed,
            Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        followVelocity = Vector3.zero;
    }
}

