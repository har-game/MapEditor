using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class ListBox : MonoBehaviour {
	
	public static bool IsWait { get; private set; } = true;
	
	public static Action<string> AddItem;
	public static Action Clear;
	public static Action<int> SelectItem;
	public static Func<string> GetLastSelectedItem;
	public static event Func<string, bool> OnLoadObjectRequest;
	
	[SerializeField] private Button template;
	[SerializeField] private Transform viewportContent;
	
	private List<GameObject> cachedItems = new List<GameObject>();
	private string lastItemName = string.Empty;
	
	private void Awake() {
		Clear += delegate {
			for( int i = 0; i < cachedItems.Count; ++i ) {
				//cachedItems[ i ].GetComponentInChildren<Button>().onClick.RemoveListeners();
				Destroy( cachedItems[ i ] );
			}
			cachedItems.Clear();
			lastItemName = string.Empty;
		};
		AddItem += name => {
			var bntText = new string( name );
			var index = cachedItems.Count;
			var bnt = GameObject.Instantiate( template.gameObject ).GetComponent<Button>();
			bnt.transform.SetParent( viewportContent );
			bnt.GetComponentInChildren<TextMeshProUGUI>( true ).text = bntText;
			bnt.onClick.AddListener( delegate { SelectItem.Invoke( index ); } );
			cachedItems.Add( bnt.gameObject );
			bnt.gameObject.SetActive( true );
		};
		SelectItem += index => {
			if( cachedItems.Count == 0 || index >= cachedItems.Count )
				return;
			var bntText = cachedItems[ index ].GetComponentInChildren<TextMeshProUGUI>( true ).text;
			if( lastItemName == bntText )
				return;
			if( OnLoadObjectRequest.Invoke( bntText ) )
				lastItemName = bntText;
		};
		GetLastSelectedItem += delegate { return lastItemName; };
		template.gameObject.SetActive( false );
		IsWait = false;
	}

}