namespace ControlTask.Exps
{
    using System;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    public class OOParams : IXmlSerializable
    {
        [XmlIgnore]
        public object Parameters { get; set; }

        public OOParams(object Param)
        {
            Parameters = Param;
        }

        public OOParams()
        {
        }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            string name = reader.GetAttribute("Name");

            Type bbbc = typeof(EOpt.Math.Optimization.BBBCParams);
            Type gem = typeof(EOpt.Math.Optimization.GEMParams);
            Type fw = typeof(EOpt.Math.Optimization.FWParams);

            Type paramsType = null;

            if (bbbc.Name == name)
            {
                paramsType = bbbc;
            }
            else if (gem.Name == name)
            {
                paramsType = gem;
            }
            else
            {
                paramsType = fw;
            }

            foreach (var constr in paramsType.GetConstructors())
            {
                if (constr.IsPublic)
                {
                    var constrParams = constr.GetParameters();

                    if (constrParams.Length == reader.AttributeCount - 1)
                    {
                        object[] parameters = new object[constr.GetParameters().Length];
                        int i = 0;

                        foreach (var param in constrParams)
                        {
                            string value = reader.GetAttribute(param.Name);

                            int paramVal = 0;

                            if (int.TryParse(value, out paramVal))
                            {
                                parameters[i++] = paramVal;
                            }
                            else
                            {
                                parameters[i++] = double.Parse(value);
                            }
                        }

                        this.Parameters = constr.Invoke(parameters);
                        break;
                    }
                }
            }
            reader.Read();
        }

        public void WriteXml(XmlWriter writer)
        {
            Type type = Parameters.GetType();

            writer.WriteAttributeString("Name", type.Name);

            foreach (var prop in type.GetProperties())
            {
                if (prop.PropertyType != typeof(bool))
                {
                    writer.WriteAttributeString(prop.Name, prop.GetValue(Parameters).ToString());
                }
            }
        }
    }
}