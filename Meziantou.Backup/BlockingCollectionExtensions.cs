// http://blogs.msdn.com/b/pfxteam/archive/2010/04/06/9990420.aspx

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Meziantou.Backup
{
    internal static class BlockingCollectionExtensions
    {
        /// <summary>
        /// Gets a partitioner for a BlockingCollection that consumes and yields the contents of the BlockingCollection.</summary>
        /// <typeparam name="T">Specifies the type of data in the collection.</typeparam>
        /// <param name="collection">The collection for which to create a partitioner.</param>
        /// <returns>A partitioner that completely consumes and enumerates the contents of the collection.</returns>
        /// <remarks>
        /// Using this partitioner with a Parallel.ForEach loop or with PLINQ eliminates the need for those
        /// constructs to do any additional locking.  The only synchronization in place is that used by the
        /// BlockingCollection internally.
        /// </remarks>
        public static Partitioner<T> GetConsumingPartitioner<T>(this BlockingCollection<T> collection, CancellationToken ct)
        {
            return new BlockingCollectionPartitioner<T>(collection, ct);
        }

        private class BlockingCollectionPartitioner<T> : Partitioner<T>
        {
            private readonly BlockingCollection<T> _collection;
            private readonly CancellationToken _ct;
            
            internal BlockingCollectionPartitioner(BlockingCollection<T> collection, CancellationToken ct)
            {
                if (collection == null) throw new ArgumentNullException(nameof(collection));

                _collection = collection;
                _ct = ct;
            }

            public override bool SupportsDynamicPartitions => true;

            public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
            {
                if (partitionCount < 1)
                    throw new ArgumentOutOfRangeException(nameof(partitionCount));

                var dynamicPartitioner = GetDynamicPartitions();
                return Enumerable.Range(0, partitionCount).Select(_ => dynamicPartitioner.GetEnumerator()).ToArray();
            }

            public override IEnumerable<T> GetDynamicPartitions()
            {
                return _collection.GetConsumingEnumerable(_ct);
            }
        }   
    }
}