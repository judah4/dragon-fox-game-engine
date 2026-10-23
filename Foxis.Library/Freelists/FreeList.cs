using System;
using System.Collections.Generic;

namespace Foxis.Library.Freelists
{
    public sealed class FreeList
    {

        private sealed class FreeListNode
        {
            public FreeListNode(ulong offset, ulong size, FreeListNode? next)
            {
                this.Offset = offset;
                this.Size = size;
                this.Next = next;
            }

            public ulong Offset { get; private set; }
            public ulong Size { get; private set; }
            public FreeListNode? Next { get; private set; }

            public void Update(ulong offset, ulong size)
            {
                this.Offset = offset;
                this.Size = size;
            }

            public void UpdateNext(FreeListNode? next)
            {
                Next = next;
            }
        }

        private const uint MINIMUM_DATA_BLOCK = 8; //8 bytes

        private ulong _totalMemorySize;
        private uint _maxEntries;
        private FreeListNode _head;
        private readonly Queue<FreeListNode> _nodePool;

        public FreeList(ulong totalMemorySize)
        {
            // Enough space to hold state, plus array for all nodes.
            int maxEntries = (int)(totalMemorySize / MINIMUM_DATA_BLOCK);  //NOTE: This might have a remainder, but that's ok.

            // If the memory required is too small, should warn about it being wasteful to use.
            if (totalMemorySize < MINIMUM_DATA_BLOCK)
            {
                throw new ArgumentException($"total memory {totalMemorySize} should be larger than the minimum data block size {MINIMUM_DATA_BLOCK}.", nameof(totalMemorySize));
            }

            // The block's layout is head* first, then array of available nodes.
            this._nodePool = new Queue<FreeListNode>(maxEntries);
            this._maxEntries = (uint)maxEntries;
            this._totalMemorySize = totalMemorySize;

            _head = new FreeListNode(0, totalMemorySize, null);
        }

        public void Destroy()
        {
            _nodePool.Clear();
            _head.UpdateNext(null);
            _head.Update(0, 0);
        }

        /// <summary>
        /// Attempts to find a free block of memory of the given size.
        /// </summary>
        /// <param name="size">The size to allocate.</param>
        /// <returns>Result with the offset if successful</returns>
        public Result<ulong> AllocateBlock(ulong size)
        {
            ulong resultOffset;
            FreeListNode? node = _head;
            FreeListNode? previous = null;
            while (node != null)
            {
                if (node.Size == size)
                {
                    // Exact match. Just return the node.
                    resultOffset = node.Offset;
                    FreeListNode? nodeToReturn = null;
                    if (previous != null)
                    {
                        previous.UpdateNext(node.Next);
                        nodeToReturn = node;
                        ReturnNode(nodeToReturn);
                    }
                    else
                    {
                        // This node is the head of the list. Reassign the head
                        // and return the previous head node.
                        nodeToReturn = _head;
                        if(node.Next != null)
                        {
                            _head = node.Next;
                            ReturnNode(nodeToReturn);
                        }
                        else
                        {
                            //zero out if this is the remaining data
                            node.Update(node.Offset + size, node.Size - size);
                        }
                    }
                    return Result.Ok(resultOffset);
                }
                else if (node.Size > size)
                {
                    // Node is larger. Deduct the memory from it and move the offset
                    // by that amount.
                    resultOffset = node.Offset;
                    node.Update(node.Offset + size, node.Size - size);
                    return Result.Ok(resultOffset);
                }

                previous = node;
                node = node.Next;
            }

            ulong free_space = GetFreeSpace();
            return Result.Fail<ulong>($"{nameof(AllocateBlock)}, no block with enough free space found (requested: {size}B, available: {free_space}B).");
        }

