using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using BottomlessIntegerNSA;

namespace BottomlessLists
{
    [Serializable]
    public class BigList<T> : IEnumerable<T>, IEnumerable, IDisposable
    {
        private static int _defaultCapacity = 1000000;
        private List<T[]> _items;
        private bool _disposed = false;
        private readonly object _writeLock = new object();

        static DeepInteger _maxArraySize = new DeepInteger(1000000);

        public DeepInteger _firstIndex = null;
        private DeepInteger _iterationCap = null;

        DeepInteger _lastIndex = -1;
        public DeepInteger LastIndex
        {
            get
            {
                return _lastIndex;
            }

            set
            {
                _lastIndex = value;
            }
        }

        DeepInteger _capacity = DeepInteger.Zero;
        public DeepInteger Capacity
        {
            get
            {
                return _capacity;
            }
        }
        public DeepInteger Length
        {
            get
            {
                return _lastIndex + 1;
            }
        }
        public DeepInteger Count
        {
            get
            {
                return Length - (_firstIndex is null ? DeepInteger.Zero : _firstIndex); ;
            }
        }

        public void SetIterationCap(DeepInteger _cap)
        {
            if (_cap is null)
            {
                _iterationCap = null;
            }
            else
            {
                _iterationCap = new DeepInteger(_cap);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ArrayEnumerator(this);
        }

        public IEnumerator<T> GetEnumerator(DeepInteger setStartIndex)
        {
            return new ArrayEnumerator(this, setStartIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ArrayEnumerator(this);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public T Last()
        {
            return this[LastIndex];
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _items = null;
                    _disposed = true;
                }
            }
        }
        ~BigList()
        {
            Dispose(true);
        }

        public BigList()
        {
            this._items = new List<T[]>();
            EnsureCapacity(0);
        }

        public BigList(BigList<T> ba)
        {
            this._items = new List<T[]>();
            EnsureCapacity(ba.Capacity);
            CopyContents(ba, ba.Length);
            this._lastIndex = ba._lastIndex;
        }

        public BigList(DeepInteger capacity)
        {
            this._items = new List<T[]>();
            EnsureCapacity(capacity);
            this._lastIndex = capacity - 1;
        }

        public BigList(IEnumerable<T> ienum)
        {
            DeepInteger cap = ienum.GetCapacity();
            this._items = new List<T[]>();
            EnsureCapacity(cap);
            CopyContents(ienum, cap);
            this._lastIndex = cap - 1;
        }

        public BigList(IEnumerable<T> ienum, DeepInteger capacity)
        {
            this._items = new List<T[]>();
            EnsureCapacity(capacity);
            CopyContents(ienum, capacity);
            this._lastIndex = ienum.GetCapacity() - 1;
        }

        // Not thread safe
        public T this[DeepInteger index]
        {
            get
            {
                if (index > _lastIndex)
                {
                    throw new ArgumentOutOfRangeException();
                }

                DeepInteger iI = index;
                DeepInteger oI = index >= _maxArraySize ? DeepInteger.DivideRem(index, _maxArraySize, out iI) : 0;

                int innerIndex = (int)iI;
                int outerIndex = (int)oI;

                return _items[outerIndex][innerIndex];
            }

            set
            {
                if (index > _capacity)
                {
                    throw new ArgumentOutOfRangeException();
                }

                DeepInteger iI = index;
                DeepInteger oI = index >= _maxArraySize ? DeepInteger.DivideRem(index, _maxArraySize, out iI) : 0;

                int innerIndex = (int)iI;
                int outerIndex = (int)oI;

                _items[outerIndex][innerIndex] = value;
            }
        }

        private void CopyContents(IEnumerable<T> contents)
        {
            CopyContents(contents, 0, 0, contents.GetCapacity());
        }

        private void CopyContents(IEnumerable<T> contents, DeepInteger length)
        {
            CopyContents(contents, 0, 0, length);
        }

        private void CopyContents(IEnumerable<T> contents, DeepInteger startFrom, DeepInteger length)
        {
            CopyContents(contents, startFrom, 0, length);
        }

        private void CopyContents(IEnumerable<T> contents, DeepInteger startFrom, DeepInteger toStart, DeepInteger length)
        {
            // The total amount of content being looped over equals
            //DeepInteger capFromContent = startFrom + length;
            // The total amount of content required equals
            DeepInteger targetCap = toStart + length;
            // The actual elements to copy then are
            //BigList<T> temp = new BigList<T>(length);

            EnsureCapacity(targetCap);

            DeepInteger index1 = 0;
            foreach (var item in contents)
            {
                if (index1 > _capacity)
                {
                    EnsureCapacity(index1);
                }

                if (index1 >= startFrom)
                {
                    this[index1] = (T)Activator.CreateInstance(typeof(T), new object[] { item });
                }

                ++index1;
            }
        }

        public static void Copy(BigList<T> sourceArray, BigList<T> destinationArray)
        {
            Copy(sourceArray, 0, destinationArray, 0, sourceArray.Length);
        }

        public static void Copy(BigList<T> sourceArray, BigList<T> destinationArray, DeepInteger length)
        {
            Copy(sourceArray, 0, destinationArray, 0, length);
        }

        public static void Copy(BigList<T> sourceArray, DeepInteger sourceIndex, BigList<T> destinationArray, DeepInteger destinationIndex, DeepInteger length)
        {
            if (null == sourceArray)
            {
                throw new ArgumentNullException("sourceArray");
            }

            if (null == destinationArray)
            {
                throw new ArgumentNullException("destinationArray");
            }

            destinationArray.EnsureCapacity(length);
            destinationArray.CopyContents(sourceArray, sourceIndex, destinationIndex, length);
        }

        public void EnsureCapacity(DeepInteger min)
        {
            if (_capacity < min)
            {
                DeepInteger defCap = new DeepInteger(_defaultCapacity);
                DeepInteger newCap = _capacity + defCap; //_capacity == 0 ? _defaultCapacity : _capacity * 2;

                while (newCap < min)
                {
                    newCap += defCap;
                }

                DeepInteger newArraysNeeded = new DeepInteger(newCap / _maxArraySize);

                if (null == _items)
                {
                    _items = new List<T[]>();
                }

                newArraysNeeded -= new DeepInteger(_items.Count());
                for (DeepInteger i = DeepInteger.Zero; i < newArraysNeeded; ++i)
                {
                    _items.Add(new T[_defaultCapacity]);
                }

                _capacity = newCap;
            }
        }

        // Thread safe
        private void SecureAdd(T item)
        {
            lock (_writeLock)
            {
                LastIndex++;

                if (LastIndex == Capacity)
                {
                    EnsureCapacity(Capacity + 1);
                }

                this[LastIndex] = (T)Activator.CreateInstance(typeof(T), new object[] { item });
            }
        }

        public void Add(T item)
        {
            SecureAdd(item);
        }

        public bool AddUnique(T item)
        {
            if (!Contains(item))
            {
                SecureAdd(item);
                return true;
            }
            return false;
        }

        BigList<T> _range;
        static readonly BigList<T> _emptyRange = new BigList<T>();
        DeepInteger _rangeIndexer = DeepInteger.Zero;
        private void SecureRemove(T item)
        {
            lock (_writeLock)
            {
                if (IndexedContains(item, out DeepInteger itemIndex))
                {
                    _range = _emptyRange;

                    for (DeepInteger i = itemIndex + 1; i < LastIndex; ++i)
                    {
                        _range.Add(this[i]);
                    }

                    _rangeIndexer = DeepInteger.Zero;
                    for (DeepInteger i = itemIndex; i < LastIndex - 1; ++i)
                    {
                        this[i] = _range[_rangeIndexer];
                        _rangeIndexer++;
                    }

                    --LastIndex;
                }
            }
        }

        public void Remove(T item)
        {
            SecureRemove(item);
        }

        public void Remove(IEnumerable<T> items)
        {
            foreach (var i in items)
            {
                SecureRemove(i);
            }
        }

        private bool _contains(T item)
        {
            foreach (var root in _items)
            {
                if (root.Contains(item))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IndexedContains(T item, out DeepInteger index)
        {
            index = new DeepInteger(0);

            foreach (var root in _items)
            {
                if (!(root is null))
                {
                    foreach (T i in root)
                    {
                        object oi = i;

                        if (!(oi is null))
                        {
                            if (i.Equals(item))
                            {
                                return true;
                            }
                        }
                        index++;
                    }
                }
            }
            return false;
        }

        public bool Contains(T item)
        {
            return _contains(item);
            //return IndexedContains(item, out DeepInteger index);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var i in items)
            {
                SecureAdd(i);
            }
        }

        public BigList<T> GetRange(DeepInteger start, DeepInteger count)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException("Attempted to get a Range starting with an Index out of bounds.");
            }

            if ((start + count) > Length)
            {
                throw new ArgumentOutOfRangeException("Attempted to get values in a Range exceeding collection capacity.");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("Attempted to get a negative amount of values as a Range.");
            }

            BigList<T> outArray = new BigList<T>(count);

            DeepInteger iterations = 0;
            for (DeepInteger i = start; iterations < count; ++i)
            {
                outArray[iterations] = this[i];
                ++iterations;
            }

            return outArray;
        }

        public void AddAt(DeepInteger index, T i)
        {
            EnsureCapacity(index + 1);

            if (index > LastIndex)
            {
                LastIndex = index;
            }

            this[index] = i;
        }

        [Serializable]
        public struct ArrayEnumerator : IEnumerator<T>, IEnumerable<T>
        {
            private readonly BigList<T> _array;
            private DeepInteger _index;
            private DeepInteger _defaultStartIndex;
            public T Current { get; private set; }

            private DeepInteger _iterationCap;

            object IEnumerator.Current
            {
                get
                {
                    if (_index == _array.Length + 1)
                    {
                        throw new InvalidOperationException($"Enumerator out of range: {_index}");
                    }
                    return Current;
                }
            }

            internal ArrayEnumerator(BigList<T> arr)
            {
                _array = arr;
                _index = 0;
                _defaultStartIndex = 0;
                _iterationCap = arr._iterationCap;
                Current = default;
            }

            internal ArrayEnumerator(BigList<T> arr, DeepInteger startIndex)
            {
                _array = arr;
                _defaultStartIndex = startIndex;
                _index = _defaultStartIndex;
                _iterationCap = arr._iterationCap;
                Current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_index < _array.Length &&
                    (!(_iterationCap is null) ? _index <= _iterationCap : true))
                {
                    Current = _array[_index];
                    _index += 1;
                    return true;
                }

                _index = _array.Length + 1;
                Current = default;
                return false;
            }

            public void Reset()
            {
                _index = _defaultStartIndex;
                Current = default;
            }

            public T Last()
            {
                return _array[_array.Length];
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }
    }
    public static class BigArrayUtilities
    {
        public static BigList<DeepInteger> Sort(BigList<DeepInteger> toSort)
        {
            BigList<DeepInteger> sortedList = new BigList<DeepInteger>(toSort.Length);
            foreach (var i in toSort)
            {
                if (!(i is null))
                {
                    DeepInteger index = 0;
                    foreach (var j in toSort)
                    {
                        if (!(j is null))
                        {
                            if (!i.Equals(j))
                            {
                                if (i > j)
                                {
                                    index++;
                                }
                            }
                        }
                    }

                    sortedList[index] = i;
                }
            }
            return sortedList;
        }

        public static DeepInteger GetCapacity<T>(this IEnumerable<T> _arr)
        {
            DeepInteger cap = DeepInteger.Zero;

            if (_arr is BigList<T>)
            {
                cap = (_arr as BigList<T>).Length;
            }
            else if (_arr is ICollection<T>)
            {
                cap = (_arr as ICollection<T>).Count;
            }
            else if (_arr is Array)
            {
                cap = (_arr as Array).Length;
            }
            else
            {
                DeepInteger lCount = DeepInteger.Zero;

                foreach (var i in _arr)
                {
                    ++lCount;
                }

                cap = lCount;
            }

            return cap;
        }

        public static DeepInteger GetCapacity(this IEnumerable _arr)
        {
            DeepInteger cap = DeepInteger.Zero;

            if (_arr is ICollection)
            {
                cap = (_arr as ICollection).Count;
            }
            else if (_arr is Array)
            {
                cap = (_arr as Array).Length;
            }
            else
            {
                DeepInteger lCount = DeepInteger.Zero;

                foreach (var i in _arr)
                {
                    ++lCount;
                }

                cap = lCount;
            }

            return cap;
        }
    }
}