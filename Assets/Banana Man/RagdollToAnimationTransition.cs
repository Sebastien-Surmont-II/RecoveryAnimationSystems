using UnityEngine;
using RootMotion.Dynamics;
using System.Collections;

public class PuppetMasterRagdollTransition : MonoBehaviour
{
    public Animator animator;
    public PuppetMaster puppetMaster;
    public float transitionSpeed = 2f; // Speed of transition to animation
    public AnimationClip recoveryAnimation; // Animation to transition to and play

    private bool isRagdoll = true;

    void Start()
    {
        if (puppetMaster == null || animator == null)
        {
            Debug.LogError("PuppetMaster or Animator not assigned!");
            return;
        }

        // Disable automatic get-up behavior
        var behaviourPuppet = puppetMaster.GetComponent<BehaviourPuppet>();
        if (behaviourPuppet != null)
        {
            behaviourPuppet.canGetUp = false;
        }

        // Start in ragdoll state if necessary
        SetRagdollState(isRagdoll);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Toggle ragdoll/animation state with spacebar
        {
            isRagdoll = !isRagdoll;

            if (isRagdoll)
            {
                SetRagdollState(true);
            }
            else
            {
                StartCoroutine(TransitionFromRagdollToAnimation());
            }
        }
    }

    private void SetRagdollState(bool enableRagdoll)
    {
        if (enableRagdoll)
        {
            puppetMaster.state = PuppetMaster.State.Dead;
            animator.enabled = false;
        }
        else
        {
            puppetMaster.state = PuppetMaster.State.Alive;
            animator.enabled = true;
        }
    }

    private IEnumerator TransitionFromRagdollToAnimation()
    {
        puppetMaster.state = PuppetMaster.State.Alive;
        puppetMaster.pinWeight = 0f; // Start fully ragdolled
        puppetMaster.muscleWeight = 0f;

        float transitionTime = 0f;

        while (transitionTime < 1f)
        {
            transitionTime += Time.deltaTime * transitionSpeed;

            // Blend the pin and muscle weights to gradually regain control
            puppetMaster.pinWeight = Mathf.Lerp(0f, 1f, transitionTime);
            puppetMaster.muscleWeight = Mathf.Lerp(0f, 1f, transitionTime);

            yield return null;
        }

        // Ensure full control is restored
        puppetMaster.pinWeight = 1f;
        puppetMaster.muscleWeight = 1f;

        // Play the recovery animation
        animator.Play(recoveryAnimation.name);
    }
}
