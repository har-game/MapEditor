using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HAR.Core {

	public struct RestartInfo {
		public bool IsPolice;
		public Vector3 Position;
		public float Heading;
		public ushort Price;
		public RestartInfo( bool isPolice, float x, float y, float z, float heading, ushort price ) => ( IsPolice, Position, Heading, Price ) = ( isPolice, new Vector3( x, y, z ), heading, price );
	}	
	
}