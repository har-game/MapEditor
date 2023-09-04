using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HAR.Core;
using TMPro;

public sealed class MapsViewer : MonoBehaviour {

	public static bool IsWait { get; private set; } = true;

	public static Action<bool> EnableViewer;
	public static Action<Transform> SetParentForTarget;
	public static Action<string, Vector3, Quaternion> UpdateTargetData;

	[SerializeField] private Transform cameraTransform;
    [SerializeField] private TextMeshProUGUI currentSection, cameraPosition;
	private Transform targetTransform;
	private Camera cam;
	
    private void Awake() { 
	
		EnableViewer += state => { 
			targetTransform = null; 
			gameObject.SetActive( state ); 
		};
		SetParentForTarget += newParent => {
			targetTransform.SetParent( newParent );
		};
		UpdateTargetData += (name, position, rotation) => {
			targetTransform.name = name;
			targetTransform.localPosition = position;
			targetTransform.localRotation = rotation;
		};
		
		EditObject.OnAbort += delegate { 
			targetTransform.gameObject.SetActive( true );
			EnableViewer.Invoke( true ); 
		};
		EditObject.OnDelete += delegate {
			Map.DeleteItem.Invoke( targetTransform.name );
			GameObject.Destroy( targetTransform.gameObject );
			EnableViewer.Invoke( true );
		};
		EditObject.OnUpdate += (name, position, rotation, onHour, offHour) => {
			var mapObject = new MapObject() {
				Mod = string.Empty, File = string.Empty, Position = position, Rotation = rotation, OnHour = onHour, OffHour = offHour, IsRemoved = false
			};
			Map.UpdateItem.Invoke( targetTransform.name, mapObject );
			targetTransform.gameObject.SetActive( true );
			EnableViewer.Invoke( true );
		};
		
		AddObject.SetActive += state => { gameObject.SetActive( !state ); };
		AddObject.OnSelected += (mod, file) => {
			FreeCam.DisableControll.Invoke( true );
			var position = cameraTransform.position;
			Map.AddItem.Invoke( mod, file, position );
		};
		
		Map.OnNewObjectAdded += newTransform => {
			targetTransform = newTransform;
			EditObject.Select.Invoke( targetTransform );
			FreeCam.DisableControll.Invoke( false );
			gameObject.SetActive( false );
		};
		
		cam = cameraTransform.gameObject.GetComponent<Camera>();
		gameObject.SetActive( false );
		IsWait = false;
	}
	
    private void Update() {
		Map.Stream( cameraTransform.position, out string currentSectionHash );
		currentSection.text = currentSectionHash;
		cameraPosition.text = cameraTransform.position.ToString();
		if( EditObject.HasTarget )
			return;
		if( Input.GetMouseButtonDown( 0 ) && Physics.Raycast( cam.ScreenPointToRay( Input.mousePosition ), out var hit, 9999f ) ) {
			var tmptargetTransform = getRootPickedObject( hit.transform, currentSectionHash );
			if( tmptargetTransform == null )
				return;
			targetTransform = tmptargetTransform;
			EditObject.Select.Invoke( targetTransform );
			gameObject.SetActive( false );
		}
	}
	
	private Transform getRootPickedObject( Transform transform, string currentSectionHash ) {
		if( transform == null )
			return null;
		if( transform.parent.name == currentSectionHash )
			return transform;
		return getRootPickedObject( transform.parent, currentSectionHash );
	}
	
	public void OnAbortButtonClick() {
		Loader.Enable = true;
		EnableViewer.Invoke( false );
		Map.Unload( delegate {
			MapsSelector.OnEnable.Invoke( true );
			Loader.Enable = false;
		} );
	}
	public void OnSaveButtonClick() {
		Loader.Enable = true;
		EnableViewer.Invoke( false );
		Map.Save( state => { 
			Loader.Enable = false;
			if( state ) {
				MapsSelector.OnEnable.Invoke( true );
			} else {
				Error.Activate( "Error saving maps info!" );
			}
		} );
	}
	
	public void OnAddButtonClick() { AddObject.SetActive.Invoke( true ); }

}