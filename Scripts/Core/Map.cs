using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HAR.Core {

	public sealed class Map {

		private static bool disableStreaming;
		private static int lastRow, lastColumn, currentRow, currentColumn;
		private static string lastSectionHash, lastMapFilePath;
		private static Dictionary<string, Map> loadedMaps;
		private static Dictionary<string, GameObject> streamedSections;

		private static void resetSetting() {
			(lastRow, lastColumn, currentRow, currentColumn) = (0, 0, 0, 0);
			(lastSectionHash, lastMapFilePath) = (string.Empty, string.Empty);
			disableStreaming = true;
		}
		private static void getSectionPosition( Vector3 position, out int row, out int column ) {
			row = ( int ) Math.Round( position.x / HAR.Config.SectionSize );
			column = ( int ) Math.Round( position.z / HAR.Config.SectionSize );
		}
		private static string getSectionHash( int row, int column ) => $"{row}_{column}";
		private static string getSectionHash( Vector3 position ) {
			getSectionPosition( position, out var row, out var column );
			return getSectionHash( row, column );
		}
		private static string getSectionHash( Vector3 position, out int row, out int column ) {
			getSectionPosition( position, out var tmpRow, out var tmpColumn );
			(row, column) = (tmpRow, tmpColumn);
			return getSectionHash( tmpRow, tmpColumn );
		}
		private static void generateSectionsForRebuild( Dictionary<string, MapObject[]> data, out string[] sectionToLoad, out string[] sectionToDestroy ) {
			var subLastRow = lastRow - 1;			// x - 1
			var addLastRow = lastRow + 1;			// x + 1
			var subLastCol = lastColumn - 1;		// z - 1
			var addLastCol = lastColumn + 1;		// z + 1
			var subCurrentRow = currentRow - 1;		// x - 1
			var addCurrentRow = currentRow + 1;		// x + 1
			var subCurrentCol = currentColumn - 1;	// z - 1
			var addCurrentCol = currentColumn + 1;	// z + 1
			var lastSections = new string[] {
				getSectionHash( subLastRow,	addLastCol ),
				getSectionHash( lastRow,	addLastCol ),
				getSectionHash( addLastRow,	addLastCol ),
				getSectionHash( subLastRow,	lastColumn ),
				getSectionHash( lastRow,	lastColumn ),
				getSectionHash( addLastRow,	lastColumn ),
				getSectionHash( subLastRow,	subLastCol ),
				getSectionHash( lastRow,	subLastCol ),
				getSectionHash( addLastRow,	subLastCol )
			};
			var nextSections = new string[] {
				getSectionHash( subCurrentRow, 	addCurrentCol ),
				getSectionHash( currentRow,		addCurrentCol ),
				getSectionHash( addCurrentRow,	addCurrentCol ),
				getSectionHash( subCurrentRow,	currentColumn ),
				getSectionHash( currentRow,		currentColumn ),
				getSectionHash( addCurrentRow,	currentColumn ),
				getSectionHash( subCurrentRow,	subCurrentCol ),
				getSectionHash( currentRow,		subCurrentCol ),
				getSectionHash( addCurrentRow,	subCurrentCol )
			};	
			sectionToLoad = ( from ls in lastSections from ns in nextSections where ls != ns && data.ContainsKey( ns ) select ns ).ToArray();
			sectionToDestroy = ( from ns in nextSections where !streamedSections.ContainsKey( ns ) select ns ).ToArray();
			(lastRow, lastColumn) = (currentRow, currentColumn);
		}
		private static void rebuildSections( Dictionary<string, MapObject[]> data, string[] sectionToLoad, string[] sectionToDestroy ) {
			var currentStep = 0;		
			var numSteps = 0;	
			Func<bool> isLoadingDone = delegate { return currentStep != numSteps; };
			foreach( var section in sectionToLoad ) {
				if( !streamedSections.ContainsKey( section ) )
					streamedSections.Add( section, new GameObject( section ) );
				foreach( var mapObject in data[ section ] ) {
					#if MAP_EDITOR_TOOL_ONLY
					if( mapObject.DontUse )
						continue;
					#endif
					var mo = mapObject;
					var goName = numSteps.ToString();
					numSteps += 1;
					HAR.Core.OBJ.Create( mo.Mod, mo.File, gameObject => {
						var go = GameObject.Instantiate( gameObject );
						go.name = goName;
						go.transform.SetParent( streamedSections[ section ].transform );
						go.transform.localPosition = mo.Position;
						go.transform.localRotation = mo.Rotation;
						go.SetActive( true );
						currentStep += 1;
					} );
				}
			}
			API.Async.Wait( isLoadingDone, delegate {
				for( int i = 0; i < sectionToDestroy.Length; ++i ) {
					GameObject.Destroy( streamedSections[ sectionToDestroy[ i ] ] );
					streamedSections.Remove( sectionToDestroy[ i ] );
				}
				disableStreaming = false; 
			} );
		}
		
		public static void Initialize( Action<bool> onCompleted ) {
			(loadedMaps, streamedSections) = (new Dictionary<string, Map>(), new Dictionary<string, GameObject>());
			resetSetting();
			API.Async.Run( delegate {
				try {
					var dirs = Directory.GetDirectories( Config.ModsDirectory );
					foreach( var modDir in dirs ) {
						var modName = new DirectoryInfo( modDir ).Name;
						if( !Config.IsValidModName( modName ) )
							continue;
						var mapFiles = Directory.GetFiles( modDir, "*.map" );
						foreach( var mapFile in mapFiles ) {
							var mapName = Path.GetFileNameWithoutExtension( new FileInfo( mapFile ).Name );
							if( !Config.IsValidMapName( mapName ) )
								continue;
							Add( mapFile );
						}
					}
					return true;
				} catch( Exception e ) { API.Logger.Print( e.Message ); }
				return false;
			}, state => { onCompleted.Invoke( state ); } );
		}
		public static void Each( Action<string> action ) {
			foreach( var path in loadedMaps )
				action.Invoke( path.Key );
		}
		public static string GetFullPath( string mod, string map ) => Path.Combine( HAR.Config.ModsDirectory, mod, $"{map}.map" );
		public static bool Add( string mod, string map ) => Add( GetFullPath( mod, map ) );
		public static bool Add( string path ) {
			if( loadedMaps.ContainsKey( path ) )
				return false;
			try {
				if( !File.Exists( path ) )
					File.WriteAllText( path, "#" );
				loadedMaps.Add( path, new Map( path ) );
				return true;
			} catch( Exception e ) { API.Logger.Print( e.Message ); }
			return false;
		}
		public static bool Remove( string mod, string map ) => Remove( GetFullPath( mod, map ) );
		public static bool Remove( string path ) {
			if( !loadedMaps.ContainsKey( path ) )
				return false;
			try {
				if( File.Exists( path ) )
					File.Delete( path );
				loadedMaps.Remove( path );
				return true;
			} catch( Exception e ) { API.Logger.Print( e.Message ); }
			return false;
		}
		public static void Load( string mod, string map, Action<bool> onCompleted ) => Load( GetFullPath( mod, map ), onCompleted );
		public static void Load( string path, Action<bool> onCompleted ) {
			if( path.Length == 0 || !loadedMaps.ContainsKey( path ) ) {
				onCompleted.Invoke( false );
				return;
			}
			if( lastMapFilePath.Length > 0 ) {
				if( lastMapFilePath == path ) {
					onCompleted.Invoke( true );
					return;
				}
				loadedMaps[ lastMapFilePath ].unload();
			}
			resetSetting();
			DestroyAllStreamedSections();
			loadedMaps[ path ].load( state => {
				if( state )
					lastMapFilePath = path;
				onCompleted.Invoke( state );
			} );
		}
		
		#if MAP_EDITOR_TOOL_ONLY
		public static bool CloneCurrentMapData( out Dictionary<string, MapObject[]> data ) {
			data = new Dictionary<string, MapObject[]>();
			if( lastMapFilePath.Length == 0 || !loadedMaps.ContainsKey( lastMapFilePath ) )
				return false;
			var currentMap = loadedMaps[ lastMapFilePath ];
			foreach( var dict in currentMap.staticObjects )
				data.Add( dict.Key, dict.Value );
			return true;
		}
		public static void ExportCurrentMap( Action<bool> onCompleted ) {
			API.Async.Run( delegate {
				try {
					
					return true;
				} catch( Exception e ) { API.Logger.Print( e.Message ); }
				return false;
			}, state => { onCompleted.Invoke( state ); } );
		}
		#endif
		
		public static void DestroyAllStreamedSections() {
			foreach( var item in streamedSections )
				GameObject.Destroy( item.Value );
			streamedSections.Clear();
		}
		public static void Stream( Vector3 position ) {
			if( lastMapFilePath.Length == 0 || !loadedMaps.ContainsKey( lastMapFilePath ) )
				return;
			Stream( position, loadedMaps[ lastMapFilePath ].staticObjects );
		}
		public static void Stream( Vector3 position, Dictionary<string, MapObject[]> data ) {
			if( disableStreaming )
				return;
			var currentSectionHash = getSectionHash( position, out int currentRow, out int currentColumn );
			if( currentSectionHash == lastSectionHash )
				return;
			(disableStreaming, lastSectionHash) = (true, currentSectionHash);
			API.Async.Run( delegate {
				generateSectionsForRebuild( data, out var sectionToLoad, out var sectionToDestroy );
				return (sectionToLoad, sectionToDestroy);
			}, sections => { rebuildSections( data, sections.Item1, sections.Item2 ); } );
		}
		
		private readonly string path;
		#if MAP_EDITOR_TOOL_ONLY
		private readonly StringBuilder comments; 
		#endif
		private readonly Dictionary<string, MapObject[]> staticObjects;
		
		private ZoneInfo[] zones;
		private RestartInfo[] restarts;
		private LocationTeleport[] locationTeleports;
		private Vector3[] mapTeleports;
		private Vector3 defaultPlayerPosition;
		private float defaultPlayerHeading;

		private Map( string mapPath ) {
			#if MAP_EDITOR_TOOL_ONLY
			comments = new StringBuilder();
			#endif
			(path, staticObjects) = (mapPath, new Dictionary<string, MapObject[]>());
		}

		private void unload() {
			#if MAP_EDITOR_TOOL_ONLY
			comments.Clear();
			#endif
			(zones, restarts, locationTeleports) = (new ZoneInfo[ 0 ], new RestartInfo[ 0 ], new LocationTeleport[ 0 ]);
			(mapTeleports, defaultPlayerPosition, defaultPlayerHeading) = (new Vector3[ 0 ], Vector3.zero, 0f);
			staticObjects.Clear();
		}
		private void load( Action<bool> onCompleted ) => API.Async.Run( readFile, onCompleted ); 
		private bool readFile() {
			try {
				var commentsMode = true;
				#if MAP_EDITOR_TOOL_ONLY
				var tmpComments = new StringBuilder();
				#endif
				var tmpDefaultPlayerPosition = Vector3.zero;
				var tmpDefaultPlayerHeading = 0f;
				var tmpRestarts = new List<RestartInfo>();
				var tmpMapTeleports = new List<Vector3>();
				var tmpLocationTeleports = new List<LocationTeleport>();
				var tmpZones = new List<ZoneInfo>();
				var tmpStaticObjects = new Dictionary<string, List<MapObject>>();
				using( TextReader reader = new StreamReader( path ) ) {
					while( reader.Peek() > -1 ) {
						var line = reader.ReadLine();
						var ln = Regex.Replace( line, "[ \t]{1,}", " ", RegexOptions.Compiled ).Trim();
						if( ln.Length == 0 )
							continue;
						if( ln[ 0 ] == '#' ) {
							commentsMode = false;
							continue;
						}
						if( commentsMode ) {
							#if MAP_EDITOR_TOOL_ONLY
							tmpComments.AppendLine( line );
							#endif
							continue;
						}
						var split = ln.Split( ' ' );
						var command = split[ 0 ];
						switch( command ) {
							case "OVERRIDE_DEFAULT_PLAYER_POSITION":
							tmpDefaultPlayerPosition = new Vector3( float.Parse( split[ 1 ] ), float.Parse( split[ 2 ] ), float.Parse( split[ 3 ] ) );
							tmpDefaultPlayerHeading = float.Parse( split[ 4 ] );
							continue;
							case "ADD_HOSPITAL_RESTART":
							tmpRestarts.Add( new RestartInfo( false, float.Parse( split[ 1 ] ), float.Parse( split[ 2 ] ), float.Parse( split[ 3 ] ), float.Parse( split[ 4 ] ), ushort.Parse( split[ 5 ] ) ) );
							continue;
							case "ADD_POLICE_RESTART":
							tmpRestarts.Add( new RestartInfo( true, float.Parse( split[ 1 ] ), float.Parse( split[ 2 ] ), float.Parse( split[ 3 ] ), float.Parse( split[ 4 ] ), ushort.Parse( split[ 5 ] ) ) );
							continue;
							case "ADD_MAP_TELEPORT":
							tmpMapTeleports.Add( new Vector3( float.Parse( split[ 1 ] ), float.Parse( split[ 2 ] ), float.Parse( split[ 3 ] ) ) );
							continue;
							case "ADD_LOCATION_TELEPORT":
							tmpLocationTeleports.Add( new LocationTeleport( float.Parse( split[ 1 ] ), float.Parse( split[ 2 ] ), float.Parse( split[ 3 ] ), float.Parse( split[ 4 ] ), ushort.Parse( split[ 5 ] ) ) );
							continue;
							case "ADD_ZONE":
							tmpZones.Add( new ZoneInfo( byte.Parse( split[ 1 ] ), float.Parse( split[ 2 ] ), float.Parse( split[ 3 ] ), float.Parse( split[ 4 ] ), float.Parse( split[ 5 ] ), float.Parse( split[ 6 ] ), float.Parse( split[ 7 ] ), ushort.Parse( split[ 8 ] ) ) );
							continue;
							case "ADD_OBJECT":
							var item = new MapObject( split[ 1 ], split[ 2 ], float.Parse( split[ 3 ] ), float.Parse( split[ 4 ] ), float.Parse( split[ 5 ] ), float.Parse( split[ 6 ] ), float.Parse( split[ 7 ] ), float.Parse( split[ 8 ] ) );
							var hash = getSectionHash( item.Position );
							if( !tmpStaticObjects.ContainsKey( hash ) )
								tmpStaticObjects.Add( hash, new List<MapObject>() );
							tmpStaticObjects[ hash ].Add( item );
							continue;
						}
					}
				}
				#if MAP_EDITOR_TOOL_ONLY
				tmpComments.AppendLine( "#" );
				comments.Append( tmpComments );
				#endif
				(zones, restarts, locationTeleports ) = (tmpZones.ToArray(), tmpRestarts.ToArray(), tmpLocationTeleports.ToArray());
				(mapTeleports, defaultPlayerPosition, defaultPlayerHeading) = (tmpMapTeleports.ToArray(), tmpDefaultPlayerPosition, tmpDefaultPlayerHeading);
				foreach( var item in tmpStaticObjects )
					staticObjects.Add( item.Key, item.Value.ToArray() );
				return true;
			} catch( Exception e ) { API.Logger.Print( e.Message ); }
			return false;
		}

	}	
	
}