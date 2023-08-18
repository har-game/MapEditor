using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Loader : MonoBehaviour {

	public static bool IsWait => instance == null;

	private static Loader instance;

	public static bool Enable { set => instance.gameObject.SetActive( value ); }

	public Image image;

	void Awake() => instance = this;
    void Update() => image.gameObject.transform.Rotate( 0f, 0f, -60f * Time.deltaTime );
	
}