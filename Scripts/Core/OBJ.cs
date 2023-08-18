using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using API;

namespace HAR.Core {
	
    public static class OBJ { 

		public static void Create( string mod, string objName, Action<GameObject> onCompleted ) => loadModelOrGetFromCache( $"{mod}.obj", $"{objName}.mtl", objName, onCompleted );

		#region TEXTURES
		private static readonly Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
		private static void loadTextureOrGetFromCache( string mod, string name, Action<Texture2D> onCompleted, bool isNormal = false ) {
			var path = Path.Combine( Config.ModsDirectory, mod, name );
			if( loadedTextures.ContainsKey( path ) ) {
				if( loadedTextures[ path ] == null ) {
					Async.Wait( delegate { return loadedTextures[ path ] == null; }, delegate {
						onCompleted.Invoke( loadedTextures[ path ] );
					} );
				} else { onCompleted.Invoke( loadedTextures[ path ] ); }
				return;
			}
			loadedTextures.Add( path, null );
			Async.Run( delegate { return readTextureFile( path ); }, bytes => {
				Texture2D result = null;
				if( bytes.Length > 0 ) {
					try {
						var tmpTexture2D = createDefaultTexture();
						if( tmpTexture2D.LoadImage( bytes ) ) {
							if( isNormal )
								makeNormalMap( ref tmpTexture2D );
							tmpTexture2D.Apply();
							tmpTexture2D.name = name;
						}
						result = tmpTexture2D;
					} catch( Exception e ) { API.Logger.Print( e.Message ); }	
				} else { result = createDefaultTexture(); }
				loadedTextures[ path ] = result;
				onCompleted.Invoke( loadedTextures[ path ] );
			} );
		}
		private static byte[] readTextureFile( string path ) {
			var result = new byte[ 0 ];
			try {
				var bytes = File.ReadAllBytes( path );
				result = bytes;
			} catch( Exception e ) { API.Logger.Print( e.Message ); }
			return result;
		}
		private static void makeNormalMap( ref Texture2D texture2D ) {
            Color[] pixels = texture2D.GetPixels();
            for( int i = 0; i < pixels.Length; ++i ) {
                Color temp = pixels[ i ];
                temp.r = pixels[ i ].g;
                temp.a = pixels[ i ].r;
                pixels[ i ] = temp;
            }
            texture2D.SetPixels( pixels );
        }
		private static Texture2D createDefaultTexture() => new Texture2D( 1, 1 );
		#endregion

