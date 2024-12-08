// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Buffers.Binary;
using Tsavorite.core;

namespace Garnet.server
{
    public enum XTRIMOpts
    {
        MAXLEN,
        MINID,
        NONE
    }

    public class GarnetStreamObject : IDisposable
    {
        readonly IDevice device;
        readonly TsavoriteLog log;


        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                log.Dispose();
                device.Dispose();
            }
            finally
            {

            }
        }
    }
}