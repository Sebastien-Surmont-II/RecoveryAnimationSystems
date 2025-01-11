using UnityEngine;

public class FootSlopeAlignment : MonoBehaviour
{
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayLength = 1.0f;
    [SerializeField] private float rotationSpeed = 10.0f;

    [SerializeField] private Vector3 leftFootOffset = Vector3.zero; // Offset for the left foot rotation
    [SerializeField] private Vector3 rightFootOffset = Vector3.zero; // Offset for the right foot rotation

    private void Update()
    {
        AdjustFootRotation(leftFoot, leftFootOffset);
        AdjustFootRotation(rightFoot, rightFootOffset);
    }

    private void AdjustFootRotation(Transform foot, Vector3 rotationOffset)
    {
        if (Physics.Raycast(foot.position, Vector3.down, out RaycastHit hit, rayLength, groundLayer))
        {
            // Calculate the rotation to align with the slope
            Quaternion targetRotation = Quaternion.FromToRotation(foot.up, hit.normal) * foot.rotation;

            // Apply the offset to the target rotation
            targetRotation *= Quaternion.Euler(rotationOffset);

            // Smoothly interpolate the foot's rotation
            foot.rotation = Quaternion.Lerp(foot.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void OnDrawGizmos()
    {
        if (leftFoot != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(leftFoot.position, leftFoot.position + Vector3.down * rayLength);
        }

        if (rightFoot != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rightFoot.position, rightFoot.position + Vector3.down * rayLength);
        }
    }
}
