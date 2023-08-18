using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace API {
	
	public static class Extensions {

		public static float ToFloat( this string input ) {
			if( input.Contains( "e" ) || input.Contains( "E" ) )
				return float.Parse( input, CultureInfo.InvariantCulture );
			var result = 0f;
			var pos = 0;
			var len = input.Length;
			if( len == 0 ) return float.NaN;
			var c = input[ 0 ];
			var sign = 1f;
			if( c == '-' ) {
				sign = -1f;
				++pos;
				if( pos >= len )
					return float.NaN;
			}
			while( true ) {
				if( pos >= len )
					return sign * result;
				c = input[ pos++ ];
				if( c < '0' || c > '9' )
					break;
				result = ( result * 10.0f ) + ( c - '0' );
			}
			if( c != '.' && c != ',' )
				return float.NaN;
			var exp = 0.1f;
			while( pos < len ) {
				c = input[ pos++ ];
				if( c < '0' || c > '9' )
					return float.NaN;
				result += ( c - '0' ) * exp;
				exp *= 0.1f;
			}
			return sign * result;
		}
		public static int ToInt( this string input ) {
			var result = 0;
			var isNegative = input[ 0 ] == '-';
			for( int i = ( isNegative ) ? 1 : 0; i < input.Length; i++ )
				result = result * 10 + ( input[ i ] - '0' );
			return isNegative ? -result : result;
		}
		public static int SetBitTo1( this int value, int position ) => value |= ( 1 << position );
		public static int SetBitTo0( this int value, int position ) => value & ~( 1 << position );
		public static bool IsBitSetTo1( this int value, int position ) => ( value & ( 1 << position ) ) != 0;
		public static bool IsBitSetTo0( this int value, int position ) => !IsBitSetTo1( value, position );
		
	}

}