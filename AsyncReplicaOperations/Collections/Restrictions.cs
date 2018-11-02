using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncReplicaOperations
{
    public class Restrictions : ICollection<Restriction>
    {
        List<Restriction> restrictions;
        int countRestrictions;
        public Restrictions()
        {
            restrictions = new List<Restriction>();
        }
        public int Count
        {
            get
            {
                return restrictions.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public int CountRestrictions
        {
            get
            {
                return countRestrictions;
            }

            set
            {
                countRestrictions = value;
            }
        }

        public void Add(Restriction item)
        {
            restrictions.Add(item);
        }

        public void Clear()
        {
            restrictions.Clear();
        }

        public bool Contains(Restriction item)
        {
            return restrictions.Contains(item);
        }

        public void CopyTo(Restriction[] array, int arrayIndex)
        {
            restrictions.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Restriction> GetEnumerator()
        {
            return restrictions.GetEnumerator();
        }

        public bool Remove(Restriction item)
        {
            return restrictions.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return restrictions.GetEnumerator();
        }
    }
}
