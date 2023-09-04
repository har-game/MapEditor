using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HAR.Core;
using TMPro;

public sealed class EditObject : MonoBehaviour {
	
	public static bool IsWait { get; private set; } = true;
	public static bool HasTarget { get; private set; }

	public static Action<Transform> Select;
	public static Action<float, float, float> MoveRelative;
	public static Action<float> RotateRelative;
	public static Func<Vector3> FetchPosition;
	
	public static event Action OnDelete, OnAbort;
	public static event Action<string, Vector3, Quaternion, byte, byte> OnUpdate;

	[SerializeField] private TMP_InputField[] dataFileds;
	private Transform target;
	
    private void Awake() { 
		Select += tergetTransform => {
			var tmoGo = GameObject.Instantiate( tergetTransform.gameObject );
			tergetTransform.gameObject.SetActive( false );
			tmoGo.name = tergetTransform.name;
			tmoGo.transform.position = tergetTransform.localPosition;
			tmoGo.transform.rotation = tergetTransform.localRotation;
			target = tmoGo.transform;
			hightlightTarget();
			refreshDataFileds();
			HasTarget = true;
			gameObject.SetActive( true );
		};
		MoveRelative += (x, y, z) => {
			var pos = target.position + new Vector3( x, y, z );
			target.position = pos;
			dataFileds[ 0 ].text = pos.x.ToString();
			dataFileds[ 1 ].text = pos.y.ToString();
			dataFileds[ 2 ].text = pos.z.ToString();
		};
		RotateRelative += y => {
			target.Rotate( 0f, y, 0f );
			dataFileds[ 4 ].text = target.eulerAngles.y.ToString();
		};
		FetchPosition += delegate { return target.position; };
		gameObject.SetActive( false );
		IsWait = false;
	}
	private void hightlightTarget( float r = 0.8f, float g = 0f, float b = 0f ) {
		var mrs = target.gameObject.GetComponentsInChildren<MeshRenderer>();
		foreach( var mr in mrs )
			foreach( var m in mr.materials ) 
				m.color = new Color( r, g, b, 1f );
	}
	private void refreshDataFileds() {
		var pos = target.position;
		var rot = target.eulerAngles;
		var mapObj = Map.FindItem( target.name );
		dataFileds[ 0 ].text = pos.x.ToString();
		dataFileds[ 1 ].text = pos.y.ToString();
		dataFileds[ 2 ].text = pos.z.ToString();
		dataFileds[ 3 ].text = rot.x.ToString();
		dataFileds[ 4 ].text = rot.y.ToString();
		dataFileds[ 5 ].text = rot.z.ToString();
		dataFileds[ 6 ].text = mapObj.OnHour.ToString();
		dataFileds[ 7 ].text = mapObj.OffHour.ToString();
	}
	private void clearTarget() {
		GameObject.Destroy( target.gameObject );
		gameObject.SetActive( false );
		HasTarget = false;
		target = null;
	}
	
	public void OnAbortButtonClick() {
		OnAbort.Invoke();
		clearTarget();
	}
	public void OnDeleteObjectButtonClick() {
		OnDelete.Invoke();
		clearTarget();
	}
	public void OnSaveObjectButtonClick() {
		if( !byte.TryParse( dataFileds[ 6 ].text, out var onHour ) )
			return;
		if( !byte.TryParse( dataFileds[ 7 ].text, out var offHour ) )
			return;
		if( onHour > 23 || offHour > 23 )
			return;
		OnUpdate.Invoke( target.name, target.position, target.rotation, onHour, offHour);
		clearTarget();		
	}
	public void ChangePosition() {
		try {
			if( !float.TryParse( dataFileds[ 0 ].text, out var px ) )
				return;
			if( !float.TryParse( dataFileds[ 1 ].text, out var py ) )
				return;
			if( !float.TryParse( dataFileds[ 2 ].text, out var pz ) )
				return;
			var lastPosition = target.position;
			float newPositionX, newPositionY, newPositionZ;
			checked {
				newPositionX = px;
				newPositionY = py;
				newPositionZ = pz;
			};
			target.position = new Vector3( newPositionX, newPositionY, newPositionZ );
		} catch( Exception e ) { API.Logger.Print( e.Message ); }
	}
	public void ChangeRotation() {
		try {
			if( !float.TryParse( dataFileds[ 3 ].text, out var rx ) )
				return;
			if( !float.TryParse( dataFileds[ 4 ].text, out var ry ) )
				return;
			if( !float.TryParse( dataFileds[ 5 ].text, out var rz ) )
				return;
			var lastRotation = target.rotation;
			float newRotationX, newRotationY, newRotationZ;
			checked {
				newRotationX = rx;
				newRotationY = ry;
				newRotationZ = rz;
			};
			target.rotation = Quaternion.Euler( newRotationX, newRotationY, newRotationZ );
		} catch( Exception e ) { API.Logger.Print( e.Message ); }
	}

}