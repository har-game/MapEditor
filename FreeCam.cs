using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/*

Original: https://gist.github.com/ashleydavis/f025c03a9221bc840a2b

Changed by wmysterio for MapEditor project

Camera movement			: WASD
Camera rotation			: Right mouse button
Camera to center		: R
Locate camera at target	: Space
Look camera at target	: F
Fast mode				: L.Shift, R.Shift
Input mode				: CapsLock
Target object movement	: ←↑→↓
Target object rotation	: Q/E

*/ 

public sealed class FreeCam : MonoBehaviour {
	
	public static Action<bool> DisableControll;
	
	public static bool IsWait { get; private set; } = true;
	
    private float movementSpeed = 10f, fastMovementSpeed = 100f, freeLookSensitivity = 3f, zoomSensitivity = 10f, fastZoomSensitivity = 50f;
	private bool looking, disableControll;
	private Func<KeyCode, bool> inputMode = Input.GetKeyDown;

	private void Awake() {
		DisableControll += state => { disableControll = state; };
		IsWait = false;
	}

    private void Update() {
		if( disableControll )
			return;
        var fastMode = Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift );
        var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;
		if( Input.GetKeyDown( KeyCode.R ) ) {
            transform.position = Vector3.zero;   
            transform.rotation = Quaternion.Euler( 90f, 0f, 0f );   
		}  
        if( Input.GetKeyDown( KeyCode.CapsLock ) ) 
			inputMode = inputMode == Input.GetKey ? Input.GetKeyDown : Input.GetKey;
        if( Input.GetKey( KeyCode.W ) ) {
            transform.position = transform.position + ( transform.forward * movementSpeed * Time.deltaTime );
        } else if( Input.GetKey( KeyCode.S ) ) {
            transform.position = transform.position + ( -transform.forward * movementSpeed * Time.deltaTime );
        }
        if( Input.GetKey( KeyCode.A ) ) {
            transform.position = transform.position + ( -transform.right * movementSpeed * Time.deltaTime );
        } else if( Input.GetKey( KeyCode.D ) ) {
            transform.position = transform.position + ( transform.right * movementSpeed * Time.deltaTime );
        }
        if( looking ) {
            var newRotationX = transform.localEulerAngles.y + Input.GetAxis( "Mouse X" ) * freeLookSensitivity;
            var newRotationY = transform.localEulerAngles.x - Input.GetAxis( "Mouse Y" ) * freeLookSensitivity;
            transform.localEulerAngles = new Vector3( newRotationY, newRotationX, 0f );
        }
        var axis = Input.GetAxis( "Mouse ScrollWheel" );
        if( axis != 0 ) {
            var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
            transform.position = transform.position + transform.forward * axis * zoomSensitivity;
        }
        if( Input.GetKeyDown( KeyCode.Mouse1 ) ) {
            startLooking();
        } else if( Input.GetKeyUp( KeyCode.Mouse1 ) ) {
            stopLooking();
        }
        //if( Input.GetKey( KeyCode.E ) ) {
        //    transform.position = transform.position + ( transform.up * movementSpeed * Time.deltaTime );
        //} else if( Input.GetKey( KeyCode.Q ) ) {
        //    transform.position = transform.position + ( -transform.up * movementSpeed * Time.deltaTime );
        //}
		if( !EditObject.HasTarget )
			return;
		var nextPosX = 0;
		//var nextPosY = 0;
		var nextPosZ = 0;
		var nextRotY = 0;
		var nextPosMult = fastMode ? 5 : 1;
		var nextRotMult = fastMode ? 2 : 1;
		if( inputMode.Invoke( KeyCode.LeftArrow ) ) {
			nextPosX = -1; 
		} else if( inputMode.Invoke( KeyCode.RightArrow ) ) {
			nextPosX = 1; 
		}
		if( inputMode.Invoke( KeyCode.UpArrow ) ) {
			nextPosZ = 1; 
		} else if( inputMode.Invoke( KeyCode.DownArrow ) ) {
			nextPosZ = -1; 
		}
		if( inputMode.Invoke( KeyCode.E ) ) {
			nextRotY = 1;
		} else if( inputMode.Invoke( KeyCode.Q ) ) {
			nextRotY = -1;
		}
		if( nextPosX != 0 || nextPosZ != 0 )
			EditObject.MoveRelative.Invoke( nextPosX * nextPosMult, 0, nextPosZ * nextPosMult );
		if( nextRotY != 0 )
			EditObject.RotateRelative.Invoke( nextRotY * nextRotMult );
		var targetPosition = EditObject.FetchPosition.Invoke();
		if( Input.GetKeyDown( KeyCode.Space ) ) 
			transform.position = targetPosition + new Vector3( 0f, 10f, -25f);
		if( Input.GetKeyDown( KeyCode.F ) ) 
			transform.LookAt( targetPosition );
    }

    private void OnDisable() => stopLooking();

    private void startLooking() {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void stopLooking() {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
	
}