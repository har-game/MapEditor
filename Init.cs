using System.Collections;
using System.IO;
using UnityEngine;

public class Init : MonoBehaviour {
	
    IEnumerator Start() {

		System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			
        while( Error.IsWait || Loader.IsWait || MapsSelector.IsWait || MapsViewer.IsWait )
			yield return null;
			
		#if UNITY_EDITOR
		var modDir = @"D:\Projects\GTAUnity\HAR\addons\mods";
		#else
		var modDir = Path.Combine( Directory.GetParent( Directory.GetParent( Application.dataPath ).FullName ).FullName, "addons", "mods" );
		#endif
		
		HAR.Config.ModsDirectory = modDir;
		
		var wait = true;
		var hasErrors = false;
		
		HAR.Core.Map.Initialize( state => { hasErrors = !state; wait = false; } );
		
		while( wait ) yield return null;

		if( hasErrors ) { 
			Error.Activate( "Error loading maps info!" );
			yield break; 
		}

		HAR.Core.Map.Each( path => MapsSelector.AddMapInfo( path ) );
		MapsSelector.Enable = true;
		Loader.Enable = false;
		
    }

}