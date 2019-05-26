namespace ControlTask.Exps
{
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    public class OOExp : BaseControlParams
    {
        public Bounds Lambda1Bounds { get; set; }

        public Bounds Lambda2Bounds { get; set; }

        [XmlArray()]
        public OOParams[] Params { get; set; }

        [XmlAttribute]
        public ProblemType ProblemType { get; set; }

        public XmlSchema GetSchema() => null;
    }

    public enum ProblemType { I1, I2 };
}