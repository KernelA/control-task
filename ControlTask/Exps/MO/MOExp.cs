namespace ControlTask.Exps
{
    using System.Xml;
    using System.Xml.Serialization;

    public class MOExp : BaseControlParams
    {
        [XmlAttribute]
        public double Lambda1 { get; set; }

        [XmlAttribute]
        public double Lambda2 { get; set; }

        [XmlAttribute]
        public double Lambda3 { get; set; }

        [XmlAttribute]
        public double Lambda4 { get; set; }

        [XmlArrayItem]
        public MOFWParam[] MOFWParams { get; set; }

        [XmlAttribute]
        public int TotalRun { get; set; }

        public MOExp()
        {
            isInitizlized = false;
        }

        public void Deconstruct(out int NSwitches, out double Tmax, out double X10, out double X20, out Bounds U1Bounds, out Bounds U2Bounds, out double Lambda1
            , out double Lambda2, out double Lambda3, out double Lambda4, out int TotalRun, out int NSteps)
        {
            base.Deconstruct(out NSwitches, out Tmax, out X10, out X20, out U1Bounds, out U2Bounds, out NSteps);
            Lambda1 = this.Lambda1;
            Lambda2 = this.Lambda2;
            TotalRun = this.TotalRun;
            Lambda3 = this.Lambda3;
            Lambda4 = this.Lambda4;
        }
    }
}