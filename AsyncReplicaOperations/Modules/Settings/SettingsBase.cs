using System.Collections.Generic;
using System.Xml;

namespace AsyncReplicaOperations
{
    public abstract class SettingsBase : SlaveBase
    {
        protected XmlDocument document;
        private List<SettingEntityBase> entityBases;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected SettingsBase(string customPath = ""):base()
        {
            constructPrerequres();
            entityBases = new List<SettingEntityBase>();
            try
            {
                document = new XmlDocument();
                this.loadSettingsFile(customPath);

                var xmlRoot = document.DocumentElement;
                this.ParseXML(xmlRoot);
            }
            catch
            {
                entityBases = new List<SettingEntityBase>();
            }
        }

        protected virtual void constructPrerequres()
        {

        }

        abstract protected void ParseXML(XmlElement xmlRoot);

        abstract public bool isValid
        {
            get;
        }
        public List<SettingEntityBase> EntitiesList { get { return entityBases; } }

        abstract protected void loadSettingsFile(string customPath);

        abstract protected void loadSettingsFile();

        abstract public void SaveConfig(string path);


    }
}
