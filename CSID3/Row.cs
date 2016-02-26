using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSID3
{
    public class Row
    {
        public Row(Dictionary<Attribute, double> values)
        {
            Values = values;
        }

        public Dictionary<Attribute, double> Values { get; private set; }
    }
}
