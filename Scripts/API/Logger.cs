using System.IO;

namespace API {
	
	internal static class Logger {
		
		static Logger() {
			try {
				if( File.Exists( "log.txt" ) )
					File.Delete( "log.txt" );
			} catch { }
		}
		
		internal static void Print( string message ) { 
			lock( typeof( File ) ) {
				try {
					File.AppendAllText( "log.txt", $"{message}\r\n" );
				} catch { }
			};
		}
		
	}

}