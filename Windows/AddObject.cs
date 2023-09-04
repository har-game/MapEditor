using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HAR.Core;
using TMPro;

public sealed class AddObject : MonoBehaviour {
	
	private const ushort PREVIEW_TEXTURE_SIZE = 512;
	
	public static bool IsWait { get; private set; } = true;

	public static Action<bool> SetActive;
	public static Action<string[]> SetMods;
	public static event Action<string, string> OnSelected;

	[SerializeField] private Toggle toggle;
	[SerializeField] private Camera previewCamera;
	[SerializeField] private Image previewImage;
	[SerializeField] private Transform previewRoot, loadingImageTransform;
	[SerializeField] private TMP_Dropdown dropdownMod;
	[SerializeField] private GameObject customList;
	[SerializeField] private Button abortButton, selectButton;
	[SerializeField] private Slider slider;

	private Rect targetRect = new Rect( 0f, 0f, PREVIEW_TEXTURE_SIZE, PREVIEW_TEXTURE_SIZE );
	private Vector2 targetPivot = new Vector2( 0.5f, 0.5f );
	private Texture2D targetTexture;
	private bool rotateObject = true;
	private bool isLoading => loadingImageTransform.gameObject.activeSelf;
	private GameObject previewGameObject;

    private void Awake() {
		targetTexture = new Texture2D( PREVIEW_TEXTURE_SIZE, PREVIEW_TEXTURE_SIZE, TextureFormat.RGBA32, false );
		SetMods += files => {
			setLoading( true );
			dropdownMod.value = -1;
			dropdownMod.options.Clear();
			API.Async.Run( delegate {
				var result = new string[ 0 ];
				try {
					var list = new List<string>();
					for( var i = 0; i < files.Length; ++i )
						list.Add( new DirectoryInfo( Path.GetDirectoryName( files[ i ] ) ).Name );
					result = list.ToArray(); 	
				} catch( Exception e ) { API.Logger.Print( e.Message ); }
				return result;
			}, names => { 
				for( var i = 0; i < names.Length; ++i )
					dropdownMod.options.Add( new TMP_Dropdown.OptionData( names[ i ] ) );
				dropdownMod.RefreshShownValue();
				setLoading( false );
				if( dropdownMod.options.Count > 0 )
					dropdownMod.value = 0;
			} );
		};
		SetActive += state => {
			gameObject.SetActive( state );
			FreeCam.DisableControll.Invoke( state );
			if( state ) {
				OnModChanged();
			} else {
				ListBox.Clear.Invoke();
			}
		};
		ListBox.OnLoadObjectRequest += file => {
			var mod = dropdownMod.options[ dropdownMod.value ].text;
			setLoading( true );
			OBJ.CreateNoCached( mod, file, go => {
				OnDisable();
				previewGameObject = go;
				previewGameObject.transform.SetParent( previewRoot );
				var layer = previewRoot.gameObject.layer;
				foreach( Transform t in previewGameObject.transform )
					t.gameObject.layer = layer;
				previewGameObject.layer = layer;
				previewGameObject.SetActive( true );
				setLoading( false );
			} );
			return true;
		};
		previewCamera.transform.LookAt( Vector3.zero );
		gameObject.SetActive( false );
		IsWait = false;
	}
	
	private void Update() {
		if( isLoading ) {
			loadingImageTransform.Rotate( 0f, 0f, -60f * Time.deltaTime );
			return;
		}
		if( rotateObject )
			previewRoot.Rotate( 0f, -60f * Time.deltaTime, 0f );
        var axis = Input.GetAxis( "Mouse ScrollWheel" );
		if( axis != 0f ) {
			slider.value = Mathf.Clamp( slider.value + axis * -12f, slider.minValue, slider.maxValue );
			SwitchFieldOfView();
		}
		previewImage.sprite = createSpriteFromTargetCamera();
	}
	public void OnModChanged() {
		if( isLoading || !gameObject.activeSelf || dropdownMod.options.Count == 0 || 0 > dropdownMod.value )
			return;
		setLoading( true );
		API.Async.Run( delegate {
			var result = new string[ 0 ];
			try {
				var list = new List<string>();
				var modDir = Path.Combine( HAR.Config.ModsDirectory, dropdownMod.options[ dropdownMod.value ].text );
				var objFiles = Directory.GetFiles( modDir, "*.obj" );
				foreach( var objFile in objFiles ) {
					var fileName = Path.GetFileNameWithoutExtension( new FileInfo( objFile ).Name );
					if( !HAR.Config.IsValidModName( fileName ) )
						continue;
					list.Add( fileName );
				}
				result = list.ToArray(); 	
			} catch( Exception e ) { API.Logger.Print( e.Message ); }
			return result;
		}, names => {
			ListBox.Clear.Invoke();
			for( var i = 0; i < names.Length; ++i )
				ListBox.AddItem.Invoke( names[ i ] );
			setLoading( false );
		} );
	}
	private void OnDisable() {
		if( previewGameObject != null )
			GameObject.DestroyImmediate( previewGameObject );
		previewGameObject = null;
	}

	public void OnSelect() {
		var lastItemName = ListBox.GetLastSelectedItem.Invoke();
		if( dropdownMod.options.Count == 0 || lastItemName.Length == 0 )
			return;
		var mod = dropdownMod.options[ dropdownMod.value ].text;
		OnSelected.Invoke( mod, lastItemName );
		SetActive.Invoke( false );
	}
	public void OnRotationOptionSwitched() => rotateObject = toggle.isOn;
	public void OnAbort() => SetActive.Invoke( false );
	public void SwitchFieldOfView() {
		previewCamera.transform.localPosition = new Vector3( 0f, slider.value, slider.value );
		previewCamera.transform.LookAt( Vector3.zero );
	}

	private void setLoading( bool state ) {
		var invertState = !state;
		abortButton.interactable = invertState;
		selectButton.interactable = invertState;
		dropdownMod.interactable = invertState;
		customList.SetActive( invertState );
		loadingImageTransform.gameObject.SetActive( state );
	}
	private Sprite createSpriteFromTargetCamera() {
		var currentRenderTexture = RenderTexture.active;
		RenderTexture.active = previewCamera.targetTexture;
		targetTexture.ReadPixels( targetRect, 0, 0 );
		targetTexture.Apply();
		RenderTexture.active = currentRenderTexture;
		return Sprite.Create( targetTexture, targetRect, targetPivot );
	}
	
}