using System.Text.RegularExpressions;

namespace HAR {
	
	public sealed class Config {
		
		public static bool IsValidModName( string name ) => Regex.IsMatch( name, @"^[a-z0-9_]{1,32}$", RegexOptions.Compiled );
		public static bool IsValidMapName( string name ) => Regex.IsMatch( name, @"^[123456789]\d{0,}$", RegexOptions.Compiled );
		
		public static string ModsDirectory;
		public static float SectionSize = 100f;

	}
	
}