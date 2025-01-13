// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Garnet.server.BTreeIndex
{
    public unsafe class BPlusTree
    {
        BTreeNode* root;
        BTreeNode* head;
        BTreeNode* tail;
        byte* tailMinKey;
        public static readonly int MAX_TREE_DEPTH = 10; // maximum allowed depth of the tree
        static int DEFAULT_SPLIT_LEAF_POSITION = (BTreeNode.LEAF_CAPACITY + 1) / 2; // position at which leaf node is split
        static int SPLIT_LEAF_POSITION = BTreeNode.LEAF_CAPACITY - 1; // position at which leaf node is split

        BTreeNode*[] rootToTailLeaf; // array of nodes from root to tail leaf
        BTreeStats stats; // statistics about the tree

        public BPlusTree()
        {
            // TODO: Use a different memory allocation policy
            root = (BTreeNode*)Marshal.AllocHGlobal(sizeof(BTreeNode)).ToPointer();
            root->Allocate(BTreeNodeType.Leaf);
            head = tail = root;
            root->info->next = root->info->previous = null;
            root->info->count = 0;
            tailMinKey = null;
            rootToTailLeaf = new BTreeNode*[MAX_TREE_DEPTH];
            stats = new BTreeStats();
            stats.depth = 1;
            stats.numLeafNodes = 1;
        }
    }
}