		#region MATERIALS
		private static readonly Dictionary<string, Dictionary<string, Material>> loadedMaterials = new Dictionary<string, Dictionary<string, Material>>();
		private static void loadMaterialsOrGetFromCache( string mod, string name, Action<Dictionary<string, Material>> onCompleted ) {
			var path = Path.Combine( Config.ModsDirectory, mod, name );
			if( loadedMaterials.ContainsKey( path ) ) {
				if( loadedMaterials[ path ] == null ) {
					Async.Wait( delegate { return loadedMaterials[ path ] == null; }, delegate {
						onCompleted.Invoke( loadedMaterials[ path ] );
					} );
				} else { onCompleted.Invoke( loadedMaterials[ path ] ); }
				return;
			}
			loadedMaterials.Add( path, null );
			Async.Run( delegate { return readMtlFile( path ); }, data => {
				var isTextureLoaded = makeMaterials( mod, data, out var dict );
				Async.Wait( isTextureLoaded, delegate {
					loadedMaterials[ path ] = dict;
					onCompleted.Invoke( loadedMaterials[ path ] );
				} );
			} );
		}
		private static Dictionary<string, MaterialData> readMtlFile( string mtlPath ) {
			var result = new Dictionary<string, MaterialData>();
			try {
				var tmpResult = new Dictionary<string, MaterialData>();
				var lines = File.ReadAllLines( mtlPath );
				string lastKey = null;
				foreach( var ln in lines ) {
					var l = Regex.Replace( ln, "[ \t]{1,}", " ", RegexOptions.Compiled ).Trim();
					if( l.Length == 0 || l[ 0 ] == '#' )
						continue;
					var split = l.Split( ' ' );
					var keyword = split[ 0 ].ToLower();
					if( keyword == "newmtl" ) {
						if( lastKey != null )
							tmpResult[ lastKey ].RefreshData();
						lastKey = split[ 1 ];
						tmpResult.Add( lastKey, new MaterialData() );
						continue;
					}
					if( lastKey == null )
						continue;
					switch( keyword ) {
						case "d":
						tmpResult[ lastKey ].UseD = true;
						tmpResult[ lastKey ].SetD( float.Parse( split[ 1 ] ) );
						continue;
						case "map_d":
						tmpResult[ lastKey ].UseMapD = true;
						tmpResult[ lastKey ].MapD = split[ 1 ];
						continue;
						case "kd":
						tmpResult[ lastKey ].UseKd = true;
						tmpResult[ lastKey ].Kd = parseColor( split );
						continue;
						case "map_kd":
						tmpResult[ lastKey ].UseMapKd = true;
						tmpResult[ lastKey ].MapKd = split[ 1 ];
						continue;
						case "map_bump":
						tmpResult[ lastKey ].UseMapBump = true;
						tmpResult[ lastKey ].MapBump = split[ 1 ];
						continue;
						case "ks":
						tmpResult[ lastKey ].UseKs = true;
						tmpResult[ lastKey ].Ks = parseColor( split );
						continue;
						case "ka":
						tmpResult[ lastKey ].UseKa = true;
						tmpResult[ lastKey ].Ka = parseColor( split );
						continue;
						//case "ns":
						//tmpResult[ lastKey ].UseNs = true;
						//tmpResult[ lastKey ].Ns = float.Parse( split[ 1 ] );
						//continue;
					}						
				}
				result = tmpResult;
			} catch( Exception e ) { API.Logger.Print( e.Message ); }
			return result;
		}
		private static Func<bool> makeMaterials( string mod, Dictionary<string, MaterialData> materialsData, out Dictionary<string, Material> result ) {
			result = new Dictionary<string, Material>();
			var numSteps = 0;
			var stepsCounter = 0;
			try {
				var tmpResult = new Dictionary<string, Material>();
				foreach( var md in materialsData ) {
					var matName = md.Key;
					var data = md.Value;
					var currentMaterial = createDefaultMaterial( matName );
					if( data.UseKd ) {
						currentMaterial.SetColor( "_Color", data.Kd );
					} else {
						currentMaterial.SetColor( "_Color", data.D );
					}
					currentMaterial.SetFloat( "_Glossiness", 0f ); // no smoothness

					if( data.UseMapKd ) {
						numSteps += 1;
						loadTextureOrGetFromCache( mod, data.MapKd, texture => {
							currentMaterial.SetTexture( "_MainTex", texture );
							stepsCounter += 1;
						} );
					}
					if( data.UseMapD ) {
						numSteps += 1;
						loadTextureOrGetFromCache( mod, data.MapD, texture => {
							currentMaterial.SetTexture( "_MainTex", texture );
							stepsCounter += 1;
						} );
					}
					if( data.UseMapBump ) {
						numSteps += 1;
                        currentMaterial.SetFloat( "_BumpScale", 0.3f );
                        currentMaterial.EnableKeyword( "_NORMALMAP" );
						loadTextureOrGetFromCache( mod, data.MapBump, texture => {
							currentMaterial.SetTexture( "_BumpMap", texture );
							stepsCounter += 1;
						}, true );
					}
					if( data.UseKs )
						currentMaterial.SetColor( "_SpecColor", data.Ks );
					if( data.UseKa ) {
						currentMaterial.SetColor( "_EmissionColor", data.Ka );
						currentMaterial.EnableKeyword( "_EMISSION" );
					}
					//if( data.UseNs )
					//	currentMaterial.SetFloat( "_Glossiness", data.Ns );
					tmpResult.Add( matName, currentMaterial );
				}
				result = tmpResult;
			} catch( Exception e ) { API.Logger.Print( e.Message ); }
			return delegate { return stepsCounter != numSteps; };
		}
		private static Material createDefaultMaterial( string name ) {
			var material = new Material( Shader.Find( "Standard (Specular setup)" ) );
			material.name = name;
			makeTransparent( ref material );
			return material;
		}
		private static Color parseColor( string[] split ) {
			try {
				return new Color( float.Parse( split[ 1 ] ), float.Parse( split[ 2 ] ), float.Parse( split[ 3 ] ), 1f );
			} catch( Exception e ) { API.Logger.Print( e.Message ); }
			return Color.white;
		}
		private static void makeTransparent( ref Material material ) {
			material.SetFloat( "_Mode", 3f );
			material.SetOverrideTag( "RenderType", "Transparent" );
			material.SetInt( "_SrcBlend", ( int ) UnityEngine.Rendering.BlendMode.One );
			material.SetInt( "_DstBlend", ( int ) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha );
			material.SetInt( "_ZWrite", 0 );
			material.DisableKeyword( "_ALPHATEST_ON" );
			material.DisableKeyword( "_ALPHABLEND_ON" );
			material.EnableKeyword( "_ALPHAPREMULTIPLY_ON" );
			material.renderQueue = ( int ) UnityEngine.Rendering.RenderQueue.Transparent;
		}
		#endregion
		
