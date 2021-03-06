using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
	class Program
	{
		static readonly Encoding AppEncoding = Encoding.UTF8;
		static readonly int NgramSize = 3;
		static readonly int SelectTop = 10;

		static async Task Main(string[] args)
		{
			Console.InputEncoding = AppEncoding;
			Console.OutputEncoding = AppEncoding;

			var path = args[0];
			var tSource = new CancellationTokenSource();
			var stopwatch = new Stopwatch();
			var analyzer = new NgramAnalyzer();

			RunCancelationHeanlerAsync(tSource);

			analyzer.IterationComplete += IterationCompletedHandler;
			analyzer.SearchCaneled += SearchCanceledHandler;

			stopwatch.Start();
			var mostFresuqnt = await analyzer
				.SearchAsync(
					path,
					SelectTop,
					NgramSize,
					blockSize: 1_048_576, // 1 Mb.
					AppEncoding,
					tSource.Token);
			stopwatch.Stop();

			WritePairs(mostFresuqnt);
			Console.WriteLine($"Elapsed milliseconds: {stopwatch.ElapsedMilliseconds}");
		}

		static async void RunCancelationHeanlerAsync(CancellationTokenSource tSource)
		{
			await Task.Run(
				() => {
					Console.ReadKey(true);
					tSource.Cancel(false);
				},
				CancellationToken.None);
		}

		static void IterationCompletedHandler(NgramAnalyzer source, NgramAnalyzerEventArgs args)
		{
			Console.SetCursorPosition(0, 0);
			Console.WriteLine($"{args.CompletedBlocks}/{args.BlockCount}" + new string(' ', 10));
		}

		static void SearchCanceledHandler(NgramAnalyzer source, NgramAnalyzerEventArgs args)
		{
			Console.SetCursorPosition(0, 0);
			Console.WriteLine($"Search canceled on {args.CanceledOnBlock} of {args.BlockCount}");
		}

		static void WritePairs(KeyValuePair<string, int>[] pairs)
		{
			foreach (var pair in pairs)
				Console.WriteLine($"{pair.Key} - {pair.Value}");

			//Console.WriteLine();
		}
	}
}
