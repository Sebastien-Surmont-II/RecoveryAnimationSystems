using UnityEngine;

public class RootmotionController : MonoBehaviour
{
    public Animator animator;
    private Vector3 previousPosition;

    private void Start()
    {
        previousPosition = transform.position;
    }

    private void OnAnimatorMove()
    {
        if (animator.applyRootMotion)
        {
            // Calculate the root motion delta
            Vector3 rootMotionDelta = animator.deltaPosition;

            // If transitioning from "get up" to "idle", adjust position manually
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("GetUp") && animator.IsInTransition(0))
            {
                // Smoothly correct root position during the transition
                transform.position += Vector3.Lerp(rootMotionDelta, Vector3.zero, 0.5f);
            }
            else
            {
                // Default root motion handling
                transform.position += rootMotionDelta;
            }

            // Rotate with root motion
            transform.rotation *= animator.deltaRotation;
        }
    }
}