		#region OBJECTS
		private static readonly Dictionary<string, GameObject> loadedGameObjects = new Dictionary<string, GameObject>();
		private static void loadModelOrGetFromCache( string mod, string objName, string mtlName, Action<GameObject> onCompleted ) {
			var path = Path.Combine( Config.ModsDirectory, mod, objName );
			if( loadedGameObjects.ContainsKey( path ) ) {
				if( loadedGameObjects[ path ] == null ) {
					Async.Wait( delegate { return loadedGameObjects[ path ] == null; }, delegate {
						onCompleted.Invoke( loadedGameObjects[ path ] );
					} );
				} else { onCompleted.Invoke( loadedGameObjects[ path ] ); }
				return;
			}
			loadedGameObjects.Add( path, null );
			loadMaterialsOrGetFromCache( mod, mtlName, materials => {
				Async.Run( delegate { return readObjFile( path ); }, objects => {
					var go = createGameObject( objects, materials );
					go.name = objName;
					go.SetActive( false );
					loadedGameObjects[ path ] = go;
					onCompleted.Invoke( loadedGameObjects[ path ] );
				} );
			} );
		}
		private static Dictionary<string, ObjectData> readObjFile( string path ) {
			var result = new Dictionary<string, ObjectData>();
			try {
				var meshBuilder = new MeshBuilder();
				var lines = File.ReadAllLines( path );
				var currentMeshName = "default";
				var currentMaterialName = "default";
				foreach( var ln in lines ) {
					var l = Regex.Replace( ln, "[ \t]{1,}", " ", RegexOptions.Compiled ).Trim();
					if( l.Length == 0 || l[ 0 ] == '#' )
						continue;
					var split = l.Split( ' ' );
					var keyword = split[ 0 ].ToLower();
					switch( keyword ) {
						case "v":
						meshBuilder.Vertices.Add( new Vector3( 
							float.Parse( split[ 1 ] ),
							float.Parse( split[ 2 ] ), 
							float.Parse( split[ 3 ] )
						) );
						continue;
						case "vn":
						meshBuilder.Normals.Add( new Vector3( 
							float.Parse( split[ 1 ] ),
							float.Parse( split[ 2 ] ), 
							float.Parse( split[ 3 ] )
						) );
						continue;
						case "vt":
						meshBuilder.UVs.Add( new Vector2( 
							float.Parse( split[ 1 ] ),
							float.Parse( split[ 2 ] )
						) );
						continue;
						case "g":
						case "o":
						currentMeshName = split[ 1 ];
						if( !meshBuilder.FaceData.ContainsKey( currentMeshName ) )
							meshBuilder.FaceData.Add( currentMeshName, new Dictionary<string, List<Vector3Int>>() );
						continue;
						case "usemtl":
						currentMaterialName = split[ 1 ];
						if( !meshBuilder.FaceData[ currentMeshName ].ContainsKey( currentMaterialName ) )
							meshBuilder.FaceData[ currentMeshName ].Add( currentMaterialName, new List<Vector3Int>() );
						continue;
						case "f":
						var tempFaceData = new List<Vector3Int>();
						var currentFaceCounter = 1;
						for( var i = 1; i < split.Length; ++i ) {
							var indicesInfo = split[ i ].Split( '/' );
							var vertexIndex = int.Parse( indicesInfo[ 0 ] ) - 1;
							var uvIndex = int.Parse( indicesInfo[ 1 ] ) - 1;
							var normalIndex = int.Parse( indicesInfo[ 2 ] ) - 1;
							if( vertexIndex < 0 )
								vertexIndex = meshBuilder.Vertices.Count - Math.Abs( vertexIndex ) + 1;
							if( normalIndex < 0 )
								normalIndex = meshBuilder.Normals.Count - Math.Abs( normalIndex ) + 1;
							if( uvIndex < 0 )
								uvIndex = meshBuilder.UVs.Count - Math.Abs( uvIndex ) + 1;
							tempFaceData.Add( new Vector3Int( vertexIndex, uvIndex, normalIndex ) );
							++currentFaceCounter;
						}
						var numFaces = currentFaceCounter;
						currentFaceCounter = 1;
						while( currentFaceCounter + 2 < numFaces ) {
							meshBuilder.FaceData[ currentMeshName ][ currentMaterialName ].Add( tempFaceData[ 0 ] );
							meshBuilder.FaceData[ currentMeshName ][ currentMaterialName ].Add( tempFaceData[ currentFaceCounter ] );
							meshBuilder.FaceData[ currentMeshName ][ currentMaterialName ].Add( tempFaceData[ currentFaceCounter + 1 ] );
							++currentFaceCounter;
						}
						continue;
					}
				}
				result = meshBuilder.GenerateObjectsData();
			} catch( Exception e ) { API.Logger.Print( e.Message ); }
			return result;
		}
		private static GameObject createGameObject( Dictionary<string, ObjectData> objectsData, Dictionary<string, Material> materials ) {
			var result = new GameObject();
			if( objectsData.Count == 0 ) {
				// make tag not loaded
			} else if( objectsData.Count == 1 ) {
				var od = objectsData.First();
				result.AddComponent<MeshFilter>().sharedMesh = od.Value.CreateMesh( od.Key );
				result.AddComponent<MeshRenderer>().sharedMaterials = od.Value.CreateMaterials( materials );
				result.name = od.Key;
			} else {
				foreach( var od in objectsData ) {
					var subObject = new GameObject( od.Key );
					subObject.AddComponent<MeshFilter>().sharedMesh = od.Value.CreateMesh( od.Key );
					subObject.AddComponent<MeshRenderer>().sharedMaterials = od.Value.CreateMaterials( materials );
					subObject.transform.SetParent( result.transform );
				}
				//combineMeshes( result );
			}
			return result;
		}
		//private static void combineMeshes( GameObject root ) {
		//	var meshFilters = root.GetComponentsInChildren<MeshFilter>( true );
		//	var combines = new CombineInstance[ meshFilters.Length ];
		//	var i = 0;
		//	while( i < meshFilters.Length ) {
		//		combines[ i ].mesh = meshFilters[ i ].sharedMesh;
		//		combines[ i ].transform = meshFilters[ i ].transform.localToWorldMatrix;
		//		meshFilters[ i ].gameObject.SetActive( false );
		//		i++;
		//	}
		//	root.AddComponent<MeshRenderer>();
		//	var meshFilter = root.AddComponent<MeshFilter>();
		//	meshFilter.sharedMesh = new Mesh();
		//	meshFilter.sharedMesh.CombineMeshes( combines );
		//
		#endregion

