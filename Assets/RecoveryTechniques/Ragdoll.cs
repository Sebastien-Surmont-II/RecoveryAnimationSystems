using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    protected enum RagdollState
    {
        Idle,
        Ragdoll,
        StandingUp,
        ResettingBones
    }
    protected RagdollState _state;
    protected Rigidbody[] _ragdollRigidbodies; // Array to hold all the rigidbodies in the ragdoll

    [SerializeField] private bool diesUsingHButton = true;

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (diesUsingHButton)
            {
                TriggerRagdoll(Vector3.zero, transform.position);
            }
        }
    }

    public void TriggerRagdoll(Vector3 force, Vector3 hitPoint)
    {
        SetRagdollState(true);

        Rigidbody hitRigidbody = _ragdollRigidbodies.OrderBy(
            rigidbody => Vector3.Distance(rigidbody.position, hitPoint)).First();

        hitRigidbody.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);

        _state = RagdollState.Ragdoll;
    }

    protected virtual void SetRagdollState(bool state) { }
}
