using System.Collections;
using System.IO;
using UnityEngine;
using System.Linq;

public class Init : MonoBehaviour {
	
    IEnumerator Start() {

		System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			
        while( Error.IsWait || Loader.IsWait || MapsSelector.IsWait || MapsViewer.IsWait || EditObject.IsWait || AddObject.IsWait || ListBox.IsWait )
			yield return null;
			
		#if UNITY_EDITOR
		var modDir = @"D:\Projects\GTAUnity\HAR\addons\mods";
		#else
		var modDir = Path.Combine( Directory.GetParent( Directory.GetParent( Application.dataPath ).FullName ).FullName, "addons", "mods" );
		#endif
		
		HAR.Config.ModsDirectory = modDir;
		
		var wait = true;
		var hasErrors = false;
		string[] mapsPaths = null;

		HAR.Core.Map.FindAllPaths( (state, paths) => { mapsPaths = paths; hasErrors = !state; wait = false; } );
		
		while( wait ) yield return null;

		if( hasErrors || mapsPaths == null ) { 
			Error.Activate( "Error loading maps info!" );
			yield break; 
		}
		
		MapsSelector.SetFiles.Invoke( mapsPaths );

		MapsSelector.OnEnable.Invoke( true );
		Loader.Enable = false;
		
    }

}