namespace ControlTask.Exps
{
    using System.Xml;
    using System.Xml.Serialization;

    public class BaseControlParams
    {
        protected bool isInitizlized;

        [XmlAttribute]
        public int NSteps { get; set; }

        [XmlAttribute]
        public int NSwitches { get; set; }

        [XmlAttribute]
        public double TMax { get; set; }

        public Bounds U1Bounds { get; set; }

        public Bounds U2Bounds { get; set; }

        [XmlAttribute]
        public double X10 { get; set; }

        [XmlAttribute]
        public double X20 { get; set; }

        public BaseControlParams()
        {
            isInitizlized = false;
        }

        public void Deconstruct(out int NSwitches, out double Tmax, out double X10, out double X20, out Bounds U1Bounds, out Bounds U2Bounds, out int NSteps)
        {
            NSwitches = this.NSwitches;
            Tmax = this.TMax;
            X10 = this.X10;
            X20 = this.X20;
            U1Bounds = this.U1Bounds;
            U2Bounds = this.U2Bounds;
            NSteps = this.NSteps;
        }
    }
}