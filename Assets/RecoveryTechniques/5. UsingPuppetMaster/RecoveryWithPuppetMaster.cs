using UnityEngine;
using RootMotion.Dynamics; // Ensure this namespace is included for PuppetMaster

public class RecoveryWithPuppetMaster : Ragdoll
{
    private struct BoneTransform
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }

    [SerializeField] private Animator animator;
    [SerializeField] private string supineStandupStateName;
    [SerializeField] private string proneStandupStateName;
    [SerializeField] private string supineStandupClipName;
    [SerializeField] private string proneStandupClipName;
    [SerializeField] private PuppetMaster puppetMaster; // Reference to PuppetMaster

    [SerializeField] private Transform _hipsBone;

    private BoneTransform[] _supineStandupAnimationBoneTransforms;
    private BoneTransform[] _proneStandupAnimationBoneTransforms;
    private BoneTransform[] _ragdollBonesTransforms;
    private Transform[] _bones;
    private bool _isSupine;

    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private GameObject ragdollHolder;

    [SerializeField, Range(0f, 1f)] private float timeToResetRootBoneDuringRecovery = .95f;

    void Start()
    {
        // Find and store all the rigidbodies in the character's hierarchy
        _ragdollRigidbodies = ragdollHolder.GetComponentsInChildren<Rigidbody>();

        SetRagdollState(false);

        //_hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
        _bones = _hipsBone.GetComponentsInChildren<Transform>();
        _supineStandupAnimationBoneTransforms = new BoneTransform[_bones.Length];
        _proneStandupAnimationBoneTransforms = new BoneTransform[_bones.Length];
        _ragdollBonesTransforms = new BoneTransform[_bones.Length];

        for (int boneIndex = 0; boneIndex < _bones.Length; boneIndex++)
        {
            _supineStandupAnimationBoneTransforms[boneIndex] = new BoneTransform();
            _proneStandupAnimationBoneTransforms[boneIndex] = new BoneTransform();
            _ragdollBonesTransforms[boneIndex] = new BoneTransform();
        }

        PopulateAnimationStartBoneTransforms(supineStandupClipName, _supineStandupAnimationBoneTransforms);
        PopulateAnimationStartBoneTransforms(proneStandupClipName, _proneStandupAnimationBoneTransforms);
    }

    void Update()
    {
        switch (_state)
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

    private void IdleBehaviour() { }

    private void RagdollBehaviour()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isSupine = CheckIfSupine();

            AlignPositionToHips();
            AlignRotationToHips();

            PopulateBoneTransforms(_ragdollBonesTransforms);

            SetState(RagdollState.ResettingBones);
        }
    }

    private bool CheckIfSupine()
    {
        return _hipsBone.forward.y > 0;
    }

    private void StandingUpBehaviour()
    {
        //Only align the root motion to the hip once the animation is almost done
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= timeToResetRootBoneDuringRecovery &&
            !onlyAllignWithIdleOnce)
        {
            AlignToIdle();
        }

        var animatorinfo = this.animator.GetCurrentAnimatorClipInfo(0);
        var current_animation = animatorinfo[0].clip.name;

        // Debug.Log("Actual clip: " + current_animation + " What it should be: " + GetStandUpStateName());
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(GetStandUpStateName()) && waitOneFrame)
        {
            Debug.Log("!animator.GetCurrentAnimatorStateInfo");
            onlyAllignWithIdleOnce = false;
            waitOneFrame = false;
            _state = RagdollState.Idle;
        }
        else { waitOneFrame = true; }
    }
    // This is a fail safe to make sure you don't get flung around and it only changes the root position once
    public bool waitOneFrame;
    public bool onlyAllignWithIdleOnce = false;

    // To make sure the player doesn't get moved back to its root position after to getting up animation is done
    // we will set the root position to be where the hip currently is.
    void AlignToIdle()
    {
        Debug.Log("AligningToIdle");
        onlyAllignWithIdleOnce = true;
        AlignPositionToHips();
    }

    private void ResettingBonesBehaviour()
    {
        animator.Play(GetStandUpStateName(), 0, 0);
        SetState(RagdollState.StandingUp);
        SetRagdollState(false);
    }

    protected override void SetRagdollState(bool enableRagdoll)
    {
        if (puppetMaster != null)
        {
            puppetMaster.state = enableRagdoll ? PuppetMaster.State.Dead : PuppetMaster.State.Alive;
        }

        /*foreach (Rigidbody rb in _ragdollRigidbodies)
        {
            rb.isKinematic = !enableRagdoll;
        }*/

        animator.enabled = !enableRagdoll;
    }

    private void AlignPositionToHips()
    {
        Vector3 hipsOriginalPosition = _hipsBone.position;
        transform.position = _hipsBone.position;

        Vector3 positionOffset = GetStandUpBoneTransforms()[0].Position;
        positionOffset.y = 0;
        positionOffset = transform.rotation * positionOffset;
        transform.position -= positionOffset;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 9999, groundLayer))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }

        _hipsBone.position = hipsOriginalPosition;
    }

    private void AlignRotationToHips()
    {
        Vector3 originalHipsPosition = _hipsBone.position;
        Quaternion originalHipsRotation = _hipsBone.rotation;

        Vector3 desiredDirection = _hipsBone.up;

        if (_isSupine)
        {
            desiredDirection *= -1;
        }

        desiredDirection.y = 0;
        desiredDirection.Normalize();

        Quaternion fromToRotation = Quaternion.FromToRotation(transform.forward, desiredDirection);
        transform.rotation *= fromToRotation;

        _hipsBone.position = originalHipsPosition;
        _hipsBone.rotation = originalHipsRotation;
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
                PopulateBoneTransforms(bonesTransforms);
                break;
            }
        }

        transform.position = positionBeforeSampling;
        transform.rotation = rotationBeforeSampling;
    }

    private string GetStandUpStateName()
    {
        return _isSupine ? supineStandupStateName : proneStandupStateName;
    }

    private BoneTransform[] GetStandUpBoneTransforms()
    {
        return _isSupine ? _supineStandupAnimationBoneTransforms : _proneStandupAnimationBoneTransforms;
    }

    void SetState(RagdollState state)
    {
        _state = state;
    }
}
