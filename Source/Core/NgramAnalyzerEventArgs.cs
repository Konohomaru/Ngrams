using System;

namespace Core
{
	public class NgramAnalyzerEventArgs : EventArgs
	{
		public int CompletedBlocks { get; private set; }

		public int BlockCount { get; private set; }

		public bool IsCanceled { get; private set; }

		public int CanceledOnBlock { get; private set; }

		public NgramAnalyzerEventArgs(int completedBlock, int blockCount)
		{
			CompletedBlocks = completedBlock;
			BlockCount = blockCount;
			IsCanceled = false;
			CanceledOnBlock = -1;
		}

		public NgramAnalyzerEventArgs(bool isCanceled, int canceledOnBlock, int blockCount)
		{
			CompletedBlocks = canceledOnBlock - 1;
			BlockCount = blockCount;
			IsCanceled = isCanceled;
			CanceledOnBlock = canceledOnBlock;
		}
	}
}
