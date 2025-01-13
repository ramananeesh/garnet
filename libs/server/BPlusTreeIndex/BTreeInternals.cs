// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;

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