using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Error : MonoBehaviour {

	public static bool IsWait => instance == null;

	private static Error instance;

	public static void Activate( string message ) {
		instance.errorMessage.text = message;
		instance.gameObject.SetActive( true );
	}

    public TextMeshProUGUI errorMessage;

	void Awake() { instance = this; gameObject.SetActive( false ); }
	
    void Update() {
		if( Input.GetKey( KeyCode.Return ) )
			Application.Quit();
	}
	
}