using UnityEngine;

public class RecoveryWithAnimation : Ragdoll
{
    private struct BoneTransform
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }

    [SerializeField] private float timeToResetBones = .6f;

    [SerializeField] private Animator animator; // Reference to the Animator component
    [SerializeField] private string standupStateName;
    [SerializeField] private string standupClipName;


    private Transform _hipsBone;

    private BoneTransform[] _standupAnimationBoneTransforms;
    private BoneTransform[] _ragdollBonesTransforms;
    private Transform[] _bones;
    private float _elapsedResetBonesTime;


    [SerializeField]
    private Transform ragdollHolder;
    [SerializeField]
    private Vector3 extraOffsetToHips;
    [SerializeField]
    private LayerMask groundLayer;

    void Start()
    {
        // Find and store all the rigidbodies in the character's hierarchy
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();

        // Initially disable the ragdoll
        SetRagdollState(false);

        _hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

        _bones = _hipsBone.GetComponentsInChildren<Transform>();
        _standupAnimationBoneTransforms = new BoneTransform[_bones.Length];
        _ragdollBonesTransforms = new BoneTransform[_bones.Length];

        for (int boneIndex = 0; boneIndex < _bones.Length; boneIndex++)
        {
            _standupAnimationBoneTransforms[boneIndex] = new BoneTransform();
            _ragdollBonesTransforms[boneIndex] = new BoneTransform();
        }

        if (ragdollHolder == null) ragdollHolder = transform;

        PopulateAnimationStartBoneTransforms(standupClipName, _standupAnimationBoneTransforms);
    }

    void Update()
    {
        switch(_state)
        {
            case RagdollState.Idle:
                IdleBehaviour();
                break;
            case RagdollState.Ragdoll:
                RagdollBehaviour();
                break;
            case RagdollState.StandingUp:
                StandingUpBehaviour();
                break;
            case RagdollState.ResettingBones:
                ResettingBonesBehaviour();
                break;
        }
    }

    private void IdleBehaviour()
    {
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            SetState(RagdollState.Ragdoll);
            SetRagdollState(true);
        }*/
    }

    private void RagdollBehaviour()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AllignPositionToHips();

            PopulateBoneTransforms(_ragdollBonesTransforms);

            SetState(RagdollState.ResettingBones);
            _elapsedResetBonesTime = 0;
        }
    }

    private void StandingUpBehaviour()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(standupStateName))
        {
            _state = RagdollState.Idle;
        }
    }

    private void ResettingBonesBehaviour()
    {
        _elapsedResetBonesTime += Time.deltaTime;

        float elapsedPercentage = _elapsedResetBonesTime / timeToResetBones;

        //Smoothen bones to getting up animation rotation
        for (int boneIndex = 0; boneIndex < _bones.Length; boneIndex++)
        {
            _bones[boneIndex].localPosition = Vector3.Lerp(
                _ragdollBonesTransforms[boneIndex].Position,
                _standupAnimationBoneTransforms[boneIndex].Position,
                elapsedPercentage);

            _bones[boneIndex].localRotation = Quaternion.Lerp(
                _ragdollBonesTransforms[boneIndex].Rotation,
                _standupAnimationBoneTransforms[boneIndex].Rotation,
                elapsedPercentage);
        }

        if (elapsedPercentage >= 1)
        {
            SetState(RagdollState.StandingUp);

            SetRagdollState(false);

            animator.Play(standupStateName, 0, 0);
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

    private void AllignPositionToHips()
    {
        Vector3 hipsOriginalPosition = _hipsBone.position;
        ragdollHolder.position = _hipsBone.position;

        Vector3 positionOffset = _standupAnimationBoneTransforms[0].Position;
        positionOffset.y = 0;
        positionOffset = ragdollHolder.rotation * positionOffset;
        ragdollHolder.position -= (positionOffset + extraOffsetToHips);

        if (Physics.Raycast(ragdollHolder.position, Vector3.down, out RaycastHit hit, 9999, groundLayer))
        {
            ragdollHolder.position = new Vector3(ragdollHolder.position.x, hit.point.y, ragdollHolder.position.z);
        }

        _hipsBone.position = hipsOriginalPosition;
    }

    void SetState(RagdollState state)
    {
        _state = state;
    }

    private void PopulateBoneTransforms(BoneTransform[] bonesTransforms)
    {
        for (int boneIndex = 0; boneIndex < _bones.Length; boneIndex++)
        {
            bonesTransforms[boneIndex].Position = _bones[boneIndex].localPosition;
            bonesTransforms[boneIndex].Rotation = _bones[boneIndex].localRotation;
        }
    }
    private void PopulateAnimationStartBoneTransforms(string clipName, BoneTransform[] bonesTransforms)
    {
        Vector3 positionBeforeSampling = transform.position;
        Quaternion rotationBeforeSampling = transform.rotation;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                clip.SampleAnimation(gameObject, 0);
                PopulateBoneTransforms(_standupAnimationBoneTransforms);
                break;
            }
        }

        transform.position = positionBeforeSampling;
        transform.rotation = rotationBeforeSampling;
    }
}
