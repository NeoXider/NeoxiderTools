using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script continually aligns the rigidbody it is attached to toward a target transform;
//As a result, the rigidbody's 'up' direction will always point away from the target;
//It can be used to align a controller toward the center of a planet, for games featuring planetary gravity;
public class AlignRigidbodyToTarget : MonoBehaviour {

    //Target transform used for alignment;
    public Transform target;

    Transform tr;
    Rigidbody r;

	// Use this for initialization
	void Start () {
        tr = transform;
        r = GetComponent<Rigidbody>();

        if(target == null)
        {
            Debug.LogWarning("No target has been assigned.", this);
            this.enabled = false;
        }
	}
	
	void FixedUpdate () {

        //Get this transform's 'forward' direction;
        Vector3 _forwardDirection = tr.forward;

        //Calculate new 'up' direction;
        Vector3 _newUpDirection = (tr.position - target.position).normalized;

        //Calculate rotation between this transform's current 'up' direction and the new 'up' direction;
        Quaternion _rotationDifference = Quaternion.FromToRotation(tr.up, _newUpDirection);
        //Apply the rotation to this transform's 'forward' direction;
        Vector3 _newForwardDirection = _rotationDifference * _forwardDirection;

        //Calculate final new rotation and set this rigidbody's rotation;
        Quaternion _newRotation = Quaternion.LookRotation(_newForwardDirection, _newUpDirection);
        r.MoveRotation(_newRotation);
    }
}
