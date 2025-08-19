using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    [Header("Follow axes")]
    [SerializeField] private bool followPitch = true; // X
    [SerializeField] private bool followYaw = true;   // Y

    [Header("Adjustments")]
    [SerializeField] private bool invertPitch = false;
    [SerializeField] private bool invertYaw = true;  
    [SerializeField] private float yawOffset = 0f;    

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        Vector3 camEuler = mainCamera.transform.eulerAngles;

        float pitch = followPitch ? (invertPitch ? -camEuler.x : camEuler.x) : transform.eulerAngles.x;
        float yaw   = followYaw   ? (invertYaw   ? -camEuler.y : camEuler.y)   : transform.eulerAngles.y;

        yaw += yawOffset;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
