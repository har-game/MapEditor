using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HAR.Core {

	public struct LocationTeleport {
		public Vector3 Position;
		public float Heading;
		public ushort RowInLanguageFile;
		public LocationTeleport( float x, float y, float z, float heading, ushort rowInLanguageFile ) => ( Position, Heading, RowInLanguageFile ) = ( new Vector3( x, y, z ), heading, rowInLanguageFile );
	}	
	
}