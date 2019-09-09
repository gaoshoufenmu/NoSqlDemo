using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QZ.Redis.Search
{
    public class BigList<T> where T : class
    {
        /// <summary>
        /// special used, so it need not to be too large
        /// </summary>
        private const int INNER_CAPACITY = 0x1000;
        private const int OUTER_CAPACITY = 0x100;
        //! ----------------- big list capacity is INNER_CAPACITY * OUTER_CAPACITY

        private int _count;
        public int Count { get { return _count; } }
        private List<List<T>> _bucket;

        public BigList()
        {
            _bucket = new List<List<T>>(1);
        }

        public T this[int index]
        {
            get { return Get(index); }
        }

        public T Get(int index)
        {
            var listof2nd = GetListFrom1st(index);
            if (listof2nd == null) return null;

            var inindex = GetIndexFrom2nd(index);
            if (inindex < listof2nd.Count)
                return listof2nd[inindex];
            return null;
        }
        public int Add(T t)
        {
            var list = GetListFrom1st(_count);
            if(list == null || list.Count == INNER_CAPACITY)
            {
                if (_bucket.Count == OUTER_CAPACITY) throw new Exception("big list is full");

                list = new List<T>();
                _bucket.Add(list);
            }

            list.Add(t);
            var index = _count;
            _count++;
            return index;
        }

        private static int GetIndexFrom1st(int index) => index / INNER_CAPACITY;
        private static int GetIndexFrom2nd(int index) => index % INNER_CAPACITY;
        private List<T> GetListFrom1st(int index)
        {
            var outIndex = GetIndexFrom1st(index);
            if (outIndex < _bucket.Count)
                return _bucket[outIndex];
            return null;
        }


        public void Clear()
        {
            foreach(var list in _bucket)
            {
                list.Clear();
            }
            _bucket.Clear();
            _count = 0;
        }
    }
}
