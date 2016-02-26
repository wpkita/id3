using System.Collections.Generic;

namespace Id3
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
                Values = new List<string> {"low", "high"};
            }
        }

        public int ColumnNumber { get; private set; }
        public string Name { get; set; }
        public bool IsDiscrete { get; private set; }
        public List<string> Values { get; }
        public int NumUsesInTree { get; set; }
    }
}
