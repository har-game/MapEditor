using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HAR.Core;

public class MapsViewer : MonoBehaviour {
	
	public static bool IsWait => instance == null;

	private static MapsViewer instance;
	private static Dictionary<string, MapObject[]> data;
	
	public static void Enable( bool state ) {
		if( !Map.CloneCurrentMapData( out data ) ) {
			Error.Activate( $"Error loading current map data!" );
			return;
		}
		instance.gameObject.SetActive( state );
	}
	
	public GameObject MapSelectorGameObject;
	public Transform CameraTransform;
	
    void Awake() { 
		gameObject.SetActive( false );
		data = new Dictionary<string, MapObject[]>();
		instance = this; 
	}

    void Update() {
		Map.Stream( CameraTransform.position, data );
	}
	
}