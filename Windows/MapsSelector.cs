using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using HAR;
using HAR.Core;

public class MapsSelector : MonoBehaviour {
	
	public static bool IsWait => instance == null;

	private static MapsSelector instance;

	public static void AddMapInfo( string path ) {
		instance.dropdown.options.Add( new TMP_Dropdown.OptionData( path ) );
		instance.dropdown.RefreshShownValue();
	}
	public static bool Enable { set => instance.gameObject.SetActive( value ); }

	public TMP_Dropdown dropdown;
	public GameObject newMapGameObject;
	public TMP_InputField[] newWindowInputs;

	void Awake() { instance = this; gameObject.SetActive( false ); }

	public void OnLoadSelectedMap() {
		if( dropdown.options.Count == 0 )
			return;
		var path = dropdown.options[ dropdown.value ].text;
		Loader.Enable = true;
		Enable = false;
		Map.Load( path, state => {
			if( state ) {
				MapsViewer.Enable( true );
				Loader.Enable = false;
				return;
			}
			Error.Activate( $"Error parsing '{path}' file!" );
		} );
	}
	public void OnDeleteSelectedMap() {
		if( dropdown.options.Count == 0 )
			return;
		if( Map.Remove( dropdown.options[ dropdown.value ].text ) ) {
			dropdown.options.RemoveAt( dropdown.value );
			dropdown.RefreshShownValue();
		}
	}
	public void OnNewMapOk() {
		var modName = newWindowInputs[ 0 ].text;
		var mapName = newWindowInputs[ 1 ].text;
		if( !Config.IsValidModName( modName ) || !Config.IsValidMapName( mapName ) )
			return;
		var path = Map.GetFullPath( newWindowInputs[ 0 ].text, newWindowInputs[ 1 ].text );
		if( !Map.Add( path ) )
			return;
		AddMapInfo( path );
		newMapGameObject.SetActive( false );
	}
	public void OnCreateNewMap() {
		newWindowInputs[ 0 ].text = string.Empty;
		newWindowInputs[ 1 ].text = string.Empty;
		newMapGameObject.SetActive( true );
	}
	public void OnNewMapAbort() => newMapGameObject.SetActive( false );

}