// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Garnet.common;

namespace Garnet.server
{
    public sealed class GarnetStreamManager : IDisposable
    {
        private Dictionary<byte[], GarnetStreamObject> streams;
        long defPageSize;
        long defMemorySize;

        SingleWriterMultiReaderLock _lock = new SingleWriterMultiReaderLock();

        public GarnetStreamManager(long pageSize, long memorySize)
        {
            streams = new Dictionary<byte[], GarnetStreamObject>(new ByteArrayComparer());
            defPageSize = pageSize;
            defMemorySize = memorySize;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (streams != null)
            {
                _lock.WriteLock();
                try
                {
                    foreach (var stream in streams.Values)
                    {
                        stream.Dispose();
                    }

                    streams.Clear();
                }
                finally
                {
                    _lock.WriteUnlock();
                }
            }

        }
    }
}