using System;

namespace Core
{
	public class LoadDrawer
	{
		public LoadDrawer Start()
		{
			Console.Clear();
			Console.CursorVisible = false;
			return this;
		}

		public void Stop()
		{
			Console.CursorVisible = true;
		}

		public void IterationCompletedHandler(NgramAnalyzer source, NgramAnalyzerEventArgs args)
		{
			Console.SetCursorPosition(0, 0);
			Console.WriteLine($"{args.CompletedBlocks}/{args.BlockCount}" + new string(' ', 10));
		}

		public void SearchCanceledHandler(NgramAnalyzer source, NgramAnalyzerEventArgs args)
		{
			Console.SetCursorPosition(0, 0);
			Console.WriteLine($"Search canceled on {args.CanceledOnBlock} of {args.BlockCount}");
		}
	}
}
