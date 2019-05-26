namespace ControlTask.Exps
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    [XmlRoot(Namespace = "experiments/moconfig")]
    public class MOControlExperiments
    {
        public MOExp[] MOExperiments { get; set; }

        public MOControlExperiments()
        {
            MOExperiments = null;
        }

        public MOControlExperiments(IReadOnlyCollection<MOExp> Exps)
        {
            this.MOExperiments = Exps.ToArray();
        }

        /// <summary>
        /// </summary>
        /// <param name="PathToXml">  </param>
        /// <exception cref="InvalidOperationException">  </exception>
        /// <returns>  </returns>
        public static MOControlExperiments Load(string PathToXml, XmlSchema Schema)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MOControlExperiments));

            object desExp = null;

            NLog.Logger logger = NLog.LogManager.GetLogger("Main");

            logger.Info($"Trying to open '{PathToXml}' with experiments.");

            using (FileStream fs = new FileStream(PathToXml, FileMode.Open))
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                settings.Schemas.Add(Schema);
                XmlReader reader = XmlReader.Create(fs, settings);
                desExp = serializer.Deserialize(reader);
            }

            logger.Info($"Experiments loaded.");

            return desExp as MOControlExperiments;
        }

#if DEBUG

        public void Save(string PathToXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MOControlExperiments));

            using (FileStream fs = new FileStream(PathToXml, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }

#endif
    }
}