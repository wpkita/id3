using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSID3
{
    public class Attribute
    {
        public Attribute(string name, int columnNumber, bool isDiscrete, List<string> values)
        {
            Name = name;
            ColumnNumber = columnNumber;
            IsDiscrete = isDiscrete;
            Values = values;

            if (!isDiscrete)
            {
                Values = new List<string>();
                Values.Add("low");
                Values.Add("high");
            }
        }

        public int ColumnNumber { get; private set; }
        public string Name { get; set; }
        public bool IsDiscrete { get; private set; }
        public List<string> Values { get; private set; }
        public int NumUsesInTree { get; set; }
    }
}
