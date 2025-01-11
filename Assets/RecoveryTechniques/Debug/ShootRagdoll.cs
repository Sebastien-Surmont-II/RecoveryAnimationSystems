using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootRagdoll : MonoBehaviour
{
    [SerializeField]
    private float _maximumForce;

    [SerializeField]
    private float _maximumForceTime;

    private float _timeMouseButtonDown;

    private Camera _camera;

    [SerializeField]
    private LayerMask layerMask;

    void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _timeMouseButtonDown = Time.time;
        }
        if (Input.GetMouseButtonUp(0))
        {
            HandleLogic(1);
        }
        if (Input.GetMouseButtonUp(1))
        {
            HandleLogic(-1);
        }
    }

    private void HandleLogic(int direction)
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, 999, layerMask))
        {
            Ragdoll ragdoll = hitInfo.collider.transform.root.GetComponentInChildren<Ragdoll>();

            if(ragdoll == null) ragdoll = hitInfo.collider.GetComponentInParent<Ragdoll>();

            if (ragdoll != null)
            {
                float mouseButtonDownDuration = Time.time - _timeMouseButtonDown;
                float forcePercentage = mouseButtonDownDuration / _maximumForceTime;
                float forceMagnitude = Mathf.Lerp(1, _maximumForce, forcePercentage);

                Vector3 forceDirection = ragdoll.transform.position - _camera.transform.position;
                forceDirection.y = 1;
                forceDirection.Normalize();

                Vector3 force = forceMagnitude * forceDirection * direction;

                ragdoll.TriggerRagdoll(force, hitInfo.point);
            }
        }
    }
}