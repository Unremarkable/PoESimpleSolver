using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace POESKillTree.Utility
{
    class BucketQueue<TValue>
    {
        private Stack<TValue>[] buckets = new Stack<TValue>[125];
        private int lowestPriority = 125;
        private int count =0;

        public BucketQueue()
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new Stack<TValue>();
            }
        }

        public void Enqueue(TValue value, int priority)
        {
            count++;
            buckets[priority].Push(value);
            if (priority < lowestPriority)
                lowestPriority = priority;
        }

        public TValue Dequeue()
        {
            if (IsEmpty())
                throw new InvalidOperationException("Can't dequeue from an empty queue, jackass");
            TValue lowest = buckets[lowestPriority].Pop();
            if (buckets[lowestPriority].Count == 0)
            {
                for (int i = lowestPriority + 1; i < buckets.Length; i++)
                {
                    lowestPriority = i;
                    if (buckets[i].Count > 0)
                    {
                        break;
                    }
                }
            }
            count--;
            return lowest;
        }

        public bool IsEmpty()
        {
            return count <= 0;
        }
    }
}
