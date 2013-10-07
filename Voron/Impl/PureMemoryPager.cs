﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Voron.Trees;

namespace Voron.Impl
{
	public unsafe class PureMemoryPager : AbstractPager
	{
		private IntPtr _ptr;
		private long _allocatedSize;
		private byte* _base;

        public PureMemoryPager(byte[] data)
	    {
            _ptr = Marshal.AllocHGlobal(data.Length);
            _base = (byte*)_ptr.ToPointer();
            NumberOfAllocatedPages = data.Length / PageSize;
            PagerState.Release();
            PagerState = new PagerState
            {
                Ptr = _ptr
            };
            PagerState.AddRef();
            fixed (byte* origin = data)
            {
                NativeMethods.memcpy(_base, origin, data.Length);
            }
	    }

		public PureMemoryPager()
		{
			_ptr = Marshal.AllocHGlobal(MinIncreaseSize);
			_base = (byte*)_ptr.ToPointer();
			NumberOfAllocatedPages = _allocatedSize / PageSize;
            PagerState.Release();
			PagerState = new PagerState
				{
					Ptr = _ptr
				};
			PagerState.AddRef();
		}


	    public override int Write(Page page)
	    {
			var toWrite = page.IsOverflow ? page.OverflowSize : PageSize;

			NativeMethods.memcpy(AcquirePagePointer(page.PageNumber), page.Base, toWrite);

		    return toWrite;
	    }

		public override unsafe int Write(Page page, long writeToPage)
		{
			throw new NotImplementedException();
		}

		public override void Dispose()
		{
            base.Dispose();
			PagerState.Release();
			_base = null;
		}

		public override void Sync()
		{
			// nothing to do here
		}

		public override void Flush(long startPage, long count)
		{
			// nothing to do here
		}

		public override byte* AcquirePagePointer(long pageNumber)
		{
			return _base + (pageNumber * PageSize);
		}

		public override void AllocateMorePages(Transaction tx, long newLength)
		{
			if (newLength <= _allocatedSize)
				throw new ArgumentException("Cannot set the legnth to less than the current length");

			var oldSize = _allocatedSize;
			_allocatedSize = newLength;
            NumberOfAllocatedPages = _allocatedSize / PageSize;
			var newPtr = Marshal.AllocHGlobal(new IntPtr(_allocatedSize));
			var newBase = (byte*)newPtr.ToPointer();
			NativeMethods.memcpy(newBase, _base, new IntPtr(oldSize));
			_base = newBase;
			_ptr = newPtr;

			var oldPager = PagerState;

			var newPager = new PagerState { Ptr = newPtr };
			newPager.AddRef(); // one for the pager

			if (tx != null) // we only pass null during startup, and we don't need it there
			{
				newPager.AddRef(); // one for the current transaction
				tx.AddPagerState(PagerState);
			}

			PagerState = newPager;
			oldPager.Release();
		}
	}
}