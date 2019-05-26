namespace ControlTask.Exps
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    public class OOExps
    {
        [XmlArrayItem]
        public OOExp[] OOExperiments { get; set; }

        public OOExps()
        {
            OOExperiments = null;
        }

        public OOExps(IReadOnlyCollection<OOExp> Exps)
        {
            this.OOExperiments = Exps.ToArray();
        }

        public static OOExps Load(string PathToXml, ValidationEventHandler XmlConfigHandler)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(OOExps), "control/config");

            object desExp = null;

            using (FileStream fs = new FileStream(PathToXml, FileMode.Open))
            {
                using (FileStream schemaStream = new FileStream(Path.Combine("ConfigXSD", "ConfigSchema.xsd"), FileMode.Open))
                {
                    XmlSchema schema = XmlSchema.Read(schemaStream, null);
                    XmlReaderSettings settings = new XmlReaderSettings
                    {
                        ValidationType = ValidationType.Schema
                    };
                    settings.Schemas.Add(schema);
                    settings.ValidationEventHandler += XmlConfigHandler;
                    settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;

                    XmlReader reader = XmlReader.Create(fs, settings);

                    while (reader.Read())
                    {
                    }
                    desExp = serializer.Deserialize(reader);
                }
            }

            return desExp as OOExps;
        }

        public void Save(string PathToXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(OOExps));

            using (FileStream fs = new FileStream(PathToXml, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }
    }
}