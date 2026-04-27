using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraBoundsZone : MonoBehaviour
{
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private BoxCollider cameraBounds;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (cameraFollow != null && cameraBounds != null)
        {
            cameraFollow.SetBounds(cameraBounds);
        }
    }
}
