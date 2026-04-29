using UnityEngine;

[ExecuteInEditMode]
public class Billboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool fullBillboard = true;
    [SerializeField] private bool freezeY = true;
    [SerializeField] private bool useMainCamera = true;

    private void Awake()
    {
        ResolveCamera();
    }

    private void LateUpdate()
    {
        ResolveCamera();

        if (targetCamera == null)
        {
            return;
        }

        Vector3 cameraDirection = fullBillboard
            ? targetCamera.transform.position - transform.position
            : targetCamera.transform.forward;

        if (freezeY && !fullBillboard)
        {
            cameraDirection.y = 0f;
        }

        if (cameraDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(cameraDirection.normalized);
    }

    private void OnValidate()
    {
        ResolveCamera();
    }

    private void ResolveCamera()
    {
        if (useMainCamera && targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
}
