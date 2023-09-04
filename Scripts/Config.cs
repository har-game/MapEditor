using System.Text.RegularExpressions;

namespace HAR {
	
	public sealed class Config {
		
		public static bool IsValidModName( string name ) => Regex.IsMatch( name, @"^[a-z][a-z0-9_]{0,31}$", RegexOptions.Compiled );
		public static bool IsValidMapName( string name ) => Regex.IsMatch( name, @"^[123456789]\d{0,5}$", RegexOptions.Compiled );
		
		public static string ModsDirectory;

	}
	
}