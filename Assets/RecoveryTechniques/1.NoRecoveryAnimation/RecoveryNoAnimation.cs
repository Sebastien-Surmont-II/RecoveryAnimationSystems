using UnityEngine;

public class RecoveryNoAnimation : Ragdoll
{
    public Animator animator; // Reference to the Animator component


    void Start()
    {
        // Find and store all the rigidbodies in the character's hierarchy
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();

        // Initially disable the ragdoll
        SetRagdollState(false);

        _hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
    }

    void Update()
    {
        switch (_state)
        {
            case RagdollState.Idle:
                break;
            case RagdollState.Ragdoll:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    AllignPositionToHips();
                    SetRagdollState(false);
                }
                break;
            case RagdollState.StandingUp:
                _state = RagdollState.Idle;
                break;
        }
    }

    protected override void SetRagdollState(bool enableRagdoll)
    {
        foreach (Rigidbody rb in _ragdollRigidbodies)
        {
            rb.isKinematic = !enableRagdoll; // Enable/disable physics on the rigidbodies
        }

        animator.enabled = !enableRagdoll; // Enable/disable the Animator component
    }

    private Transform _hipsBone;
    [SerializeField] private LayerMask groundLayer;
    private void AllignPositionToHips()
    {
        //Change the ragdoll holder & logic holder's gameobject's position to that of the hip's
        transform.position = _hipsBone.position;
        //Re-adjust the position with respect to the ground, using a layermask to only hit the ground
        //This ensures the ragdoll doesn't just float in the air
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2, groundLayer))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
    }
}
