using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncReplicaOperations
{
    public class RuntimeRegionSettings : RegionSetting
    {
        private DirectionsEnum direction;
        private Restrictions restrictions;

        public RuntimeRegionSettings()
        {
            restrictions = new Restrictions();
        }

        public DirectionsEnum Direction { get { return direction; } set { direction = value; } }

        public Restrictions Restrictions
        {
            get
            {
                return restrictions;
            }

            set
            {
                restrictions = value;
            }
        }
    }
}
