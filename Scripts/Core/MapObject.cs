using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HAR.Core {

	public struct MapObject {
		public string Mod, File;
		public Vector3 Position;
		public Quaternion Rotation;
		public byte OnHour, OffHour;
		public bool IsRemoved;
	}	
	
}