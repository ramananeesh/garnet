// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Garnet.server.BTreeIndex
{

    public enum BTreeNodeType
    {
        Internal,
        Leaf
    }

    /// <summary>
    /// Represents information stored in a node in the B+tree
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct NodeData
    {
        [FieldOffset(0)]
        public Value* values;
        [FieldOffset(0)]
        public BTreeNode** children;
    }

    [StructLayout(LayoutKind.Explicit, Size = sizeof(byte) + sizeof(ulong))]
    public struct Value
    {
        [FieldOffset(0)]
        public byte valid;
        [FieldOffset(1)]
        public ulong address;

        public bool Valid
        {
            get
            {
                return valid == 1;
            }
            set
            {
                valid = (byte)(value ? 1 : 0);
            }
        }
    }

    public unsafe struct NodeInfo
    {
        public BTreeNodeType type;
        public int count;
        public BTreeNode* next;
        public BTreeNode* previous;
        public uint validCount;
    }

    public unsafe struct BTreeNode
    {
        public static int PAGE_SIZE = 4096;
        public static int KEY_SIZE = 16; // key size in bytes.
        public static int LEAF_CAPACITY = (PAGE_SIZE - sizeof(NodeInfo)) / (KEY_SIZE + sizeof(Value));
        public static int INTERNAL_CAPACITY = (PAGE_SIZE - sizeof(NodeInfo) - sizeof(BTreeNode*)) / (KEY_SIZE + sizeof(BTreeNode*));

        public NodeInfo* info;
        public byte* keys;
        public NodeData data; // data in the node
        public IntPtr memoryBlock; // pointer to the memory block

        public void Allocate(BTreeNodeType type)
        {
            // TODO: Use a different memory allocation policy
            memoryBlock = Marshal.AllocHGlobal(PAGE_SIZE);
            info = (NodeInfo*)memoryBlock.ToPointer();
            info->count = 0;
            info->validCount = 0;
            info->type = type;
            info->next = null;
            info->previous = null;

            byte* baseAddress = (byte*)memoryBlock + sizeof(NodeInfo);
            keys = (byte*)baseAddress;
            int capacity = (type == BTreeNodeType.Leaf) ? LEAF_CAPACITY : INTERNAL_CAPACITY;
            byte* dataStart = baseAddress + capacity * KEY_SIZE;
            if (type == BTreeNodeType.Leaf)
            {
                data.values = (Value*)dataStart;
            }
            else
            {
                data.children = (BTreeNode**)dataStart;
            }
        }

        public byte* GetKey(int index)
        {
            byte* keyAddress = keys + (index * KEY_SIZE);
            return keyAddress;
        }

        public void SetKey(int index, byte* keyData)
        {
            byte* keyAddress = keys + (index * KEY_SIZE);
            Buffer.MemoryCopy(keyData, keyAddress, KEY_SIZE, KEY_SIZE);
        }

        public void SetChild(int index, BTreeNode* child)
        {
            data.children[index] = child;
        }

        public BTreeNode* GetChild(int index)
        {
            return data.children[index];
        }

        public void SetValue(int index, Value value)
        {
            data.values[index] = value;
        }

        public Value GetValue(int index)
        {
            return data.values[index];
        }

        public void SetValueValid(int index, bool valid)
        {
            data.values[index].Valid = valid;
        }

        public void InsertTombstone(int index)
        {
            data.values[index].Valid = false;
        }

        public int UpperBound(byte* key)
        {
            if (info->count == 0)
            {
                return 0;
            }
            int left = 0, right = info->count - 1;
            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                byte* midKey = GetKey(mid);
                int cmp = Compare(key, midKey);
                if (cmp < 0)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }
            return left;
        }

        public int LowerBound(byte* key)
        {
            if (info->count == 0)
            {
                return 0;
            }
            int left = 0, right = info->count - 1;
            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                byte* midKey = GetKey(mid);
                int cmp = Compare(midKey, key);
                if (cmp == 0)
                {
                    return mid;
                }
                else if (cmp < 0)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
            return left;
        }

        public void UpgradeToInternal()
        {
            info->type = BTreeNodeType.Internal;
            data.children = (BTreeNode**)(keys + (INTERNAL_CAPACITY * KEY_SIZE)); // should be keys + Internal capacity?
        }

        /// <summary>
        /// Compares two keys
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns>-1 if key1 is less than key2; 0 if key1 == key2; 1 if key1 > key2</returns>
        public static int Compare(byte* key1, byte* key2)
        {

            if (Sse2.IsSupported)
            {
                var v1 = Sse2.LoadVector128(key1);
                var v2 = Sse2.LoadVector128(key2);

                var mask = Sse2.MoveMask(Sse2.CompareEqual(v1, v2));

                if (mask != 0xFFFF) // Not all bytes are equal
                {
                    // Find the index of the first differing byte
                    int index = BitOperations.TrailingZeroCount(~mask); // Invert mask to find first zero (differing byte)
                    return key1[index] < key2[index] ? -1 : 1;
                }

                return 0; // Arrays are equal
            }
            else
            {
                return new Span<byte>(key1, KEY_SIZE).SequenceCompareTo(new Span<byte>(key2, KEY_SIZE));
            }
        }

        public void Deallocate()
        {
            if (memoryBlock != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(memoryBlock);
                memoryBlock = IntPtr.Zero;

                // After freeing the memory, explicitly set pointers to null to avoid dangling pointers.
                info = null;
                keys = null;
                data.values = null; // Only necessary if data.values or data.children was separately allocated
                data.children = null;
            }
        }
    }

    /// <summary>
    /// Statistics about the B+Tree
    /// </summary>
    public struct BTreeStats
    {
        // general index stats
        public int depth;
        public ulong numLeafNodes;
        public ulong numInternalNodes;

        // workload specific stats
        public long totalInserts;           // cumulative number of inserts to the index
        public long totalDeletes;           // cumulative number of deletes to the index
        public ulong totalFastInserts;       // cumulative number of fast inserts to the index
        public long numKeys;                // number of keys currently indexed
        public ulong numValidKeys;           //  number of keys that are not tombstoned 

        public BTreeStats()
        {
            depth = 0;
            numLeafNodes = 0;
            numInternalNodes = 0;
            totalInserts = 0;
            totalDeletes = 0;
            totalFastInserts = 0;
            numKeys = 0;
            numValidKeys = 0;
        }
    }
}