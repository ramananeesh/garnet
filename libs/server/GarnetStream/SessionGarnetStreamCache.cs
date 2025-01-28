// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using Garnet.common;
using Tsavorite.core;

namespace Garnet.server
{
    internal struct SessionGarnetStreamCache
    {
        const int DefaultCacheSize = 16;
        readonly Dictionary<byte[], GarnetStreamObject> streamCache = new Dictionary<byte[], GarnetStreamObject>(DefaultCacheSize, new ByteArrayComparer());
        readonly byte[][] streamKeysCache = new byte[DefaultCacheSize][];
        int cachedStreamsCount = 0;
        int front = 0;

        public SessionGarnetStreamCache()
        { }

        /// <summary>
        /// Lookup a stream in the cahce. Since the cache is expected to be small, we can sequentially scan.
        /// </summary>
        /// <param name="key">name of stream to lookup</param>
        /// <param name="stream">stream found from the cache</param>
        /// <returns>true if stream exists in cache</returns>
        public bool TryGetStreamFromCache(ReadOnlySpan<byte> key, out GarnetStreamObject stream)
        {
            return streamCache.TryGetValue(key.ToArray(), out stream);
        }

        /// <summary>
        /// Add a stream to the cache. If the cache is full, we don't add the stream.
        /// </summary>
        /// <param name="key">name of stream</param>
        /// <param name="stream">reference to stream object</param>
        /// <returns>true if successfully added</returns>
        public bool TryAddStreamToCache(byte[] key, GarnetStreamObject stream)
        {
            if (cachedStreamsCount < DefaultCacheSize)
            {
                streamCache.Add(key, stream);
                // add to circular array and update front
                streamKeysCache[front] = key;
                front = (front + 1) % DefaultCacheSize;
                cachedStreamsCount++;
                return true;
            }

            streamCache.Remove(streamKeysCache[front]);
            streamCache.Add(key, stream);
            // add to circular array where we removed the oldest stream
            streamKeysCache[front] = key;
            front = (front + 1) % DefaultCacheSize;
            // we don't need to update cachedStreamsCount since we added and removed a stream
            return true;

        }
    }
}