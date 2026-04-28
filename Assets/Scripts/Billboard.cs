using UnityEngine;

[ExecuteInEditMode]
public class Billboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool freezeY = true;
    [SerializeField] private bool useMainCamera = true;

    private void Awake()
    {
        if (useMainCamera && targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            return;
        }

        // Поворачиваем объект лицом к камере
        Vector3 cameraDirection = targetCamera.transform.forward;

        if (freezeY)
        {
            cameraDirection.y = 0f; // Убираем вертикальную компоненту для плоского поворота
        }

        if (cameraDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(cameraDirection);
        }
    }

    private void OnValidate()
    {
        if (useMainCamera && targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
}