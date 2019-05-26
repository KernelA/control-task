namespace ControlTask.Exps
{
    using System.Xml.Serialization;

    public class MOFWParam
    {
        [XmlAttribute]
        public double Amax { get; set; }

        [XmlAttribute]
        public int Imax { get; set; }

        [XmlAttribute]
        public double M { get; set; }

        [XmlAttribute]
        public int NP { get; set; }

        [XmlAttribute]
        public int Smax { get; set; }

        [XmlAttribute]
        public int Smin { get; set; }
    }
}