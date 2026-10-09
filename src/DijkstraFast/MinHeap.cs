using System;

namespace DijkstraFast
{
    /// <summary>
    /// A binary min-heap of (node, priority) pairs. Duplicate nodes are allowed;
    /// the search skips outdated entries when it pops them.
    /// </summary>
    internal sealed class MinHeap
    {
        private Entry[] _entries;
        private int _count;

        public MinHeap(int capacity = 16)
        {
            _entries = new Entry[Math.Max(capacity, 4)];
        }

        public int Count => _count;

        public void Push(int node, double priority)
        {
            if (_count == _entries.Length)
            {
                Array.Resize(ref _entries, _entries.Length * 2);
            }

            // Move parents down until the new entry's slot is found.
            var index = _count++;

            while (index > 0)
            {
                var parent = (index - 1) >> 1;

                if (_entries[parent].Priority <= priority)
                {
                    break;
                }

                _entries[index] = _entries[parent];
                index = parent;
            }

            _entries[index] = new Entry(node, priority);
        }

        public bool TryPop(out int node, out double priority)
        {
            if (_count == 0)
            {
                node = -1;
                priority = 0;

                return false;
            }

            node = _entries[0].Node;
            priority = _entries[0].Priority;

            // Take the last entry and move it down from the root to its slot.
            var last = _entries[--_count];
            var index = 0;

            while (true)
            {
                var child = (index << 1) + 1;

                if (child >= _count)
                {
                    break;
                }

                if (child + 1 < _count && _entries[child + 1].Priority < _entries[child].Priority)
                {
                    child++;
                }

                if (last.Priority <= _entries[child].Priority)
                {
                    break;
                }

                _entries[index] = _entries[child];
                index = child;
            }

            _entries[index] = last;

            return true;
        }

        private readonly struct Entry
        {
            public Entry(int node, double priority)
            {
                Node = node;
                Priority = priority;
            }

            public int Node { get; }

            public double Priority { get; }
        }
    }
}