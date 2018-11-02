using System;
using System.Collections;
using System.Collections.Generic;

namespace AsyncReplicaOperations
{
    internal class GlobalCoordinator:ICollection<SlaveBase>
    {
        private static GlobalCoordinator instance;
        private List<SlaveBase> libraryClasses;

        private GlobalCoordinator()
        {
            libraryClasses = new List<SlaveBase>();
        }

        public int Count
        {
            get
            {
                return libraryClasses.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public static GlobalCoordinator GetInstance()
        {
            if(instance == null)
            {
                instance = new GlobalCoordinator();
            }
            return instance;
        }

        public void Add(SlaveBase item)
        {
            libraryClasses.Add(item);
        }

        public void Clear()
        {
            foreach(SlaveBase s in libraryClasses)
            {
                s.Dispose();
            }
            libraryClasses.Clear();
        }

        public bool Contains(SlaveBase item)
        {
            return libraryClasses.Contains(item);
        }

        public void CopyTo(SlaveBase[] array, int arrayIndex)
        {
            libraryClasses.CopyTo(array, arrayIndex);
        }

        public IEnumerator<SlaveBase> GetEnumerator()
        {
            return libraryClasses.GetEnumerator();
        }

        public bool Remove(SlaveBase item)
        {
            Boolean ret =true;
            try
            {
                var libItem = libraryClasses.Find(x => x == item);
                libItem.Dispose();
                libraryClasses.Remove(libItem);
            }
            catch
            {
                ret = false;
            }
            return ret;

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return libraryClasses.GetEnumerator();
        }
    }
}