        /// <summary>
        /// Attempts to free a block of memory at the given offset, and of the given 
        /// size.Can fail if invalid data is passed.
        /// </summary>
        /// <param name="size">The size to be freed.</param>
        /// <param name="offset">The offset to free at.</param>
        /// <returns>Success or error</returns>
        public Result<bool> FreeBlock(ulong size, ulong offset)
        {
            FreeListNode? node = _head;
            FreeListNode? previous = null;
            while (node != null)
            {
                if (node.Offset == offset)
                {
                    // Can just be appended to this node.
                    node.Update(node.Offset, node.Size + size);

                    // Check if this then connects the range between this and the next
                    // node, and if so, combine them and return the second node..
                    if (node.Next != null && node.Next.Offset == node.Offset + node.Size)
                    {
                        node.Update(node.Offset, node.Size + node.Next.Size);
                        FreeListNode next = node.Next;
                        node.UpdateNext(node.Next.Next);
                        ReturnNode(next);
                    }
                    return Result.Ok(true);
                }
                else if (node.Offset > offset)
                {
                    // Iterated beyond the space to be freed. Need a new node.
                    FreeListNode newNode = GetNode();
                    newNode.Update(offset, size);

                    // If there is a previous node, the new node should be inserted between this and it.
                    if (previous != null)
                    {
                        previous.UpdateNext(newNode);
                        newNode.UpdateNext(node);
                    }
                    else
                    {
                        // Otherwise, the new node becomes the head.
                        newNode.UpdateNext(node);
                        _head = newNode;
                    }

                    // Double-check next node to see if it can be joined.
                    if (newNode.Next != null && newNode.Offset + newNode.Size == newNode.Next.Offset)
                    {
                        newNode.Update(newNode.Offset, newNode.Size + newNode.Next.Size);
                        FreeListNode rubbish = newNode.Next;
                        newNode.UpdateNext(rubbish.Next);
                        ReturnNode(rubbish);
                    }

                    // Double-check previous node to see if the new_node can be joined to it.
                    if (previous != null && previous.Offset + previous.Size == newNode.Offset)
                    {
                        previous.Update(previous.Offset, previous.Size + newNode.Size);
                        FreeListNode rubbish = newNode;
                        previous.UpdateNext(rubbish.Next);
                        ReturnNode(rubbish);
                        _head = previous;
                    }

                    return Result.Ok(true);
                }

                previous = node;
                node = node.Next;
            }

            return Result.Fail<bool>("Unable to find block to be freed. Corruption possible?");
        }

        public void Resize(ulong totalMemorySize)
        {
            if(totalMemorySize < _totalMemorySize)
            {
                throw new ArgumentException("Cannot resize free list to be smaller {totalMemorySize} than existing total memory size {_totalMemorySize}.", nameof(totalMemorySize));
            }

            // Enough space to hold state, plus array for all nodes.
            int maxEntries = (int)(totalMemorySize / MINIMUM_DATA_BLOCK);  //NOTE: This might have a remainder, but that's ok.

            // If the memory required is too small, should warn about it being wasteful to use.
            if (totalMemorySize < MINIMUM_DATA_BLOCK)
            {
                throw new ArgumentException("total memory should be larger than the minimum data block size.", nameof(totalMemorySize));
            }

            this._maxEntries = (uint)maxEntries;
            this._totalMemorySize = totalMemorySize;
        }

        /// <summary>
        /// Clears the free list.
        /// </summary>
        public void Clear()
        {
            _nodePool.Clear();
            _head.UpdateNext(null);
            _head.Update(0, 0);

            FreeListNode? node = _head.Next;

            while (node != null)
            {
                var next = node.Next;
                ReturnNode(node);
                node = next;
            }
        }

        /// <summary>
        /// Returns the amount of free space in this list. NOTE: Since this has
        /// to iterate the entire internal list, this can be an expensive operation.
        /// Use sparingly.
        /// </summary>
        /// <returns>The amount of free space in bytes.</returns>
        public ulong GetFreeSpace()
        {
            ulong running_total = 0;
            FreeListNode? node = _head;
            while (node != null)
            {
                running_total += node.Size;
                node = node.Next;
            }

            return running_total;
        }

        private FreeListNode GetNode()
        {
            if (_nodePool.Count > 0)
            {
                return _nodePool.Dequeue();
            }
            return new FreeListNode(0, 0, null);
        }

        private void ReturnNode(FreeListNode node)
        {
            node.UpdateNext(null);
            node.Update(0, 0);
            if(_nodePool.Count < _maxEntries)
            {
                _nodePool.Enqueue(node);
            }
        }
    }
}
