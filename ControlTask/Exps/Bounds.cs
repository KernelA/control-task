namespace ControlTask.Exps
{
    using System.Xml.Serialization;

    public class Bounds
    {
        [XmlAttribute]
        public double Lower { get; set; }

        [XmlAttribute]
        public double Upper { get; set; }

        public Bounds()
        {
        }

        public Bounds(double Lower, double Upper)
        {
            this.Lower = Lower;
            this.Upper = Upper;
        }
    }
}