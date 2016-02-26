using System.Collections.Generic;

namespace Id3
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
