using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HAR.Core {

	public struct MapObject {
		#if MAP_EDITOR_TOOL_ONLY
		public bool DontUse;
		#endif
		public string Mod, File;
		public Vector3 Position;
		public Quaternion Rotation;
		public MapObject( string mod, string file, float x, float y, float z, float rotationX, float rotationY, float rotationZ ) => ( DontUse, Mod, File, Position, Rotation) = ( false, mod, file, new Vector3( x, y, z ), Quaternion.Euler( rotationX, rotationY, rotationZ ) );
	}	
	
}