using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
	public sealed class NgramAnalyzer
	{
		public event Action<NgramAnalyzer, NgramAnalyzerEventArgs> IterationComplete;
		public event Action<NgramAnalyzer, NgramAnalyzerEventArgs> SearchCaneled;

		public async Task<KeyValuePair<string, int>[]> SearchAsync(
			string path,
			int topCount,
			int ngramSize,
			int blockSize,
			Encoding fileEncoding,
			CancellationToken token)
		{
			return await Task.Run(
				() => {
					using var stream = new FileStream(path, FileMode.Open);
					var buffer = new byte[blockSize]; 

					var counter = new NgramAnalyzer();
					var ngrams = new ConcurrentDictionary<string, int>();
					var mostFrequent = new KeyValuePair<string, int>[topCount];
					var currentBlock = 0;
					var blockCount = (int)(stream.Length / blockSize) + 1;

					while (
						stream.Read(buffer, 0, buffer.Length) is int readBytes &&
						readBytes > 0 &&
						!token.IsCancellationRequested) {

						var text = fileEncoding.GetString(buffer, 0, readBytes);

						if (stream.Position + ngramSize - 1 < stream.Length)
							stream.Seek(-ngramSize + 1, SeekOrigin.Current);

						counter.CountNgramsParallel(text, ngramSize, ngrams, token);
						counter.FindMostFrequent(ngrams, mostFrequent, token);

						if (!token.IsCancellationRequested)
							OnIterationComplete(++currentBlock, blockCount);
					}

					if (token.IsCancellationRequested)
						OnSearchCanceled(currentBlock, blockCount);

					return mostFrequent;
				},
				CancellationToken.None);
		}

		private void OnIterationComplete(int completedBlock, int blockCount)
		{
			IterationComplete?.Invoke(this, new NgramAnalyzerEventArgs(
				completedBlock,
				blockCount));
		}

		private void OnSearchCanceled(int canceledOnBlock, int blockCount)
		{
			SearchCaneled?.Invoke(this, new NgramAnalyzerEventArgs(
				true,
				canceledOnBlock,
				blockCount));
		}

		private void CountNgramsParallel(
			string text,
			int ngramSize,
			ConcurrentDictionary<string, int> ngrams,
			CancellationToken token)
		{
			Parallel.ForEach(Partitioner.Create(0, text.Length), (block) => {
				var offset = block.Item1;
				var bound = block.Item2;

				for (int blockI = offset; blockI < bound && !token.IsCancellationRequested; ++blockI) {
					if (!char.IsLetter(text[blockI])) continue;

					var ngram = new StringBuilder();
					var isNgram = false;

					for (
						int ngramI = 0;
						ngramI < ngramSize &&
						blockI + ngramI + (ngramSize - ngramI - 1) < text.Length &&
						!token.IsCancellationRequested;
						++ngramI) {
						if (char.IsLetter(text[blockI + ngramI])) {
							ngram.Append(char.ToLower(text[blockI + ngramI]));
							isNgram = true;
						} else {
							isNgram = false;
							blockI += ngramI;
							break;
						}
					}

					if (isNgram && !token.IsCancellationRequested)
						ngrams.AddOrUpdate(ngram.ToString(), 1, (keu, value) => value + 1);
				}
			});
		}

		private void FindMostFrequent(
			IDictionary<string, int> ngrams,
			KeyValuePair<string, int>[] mostFrequent,
			CancellationToken token)
		{
			foreach (var ngram in ngrams) {
				if (token.IsCancellationRequested) break;

				lock (mostFrequent.SyncRoot) {
					var leastFrequent = 0;
					var alreadyInserted = false;

					for (int i = 0; i < mostFrequent.Length && !token.IsCancellationRequested; ++i) {
						if (mostFrequent[i].Key is null) {
							mostFrequent[i] = ngram;
							alreadyInserted = true;
							break;
						} else if (mostFrequent[i].Key == ngram.Key) {
							mostFrequent[i] = ngram;
							alreadyInserted = true;
							break;
						} else if (mostFrequent[i].Value < mostFrequent[leastFrequent].Value) {
							leastFrequent = i;
						}
					}

					if (!alreadyInserted && mostFrequent[leastFrequent].Value < ngram.Value) {
						mostFrequent[leastFrequent] = ngram;
					}
				}
			}
		}
	}
}
