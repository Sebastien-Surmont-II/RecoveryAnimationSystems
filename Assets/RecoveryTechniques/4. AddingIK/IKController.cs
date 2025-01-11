using UnityEngine;

public class IKController : MonoBehaviour
{
    public Animator animator;

    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform rightHandRaycastTransform;
    public Transform leftHandRaycastTransform;
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform rightFootRaycastTransform;
    public Transform leftFootRaycastTransform;

    [Header("Settings")]
    [Range(0, 1)] public float handWeight = 1f; // How strongly the IK affects the hands
    [Range(0, 1)] public float footWeight = 1f; // How strongly the IK affects the feet
    [Range(-2, 2)] public float footOffset = 0.1f;
    public float handOffset = 0.1f;
    public float bodyOffset = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private bool isIdle = false;
    [SerializeField] private bool rotationIsAlignedToSlope;
    [SerializeField] private bool positionIsAlignedToSlope;
    [SerializeField] private bool isAnimationDriven = false;

    [SerializeField] private float handRaycastDistance = 2;

    private void Update()
    {
        UpdateIKState();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator)
        {
            // Hands
            //HandleHandIK();

            AdjustBodyToSlope();

            // Feet
            if (isAnimationDriven)
            {
                AdjustHandToGround(AvatarIKGoal.LeftHand, leftHandRaycastTransform, false);
                AdjustHandToGround(AvatarIKGoal.RightHand, rightHandRaycastTransform, true);
                // During animations like "get up", use animation and adjust feet alignment with ground
                AdjustFootToGround(AvatarIKGoal.LeftFoot, leftFootRaycastTransform, false);
                AdjustFootToGround(AvatarIKGoal.RightFoot, rightFootRaycastTransform, true);
            }
            else
            {
                // In idle or walking, fully use IK for feet
                /*animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);*/
                AdjustFootToGround(AvatarIKGoal.LeftFoot, leftFootTarget, false);
                AdjustFootToGround(AvatarIKGoal.RightFoot, rightFootTarget, true);
            }
        }
    }

    private void HandleHandIK()
    {
        if (leftHandTarget)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, handWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, handWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }

        if (rightHandTarget)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, handWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, handWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }
    }

    private void AdjustFootToGround(AvatarIKGoal foot, Transform raycastTransform, bool isRight)
    {
        if (raycastTransform == null) return;

        RaycastHit hit;
        Vector3 origin = raycastTransform.position + Vector3.up;

        if (Physics.Raycast(origin, Vector3.down, out hit, 2f, groundLayer))
        {
            // Set foot position
            Vector3 footPosition = hit.point + new Vector3(0, footOffset, 0);

            if(isRight) rightFootTarget.position = footPosition;
            else leftFootTarget.position = footPosition;
            animator.SetIKPositionWeight(foot, footWeight);
            animator.SetIKPosition(foot, footPosition);

            // Calculate foot forward direction
            Vector3 footForward;

            if (isAnimationDriven) // Special handling for animation-driven states like "Get Up"
            {
                // Blend foot's local forward direction with the body's forward direction
                Vector3 bodyForward = transform.forward;
                footForward = Vector3.Lerp(
                    Vector3.ProjectOnPlane(raycastTransform.forward, hit.normal).normalized,
                    Vector3.ProjectOnPlane(bodyForward, hit.normal).normalized,
                    0.7f // Adjust blend factor as needed
                ).normalized;
            }
            else
            {
                // Regular IK alignment
                footForward = Vector3.ProjectOnPlane(raycastTransform.forward, hit.normal).normalized;
            }

            // Compute foot rotation
            Quaternion footRotation = Quaternion.LookRotation(footForward, hit.normal);
            animator.SetIKRotationWeight(foot, footWeight);
            animator.SetIKRotation(foot, footRotation);
        }
    }
    private void AdjustHandToGround(AvatarIKGoal hand, Transform raycastTransform, bool isRight)
    {
        if (raycastTransform == null) return;

        RaycastHit hit;
        Vector3 origin = raycastTransform.position + Vector3.up;

        if (Physics.Raycast(origin, Vector3.down, out hit, handRaycastDistance, groundLayer))
        {
            // Set foot position
            Vector3 handPosition = hit.point + new Vector3(0, handOffset, 0);

            if (isRight) rightHandTarget.position = handPosition;
            else leftHandTarget.position = handPosition;

            animator.SetIKPositionWeight(hand, handWeight);
            animator.SetIKPosition(hand, handPosition);

            // Calculate foot forward direction
            Vector3 handForward;

            if (isAnimationDriven) // Special handling for animation-driven states like "Get Up"
            {
                // Blend foot's local forward direction with the body's forward direction
                Vector3 bodyForward = transform.forward;
                handForward = Vector3.Lerp(
                    Vector3.ProjectOnPlane(raycastTransform.forward, hit.normal).normalized,
                    Vector3.ProjectOnPlane(bodyForward, hit.normal).normalized,
                    0.7f // Adjust blend factor as needed
                ).normalized;
            }
            else
            {
                // Regular IK alignment
                handForward = Vector3.ProjectOnPlane(raycastTransform.forward, hit.normal).normalized;
            }

            // Compute foot rotation
            Quaternion handRotation = Quaternion.LookRotation(handForward, hit.normal);
            animator.SetIKRotationWeight(hand, handWeight);
            animator.SetIKRotation(hand, handRotation);
        }
    }
    private void AdjustBodyToSlope()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up;

        if (Physics.Raycast(origin, Vector3.down, out hit, 2f, groundLayer))
        {
            if (positionIsAlignedToSlope)
            {
                // Adjust body height
                transform.position = Vector3.Lerp(
                    transform.position,
                    hit.point + new Vector3(0, bodyOffset, 0),
                    0.1f // Smooth transition
                );
            }
            if (rotationIsAlignedToSlope)
            {
                // Align body rotation to slope
                Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    slopeRotation * Quaternion.Euler(0, transform.eulerAngles.y, 0), // Keep Y-axis
                    0.1f // Smooth transition
                );
            }
        }
    }



    private void UpdateIKState()
    {
        // Adjust the state based on the current animation
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        isIdle = stateInfo.IsName("Idle");
        isAnimationDriven = stateInfo.IsName("GetUpProne") || stateInfo.IsName("GetUpSupine") || stateInfo.IsName("Fall") || stateInfo.IsTag("AnimationDriven");

        // You can use tags in the Animator to generalize this further for multiple animation-driven states.
    }
}
