// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Garnet.server
{
    /// <summary>
    ///  Represents a GarnetStreamID, which is a 128-bit identifier for an entry in a stream.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct GarnetStreamID
    {
        [FieldOffset(0)]
        public ulong ms;
        [FieldOffset(8)]
        public ulong seq;
        [FieldOffset(0)]
        public fixed byte idBytes[16];

        public GarnetStreamID(ulong ms, ulong seq)
        {
            BinaryPrimitives.WriteUInt64BigEndian(new Span<byte>(Unsafe.AsPointer(ref this.ms), 8), ms);
            BinaryPrimitives.WriteUInt64BigEndian(new Span<byte>(Unsafe.AsPointer(ref this.seq), 8), seq);
        }
    }
}