		#region DATA
		private sealed class MaterialData {
			
			private float d;
			
			public bool UseD;
			public Color D = Color.white;
			public bool UseMapD;
			public string MapD = string.Empty;
			public bool UseKd;
			public Color Kd = Color.white;
			public bool UseMapKd;
			public string MapKd = string.Empty;
			public bool UseMapBump;
			public string MapBump = string.Empty;
			public bool UseKs;
			public Color Ks = Color.white;
			public bool UseKa;
			public Color Ka = Color.white;
			//public bool UseNs = true;
			//public float Ns;

			public void SetD( float value ) => d = value;
			public void RefreshData() {
				D.a = d;
				Kd.a = d;
				Ka *= 0.05f;
				//Ns /= 1000f;
			}
		}
		private sealed class ObjectData {
			public Vector3[] Vertices;
			public Vector3[] Normals;
			public Vector2[] UVs;
			public Dictionary<string, int[]> Triangles;
			public string[] Materials;
			public int SubMeshCount;
			public ObjectData( int subMeshCount ) => (SubMeshCount, Materials) = (subMeshCount, new string[ subMeshCount ] );
			public Mesh CreateMesh( string name ) {
				var mesh = new Mesh();
				mesh.name = name;
				mesh.subMeshCount = SubMeshCount;
				mesh.vertices = Vertices;
				mesh.normals = Normals;
				mesh.uv = UVs;
				var i = 0;
				foreach( var mat in Materials ) {
					mesh.SetTriangles( Triangles[ mat ], i, true );
					++i;
				}
				mesh.RecalculateBounds();
				mesh.RecalculateTangents();
				return mesh;
			}
			public Material[] CreateMaterials( Dictionary<string, Material> materials ) {
				var targetMaterials = new Material[ SubMeshCount ];
				for( var i = 0; i < SubMeshCount; ++i ) {
					var materialName = Materials[ i ];
					targetMaterials[ i ] = materials.ContainsKey( materialName ) ? materials[ materialName ] : createDefaultMaterial( materialName );
				}
				return targetMaterials;
			}
		}
		private sealed class MeshBuilder {
			public List<Vector3> Vertices = new List<Vector3>();
			public List<Vector3> Normals = new List<Vector3>();
			public List<Vector2> UVs = new List<Vector2>();
			// [mesh][material][indices]
			public Dictionary<string, Dictionary<string, List<Vector3Int>>> FaceData = new Dictionary<string, Dictionary<string, List<Vector3Int>>>();
			public Dictionary<string, ObjectData> GenerateObjectsData() {
				var result = new Dictionary<string, ObjectData>();
				foreach( var obj in FaceData ) {
					var od = new ObjectData( obj.Value.Count );
					var tCount = obj.Value.Values.Sum( x => x.Count );
					if( tCount == 0 ) // obj.Key.ToLower().Contains( "shadow_plane" ) || 
						continue;
					var newVerts = new Vector3[ tCount ];
					var newUVs = new Vector2[ tCount ];
					var newNormals = new Vector3[ tCount ];
					var tris = new Dictionary<string, int[]>();
					var remapIndices = new Dictionary<string, int>();
					var i = 0;
					foreach( var mat in obj.Value ) {
						var j = 0;
						if( !tris.ContainsKey( mat.Key ) )
							tris.Add( mat.Key, new int[ mat.Value.Count ] );
						foreach( var face in mat.Value ) {
							string key = face.x + "|" + face.y + "|" + face.z; // ?
							if( !remapIndices.ContainsKey( key ) ) {
								newVerts[ i ] = Vertices[ face.x ];
								if( face.y >= 0 )
									newUVs[i] = UVs[ face.y ];
								if( face.z >= 0 )
									newNormals[ i ] = Normals[ face.z ];
								remapIndices.Add( key, i );
								i++;
							}
							tris[ mat.Key ][ j ] = remapIndices[ key ];
							++j;
						}
					}
					Array.Resize<Vector3>( ref newVerts, i );
					Array.Resize<Vector3>( ref newNormals, i );
					Array.Resize<Vector2>( ref newUVs, i );
					i = 0;
					foreach( var mat in obj.Value ) {
						od.Materials[ i ] = mat.Key;
						++i;
					}
					od.Vertices = newVerts;
					od.Normals = newNormals;
					od.UVs = newUVs;
					od.Triangles = tris;
					result.Add( obj.Key, od );	
				}
				return result;
			}
		}
		#endregion
		
	}
	
}