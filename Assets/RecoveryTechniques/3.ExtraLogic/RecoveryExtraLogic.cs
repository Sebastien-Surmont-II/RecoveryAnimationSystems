using UnityEngine;

public class RecoveryExtraLogic : Ragdoll
{
    private struct BoneTransform
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }

    [SerializeField] private float timeToResetBones = .6f;

    [SerializeField] private Animator animator; // Reference to the Animator component
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private Rigidbody rb;
    [SerializeField] 
    private string supineStandupStateName;
    [SerializeField] 
    private string proneStandupStateName;
    [SerializeField] 
    private string supineStandupClipName;
    [SerializeField] 
    private string proneStandupClipName;


    private Transform _hipsBone;
    [SerializeField]
    private Transform ragdollHolder;

    private BoneTransform[] _supineStandupAnimationBoneTransforms;
    private BoneTransform[] _proneStandupAnimationBoneTransforms;
    private BoneTransform[] _ragdollBonesTransforms;
    private Transform[] _bones;
    private float _elapsedResetBonesTime;
    private bool _isSupine;

    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField]
    private Vector3 extraOffsetToHips;

    void Start()
    {
        // Find and store all the rigidbodies in the character's hierarchy
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();

        // Initially disable the ragdoll
        SetRagdollState(false);

        _hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

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
        //First check if the ragdoll wants to get up, right now it's using an input, but could be a delayed function
        if (WantsToGetUp())
        {
            _isSupine = _hipsBone.forward.y > 0;


            AlignPositionToHips();
            AlignRotationToHips();

            PopulateBoneTransforms(_ragdollBonesTransforms);

            SetState(RagdollState.ResettingBones);
            _elapsedResetBonesTime = 0;
        }
    }

    private bool WantsToGetUp()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    private void StandingUpBehaviour()
    {
        //Only align the root motion to the hip once the animation is almost done
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f &&
            animator.GetCurrentAnimatorStateInfo(0).IsName(GetStandUpStateName()) && !onlyAllignWithIdleOnce)
        {
            Debug.Log("animator.GetCurrentAnimatorStateInfo(0).IsName(GetStandUpStateName())");
            AlignToIdle();
        }

        //If the animation is done, the onlyAllignWithIdleOnce can be reset & the state set to idle
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(GetStandUpStateName()))
        {
            Debug.Log("!animator.GetCurrentAnimatorStateInfo");
            onlyAllignWithIdleOnce = false;
            _state = RagdollState.Idle;
        }
    }

    // This is a fail safe to make sure you don't get flung around and it only changes the root position once
    private bool onlyAllignWithIdleOnce = false;

    // To make sure the player doesn't get moved back to its root position after to getting up animation is done
    // we will set the root position to be where the hip currently is.
    void AlignToIdle()
    {
        onlyAllignWithIdleOnce = true;
        AlignPositionToHips();
    }

    private void ResettingBonesBehaviour()
    {
        _elapsedResetBonesTime += Time.deltaTime;

        float elapsedPercentage = _elapsedResetBonesTime / timeToResetBones;

        BoneTransform[] standUpBoneTransform = GetStandUpBoneTransforms();

        //Smoothen bones to getting up animation rotation
        for (int boneIndex = 0; boneIndex < _bones.Length; boneIndex++)
        {
            _bones[boneIndex].localPosition = Vector3.Lerp(
                _ragdollBonesTransforms[boneIndex].Position,
                standUpBoneTransform[boneIndex].Position,
                elapsedPercentage);

            _bones[boneIndex].localRotation = Quaternion.Lerp(
                _ragdollBonesTransforms[boneIndex].Rotation,
                standUpBoneTransform[boneIndex].Rotation,
                elapsedPercentage);
        }

        if (elapsedPercentage >= 1)
        {
            SetState(RagdollState.StandingUp);

            SetRagdollState(false);

            animator.Play(GetStandUpStateName(), 0, 0);
        }
    }

    protected override void SetRagdollState(bool enableRagdoll)
    {
        foreach (Rigidbody rb in _ragdollRigidbodies)
        {
            rb.isKinematic = !enableRagdoll; // Enable/disable physics on the rigidbodies
        }

        animator.enabled = !enableRagdoll;
        if(capsuleCollider != null && rb != null)
        {
            capsuleCollider.enabled = !enableRagdoll;
            rb.isKinematic = enableRagdoll;
        }
    }
    private void AlignPositionToHips()
    {
        Vector3 hipsOriginalPosition = _hipsBone.position;
        ragdollHolder.position = _hipsBone.position;

        Vector3 positionOffset = GetStandUpBoneTransforms()[0].Position;
        positionOffset.y = 0;
        positionOffset = ragdollHolder.rotation * positionOffset;
        ragdollHolder.position -= (positionOffset + extraOffsetToHips);

        if (Physics.Raycast(ragdollHolder.position, Vector3.down, out RaycastHit hit, 9999, groundLayer))
        {
            ragdollHolder.position = new Vector3(ragdollHolder.position.x, hit.point.y, ragdollHolder.position.z);
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

        Quaternion fromToRotation = Quaternion.FromToRotation(ragdollHolder.forward, desiredDirection);
        ragdollHolder.rotation *= fromToRotation;

        _hipsBone.position = originalHipsPosition;
        _hipsBone.rotation = originalHipsRotation;
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
        Vector3 positionBeforeSampling = ragdollHolder.position;
        Quaternion rotationBeforeSampling = ragdollHolder.rotation;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                clip.SampleAnimation(gameObject, 0);
                PopulateBoneTransforms(bonesTransforms);
                break;
            }
        }

        ragdollHolder.position = positionBeforeSampling;
        ragdollHolder.rotation = rotationBeforeSampling;
    }

    private string GetStandUpStateName()
    {
        return _isSupine ? supineStandupStateName : proneStandupStateName;
    }
    private BoneTransform[] GetStandUpBoneTransforms()
    {
        return _isSupine ? _supineStandupAnimationBoneTransforms : _proneStandupAnimationBoneTransforms;
    }

}
