using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HAR.Core {

	public struct ZoneInfo {
		public byte Id;
		public Vector3 Position, Radius;
		public ushort RowInLanguageFile;
		public ZoneInfo( byte id, float x, float y, float z, float raiusX, float raiusY, float radiusZ, ushort rowInLanguageFile ) => ( Id, Position, Radius, RowInLanguageFile ) = ( id, new Vector3( x, y, z ), new Vector3( raiusX, raiusY, radiusZ ), rowInLanguageFile );
	}	
	
}