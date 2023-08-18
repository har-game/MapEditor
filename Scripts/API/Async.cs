using System;
using System.Threading.Tasks;

namespace API {
		
	internal static class Async {

		internal static void Wait( Func<bool> condition, Action onCompleted ) { Task.Run( delegate { while( condition.Invoke() ) continue; } ).GetAwaiter().OnCompleted( onCompleted ); }
		internal static void Run( Action action, Action onCompleted ) => Task.Run( action ).GetAwaiter().OnCompleted( onCompleted );
		internal static void Run<T>( Func<T> action, Action<T> onCompleted ) {
			var task = Task.Run( action );
			task.GetAwaiter().OnCompleted( delegate { onCompleted.Invoke( task.Result ); } );
		}

	}

} 