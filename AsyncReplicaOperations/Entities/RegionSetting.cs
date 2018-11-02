namespace AsyncReplicaOperations
{
    public class RegionSetting:SettingEntityBase
    {
        private string Id;
        private string Name;
        private string Server;
        private string StageName;

        public string RegionId
        {
            get
            {
                return Id;
            }
            set
            {
                Id = value;
            }
        }

        public string RegionName
        {
            get
            {
                return Name;
            }
            set
            {
                Name = value;
            }
        }

        public string ServerName
        {
            get
            {
                return Server;
            }
            set
            {
                Server = value;
            }
        }

        public string StageDBName
        {
            get
            {
                return StageName;
            }
            set
            {
                StageName = value;
            }
        }
    }
}
