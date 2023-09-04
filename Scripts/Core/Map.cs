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
		
		static Map() {
			DeleteItem = name => {
				var split = name.Split( '_' );
				currentMap.objects[ split[ 0 ] ][ int.Parse( split[ 1 ] ) ].IsRemoved = true;	
			};
			FindItem = name => {
				var split = name.Split( '_' );
				return currentMap.objects[ split[ 0 ] ][ int.Parse( split[ 1 ] ) ];
			};
			UpdateItem = ( name, data ) => {
				var split = name.Split( '_' );
				var oldSection = split[ 0 ];
				var oldIndex = int.Parse( split[ 1 ] );
				var oldData = currentMap.objects[ oldSection ][ oldIndex ];
				data.Mod = oldData.Mod;
				data.File = oldData.File;
				currentMap.objects[ oldSection ][ oldIndex ].IsRemoved = true;
				var newSection = getSectionHash( data.Position );
				var newIndex = 0;
				if( newSection == oldSection ) {
					newIndex = currentMap.objects[ newSection ].Length;
				} else {
					if( currentMap.objects.ContainsKey( newSection ) ) {
						newIndex = currentMap.objects[ newSection ].Length;
					} else {
						currentMap.objects.Add( newSection, new MapObject[ 0 ] );
						var newSectionTransform = new GameObject( newSection ).transform;
						currentMap.sections.Add( newSection, newSectionTransform );
						newSectionTransform.SetParent( currentMap.root );
					}
					MapsViewer.SetParentForTarget.Invoke( currentMap.sections[ newSection ] );
				}
				var arr = currentMap.objects[ newSection ];
				var newHash = $"{newSection}_{newIndex}";
				Array.Resize( ref arr, newIndex + 1 );
				currentMap.objects[ newSection ] = arr;
				currentMap.objects[ newSection ][ newIndex ] = data;
				MapsViewer.UpdateTargetData.Invoke( newHash, data.Position, data.Rotation );
			};
			AddItem += (mod, file, position ) => {
				OBJ.Create( mod, file, go => {
					var section = getSectionHash( position );
					if( !currentMap.sections.ContainsKey( section ) ) {
						var newSectionTransform = new GameObject( section ).transform;
						currentMap.sections.Add( section, newSectionTransform );
						currentMap.objects.Add( section, new MapObject[ 0 ] );
						newSectionTransform.SetParent( currentMap.root );
					}
					var arr = currentMap.objects[ section ];
					var index = currentMap.objects[ section ].Length;
					var hash = $"{section}_{index}";
					var data = new MapObject() {
						Mod = mod, File = file, Position = position,
						Rotation = Quaternion.Euler( 0f, 0f, 0f ),
						OnHour = 0, OffHour = 0, IsRemoved = false
					};
					Array.Resize( ref arr, index + 1 );
					go.transform.SetParent( currentMap.sections[ section ] );
					go.transform.position = position;
					go.name = hash;
					currentMap.objects[ section ] = arr;
					currentMap.objects[ section ][ index ] = data;
					go.SetActive( true );
					OnNewObjectAdded.Invoke( go.transform );
				} );
			};
		}

		public static event Action<Transform> OnNewObjectAdded;
		public static Action<string, string, Vector3> AddItem;
		public static Action<string> DeleteItem;
		public static Func<string, MapObject> FindItem;
		public static Action<string, MapObject> UpdateItem;
		
		public static void Load( string path, Action<bool> onCompleted ) => new Map( path, onCompleted );
		public static void FindAllPaths( Action<bool, string[]> onCompleted ) {
			string[] result = null;
			API.Async.Run( delegate {
				try {
					var tmpResult = new Dictionary<string, bool>();
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
							tmpResult.Add( mapFile, false );
						}
					}
					result = tmpResult.Keys.ToArray();
					return true;
				} catch( Exception e ) { API.Logger.Print( e.Message ); }
				return false;
			}, state => { onCompleted.Invoke( state, result ); } );
		}
		public static void Stream( Vector3 position, out string currentSectionHash ) {
			currentSectionHash = string.Empty;
			if( currentMap == null )
				return;
			currentMap.stream( position, out currentSectionHash );
		} 
		public static void Unload( Action onCompleted ) {
			if( currentMap != null ) {
				currentMap.unload();
				currentMap = null;
			}
			onCompleted.Invoke();
			return;
		}
		public static void Save( Action<bool> onCompleted ) {
			if( currentMap == null ) {
				onCompleted.Invoke( true );
				return;
			}
			currentMap.export( onCompleted );
		}

		private const byte MAX_SECTIONS_COUNT = 9;
		private static float SECTION_SIZE = 300f;
		
		private static Map currentMap = null;
	
		private static void getSectionPosition( Vector3 position, out int row, out int column ) {
			column = ( int ) Math.Round( position.x / SECTION_SIZE );
			row = ( int ) Math.Round( position.z / SECTION_SIZE );
		}
		private static string getSectionHash( int row, int column ) => $"{column}x{row}";
		private static string getSectionHash( Vector3 position ) {
			getSectionPosition( position, out var row, out var column );
			return getSectionHash( row, column );
		}
		private static string getSectionHash( Vector3 position, out int row, out int column ) {
			getSectionPosition( position, out var tmpRow, out var tmpColumn );
			(row, column) = (tmpRow, tmpColumn);
			return getSectionHash( tmpRow, tmpColumn );
		}

		private string lastSectionHash, currentMapPath;
		private Transform root;
		private Dictionary<string, MapObject[]> objects;
		private Dictionary<string, Transform> sections;
		private StringBuilder comments;
		
		private string[] lastSectionsHashes;
		
		private Map( string path, Action<bool> onCompleted ) {
			if( currentMap != null ) {	
				if( currentMap.currentMapPath == path ) {
					onCompleted.Invoke( true );
					return;
				}
				unload();
			}
			lastSectionsHashes = new string[ MAX_SECTIONS_COUNT ];
			for( int i = 0; i < MAX_SECTIONS_COUNT; ++i )
				lastSectionsHashes[ i ] = string.Empty;
			(currentMapPath, lastSectionHash) = (path, string.Empty);
			(objects, sections, comments) = (new Dictionary<string, MapObject[]>(), new Dictionary<string, Transform>(), new StringBuilder());
			root = new GameObject( "MAP" ).transform;
			readFile( path, state => {
				if( !state ) {
					onCompleted.Invoke( false );
					return;
				}
				var currentStep = 0;		
				var numSteps = 0;
				foreach( var o in objects ) {
					var indexName = 0;
					var mapObject = o.Value;
					var namePrefix = new String( o.Key );
					var sectionTransform = new GameObject( namePrefix ).transform;
					sectionTransform.SetParent( root );
					sections.Add( namePrefix, sectionTransform );
					foreach( var item in mapObject ) {
						var mo = item;
						var goName = $"{namePrefix}_{indexName}";
						numSteps += 1;
						indexName += 1;
						HAR.Core.OBJ.Create( mo.Mod, mo.File, gameObject => {
							var go = GameObject.Instantiate( gameObject );
							go.name = goName;
							go.transform.SetParent( sectionTransform );
							go.transform.localPosition = mo.Position;
							go.transform.localRotation = mo.Rotation;
							go.SetActive( true );
							currentStep += 1;
						} );
					}
					sectionTransform.gameObject.SetActive( false );
				}
				Func<bool> isDone = delegate { return currentStep != numSteps; };
				API.Async.Wait( isDone, delegate {
					currentMap = this;
					onCompleted.Invoke( state ); 
				} );
			} );
		}
		
		private void unload() {
			GameObject.Destroy( root.gameObject );
			HAR.Core.OBJ.ClearCaches();
			comments.Clear();
			objects.Clear();
			sections.Clear();
			(lastSectionsHashes, currentMap) = (null, null);
		}
		private void readFile( string path, Action<bool> onCompleted ) {
			API.Async.Run( delegate {
				try {
					var tmpObjects = new Dictionary<string, List<MapObject>>();
					var tmpComments = new StringBuilder();
					var commentsMode = true;
					using( TextReader reader = new StreamReader( path ) ) {
						while( reader.Peek() > -1 ) {
							var line = reader.ReadLine();
							var ln = Regex.Replace( line, "[ \t]{1,}", " ", RegexOptions.Compiled ).Trim();
							if( ln.Length == 0 ) {
								tmpComments.AppendLine( line );
								continue;
							}
							if( ln[ 0 ] == '#' ) {
								commentsMode = false;
								tmpComments.AppendLine( line );
								continue;
							}
							if( commentsMode ) {
								tmpComments.AppendLine( line );
								continue;
							}
							var split = ln.Split( ' ' );
							if( split[ 0 ] != "ADD_OBJECT" ) {
								tmpComments.AppendLine( line );
								continue;
							}
							var item = new MapObject() {
								Mod = split[ 1 ],
								File = split[ 2 ],
								Position = new Vector3( 
									float.Parse( split[ 3 ] ), 
									float.Parse( split[ 4 ] ), 
									float.Parse( split[ 5 ] )
								), 
								Rotation = Quaternion.Euler( 
									float.Parse( split[ 6 ] ),
									float.Parse( split[ 7 ] ),
									float.Parse( split[ 8 ] ) 
								),
								OnHour = byte.Parse( split[ 9 ] ),
								OffHour = byte.Parse( split[ 10 ] ),
								IsRemoved = false
							};
							var fileName = Path.Combine( HAR.Config.ModsDirectory, split[ 1 ], $"{split[ 2 ]}.obj" );
							if( !File.Exists( fileName ) ) {
								tmpComments.AppendLine( line );
								continue;
							}
							
							// if ! exsist move to comment!!!
							var hash = getSectionHash( item.Position );
							if( !tmpObjects.ContainsKey( hash ) )
								tmpObjects.Add( hash, new List<MapObject>() );
							tmpObjects[ hash ].Add( item );
						}	
					}
					if( commentsMode )
						tmpComments.AppendLine( "#" );
					comments.Append( tmpComments );
					foreach( var item in tmpObjects )
						objects.Add( item.Key, item.Value.ToArray() );
					return true;
				} catch( Exception e ) { API.Logger.Print( e.Message ); }
				return false;
			}, state => { onCompleted.Invoke( state ); } );
		}
		private void stream( Vector3 position, out string currentSectionHash ) {
			currentSectionHash = getSectionHash( position, out int currentRow, out int currentColumn );
			if( currentSectionHash == lastSectionHash )
				return;
				
			var currentSectionsHashes = getCurrentSectionsHashes( currentRow, currentColumn );

			var sectionToDisable = from ls in lastSectionsHashes
								   from cs in currentSectionsHashes
								   where ls != cs && sections.ContainsKey( ls ) && ls.Length > 0
								   select ls;
								   
			foreach( var std in sectionToDisable )
				sections[ std ].gameObject.SetActive( false );

			var sectionToEnable = from cs in currentSectionsHashes
								  where sections.ContainsKey( cs )
								  select cs;
			
			foreach( var std in sectionToEnable )
				sections[ std ].gameObject.SetActive( true );

			for( int i = 0; i < MAX_SECTIONS_COUNT; ++i )
				lastSectionsHashes[ i ] = currentSectionsHashes[ i ];

			lastSectionHash = currentSectionHash;

		}
		private string[] getCurrentSectionsHashes( int row, int column ) {
			var rowUp = row + 1;
			var rowDown = row - 1;
			var columnLeft = column - 1;
			var columnRight = column + 1;
			return new string[ MAX_SECTIONS_COUNT ] {
				getSectionHash( rowUp, columnLeft ),
				getSectionHash( rowUp, column ),
				getSectionHash( rowUp, columnRight ),
				getSectionHash( row, columnLeft ),
				getSectionHash( row, column ),
				getSectionHash( row, columnRight ),
				getSectionHash( rowDown, columnLeft ),
				getSectionHash( rowDown, column ),
				getSectionHash( rowDown, columnRight )
			};
		}
		private void export( Action<bool> onCompleted ) {
			API.Async.Run( delegate {
				try {
					using( TextWriter tw = new StreamWriter( currentMapPath ) ) {
						tw.Write( comments );
						//tw.WriteLine( "# ZONES" );
						foreach( var mapObjects in objects.Values ) {
							//tw.WriteLine( "# MAP OBJECTS" );
							foreach( var obj in mapObjects ) {
								if( obj.IsRemoved )
									continue;
								var pos = obj.Position;
								var rot = obj.Rotation.eulerAngles;
								tw.WriteLine( $"ADD_OBJECT {obj.Mod} {obj.File} {pos.x} {pos.y} {pos.z} {rot.x} {rot.y} {rot.z} {obj.OnHour} {obj.OffHour}" );	
							}
						}
					}
					return true;
				} catch( Exception e ) { API.Logger.Print( e.Message ); }
				return false;
			}, state => { onCompleted.Invoke( state ); } );
		}
		
	}	
	
}