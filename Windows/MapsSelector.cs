using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;
using HAR;
using HAR.Core;

public sealed class MapsSelector : MonoBehaviour {
	
	public static bool IsWait { get; private set; } = true;

	public static Action<string[]> SetFiles;
	public static Action<bool> OnEnable;

	private static string getFullMapPath( string mod, string map ) => Path.Combine( HAR.Config.ModsDirectory, mod, $"{map}.map" );
		
	public TMP_Dropdown dropdown;
	public GameObject newMapGameObject;
	public TMP_InputField[] newWindowInputs;

	void Awake() {
		OnEnable += state => { gameObject.SetActive( state ); };
		SetFiles += files => {
			for( var i = 0; i < files.Length; ++i )
				dropdown.options.Add( new TMP_Dropdown.OptionData( files[ i ] ) );
			dropdown.RefreshShownValue();
		};
		gameObject.SetActive( false ); 
		IsWait = false;
	}

	public void OnLoadSelectedMap() {
		if( dropdown.options.Count == 0 )
			return;
		var path = dropdown.options[ dropdown.value ].text;
		Loader.Enable = true;
		OnEnable.Invoke( false );
		Map.Load( path, state => {
			if( state ) {
				var paths = ( from opt in dropdown.options select opt.text ).ToArray();
				AddObject.SetMods.Invoke( paths );
				MapsViewer.EnableViewer.Invoke( true );
				Loader.Enable = false;
				return;
			}
			Error.Activate( $"Error parsing '{path}' file!" );
		} );
	}
	public void OnDeleteSelectedMap() {
		if( dropdown.options.Count == 0 )
			return;
		var path = dropdown.options[ dropdown.value ].text;
		try {
			if( File.Exists( path ) )
				File.Delete( path );
			dropdown.options.RemoveAt( dropdown.value );
			dropdown.RefreshShownValue();
		} catch( Exception e ) { API.Logger.Print( e.Message ); }
	}
	public void OnNewMapOk() {
		var modName = newWindowInputs[ 0 ].text;
		var mapName = newWindowInputs[ 1 ].text;
		if( !Config.IsValidModName( modName ) || !Config.IsValidMapName( mapName ) )
			return;
		var modDir = Path.Combine( HAR.Config.ModsDirectory, modName );
		var mapPath = Path.Combine( modDir, $"{mapName}.map" );
		try {
			if( !Directory.Exists( modDir ) )
				Directory.CreateDirectory( modDir );
			if( !File.Exists( mapPath ) )
				File.WriteAllText( mapPath, "#" );
			dropdown.options.Add( new TMP_Dropdown.OptionData( mapPath ) );
			dropdown.RefreshShownValue();
		} catch( Exception e ) { API.Logger.Print( e.Message ); return; }
		newMapGameObject.SetActive( false );
	}
	public void OnCreateNewMap() {
		newWindowInputs[ 0 ].text = string.Empty;
		newWindowInputs[ 1 ].text = string.Empty;
		newMapGameObject.SetActive( true );
	}
	public void OnNewMapAbort() => newMapGameObject.SetActive( false );
	public void OnQuitButton() => Application.Quit();
}