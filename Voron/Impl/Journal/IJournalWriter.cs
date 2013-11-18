﻿using System;
using System.Threading.Tasks;

namespace Voron.Impl.Journal
{
	public unsafe interface IJournalWriter : IDisposable
	{
		Task WriteGatherAsync(long position, byte*[] pages);
		long NumberOfAllocatedPages { get;  }
	    bool DeleteOnClose { get; set; }
	    IVirtualPager CreatePager();
	    void Read(long pageNumber, byte* buffer, int count);
	}
}