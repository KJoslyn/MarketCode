using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class TimeSortedCollection<T> : ICollection<T> where T : HasTime
    {
        private List<T> _list;

        public TimeSortedCollection() {
            _list = new List<T>(); 
        }

        public TimeSortedCollection(IEnumerable<T> items) 
        {
            _list = new List<T>(items);
            Sort();
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _list.Add(item);
            Sort();
        }

        public void Clear() => _list.Clear();

        public bool Contains(T item) => _list.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        public bool Remove(T item)
        {
            bool result = _list.Remove(item);
            Sort();
            return result;
        }

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        private void Sort()
        {
            _list = _list.OrderBy(obj => obj.Time).ToList();
        }
    }
}
