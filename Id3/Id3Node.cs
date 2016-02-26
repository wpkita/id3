using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Id3
{
    public class Id3Node
    {
        private Attribute chosenAttribute;
        private double chosenAttributeThreshold;
        private Dictionary<double, Id3Node> branches = new Dictionary<double,Id3Node>();
        private bool isLeaf;
        private double leafLabel;
        static int nodeCount = 0;
        const int MAX_CONT_ATT_USES_PER_TREE = 15;
        int depth;

        public Id3Node(double leafLabel)
        {
            isLeaf = true;
            this.leafLabel = leafLabel;
        }

        public Id3Node(List<Row> rows, List<Attribute> attributes, Attribute targetAttribute, int depth)
        {
            this.depth = depth;

            Console.WriteLine("{0} to node {1}", rows.Count, nodeCount++);

            bool areAllTargetAttributeValuesTheSame = rows.Select(r => r.Values[targetAttribute])
                                                          .Distinct()
                                                          .Count() == 1;
            if (areAllTargetAttributeValuesTheSame)
            {
                isLeaf = true;
                leafLabel = rows.First().Values[targetAttribute];
            }
            else if (attributes.Count == 0)
            {
                isLeaf = true;
                leafLabel = GetMostCommonTargetAttributeValue(rows, targetAttribute);
            }
            else
            {
                ChooseAttribute(rows, attributes, targetAttribute);
                GrowBranches(rows, attributes, targetAttribute);
            }
        }

        public double Classify(Row row)
        {
            double assignedLabel;

            if (isLeaf)
            {
                assignedLabel = leafLabel;
            }
            else
            {
                assignedLabel = branches[GetDiscretizedAttributeValue(row.Values[chosenAttribute])].Classify(row);
            }

            return assignedLabel;
        }

        private void ChooseAttribute(List<Row> rows, List<Attribute> attributes, Attribute targetAttribute)
        {
            Dictionary<Attribute, double> attributeGains = new Dictionary<Attribute,double>();
            Dictionary<Attribute, double> attributeThresholds = new Dictionary<Attribute, double>();

            double entropyForAllRows = Entropy(rows.GroupBy(r => r.Values[targetAttribute]).Select(g => g.Count()).ToList());

            //for (int i = 0; i < depth; i++) Console.Write(" ");
            //Console.WriteLine("ENTROPY:\t{0}", entropyForAllRows);

            foreach (Attribute attribute in attributes)
            {
                if (attribute.IsDiscrete)
                {
                    attributeGains[attribute] = DiscreteGain(rows, attribute, targetAttribute, entropyForAllRows);
                }
                else
                {
                    Tuple<double, double> continuousGainInfo = ContinuousGain(rows, attribute, targetAttribute, entropyForAllRows);
                    attributeGains[attribute] = continuousGainInfo.Item1;
                    attributeThresholds[attribute] = continuousGainInfo.Item2;
                }
            }

            double highestGain = attributeGains.Values.Max();
            chosenAttribute = attributeGains.First(ag => ag.Value == highestGain).Key;

            for (int i = 0; i < depth; i++) Console.Write(" ");
            Console.WriteLine(":Chose {0}", chosenAttribute.Name);

            if (!chosenAttribute.IsDiscrete)
            {
                chosenAttributeThreshold = attributeThresholds[chosenAttribute];
            }
        }

        private double DiscreteGain(List<Row> rows, Attribute attribute, Attribute targetAttribute, double entropyForAllRows)
        {
            int numberOfRows = rows.Count;
            double gain = entropyForAllRows;

            for (int i = 0; i < attribute.Values.Count; i++)
            {
                double attributeValue = (double)i;
                var rowsWithAttributeValue = rows.Where(r => r.Values[attribute] == attributeValue);
                int numberOfRowsWithAttributeValue = rowsWithAttributeValue.Count();
                gain -= (double)numberOfRowsWithAttributeValue / numberOfRows * Entropy(rowsWithAttributeValue.GroupBy(r => r.Values[targetAttribute]).Select(g => g.Count()).ToList());
            }
            return gain;
        }

        /*
         * Completely different from before. In the old C++ code,
         * k passes would be made through the rows where k is the number of thresholds.
         * This new way only traverses the rows twice. Of course,
         * this method is run once per attribute, as before. This new
         * implementation, combined with using doubles instead of strings in memory,
         * greatly decreased the running time.
         */
        private Tuple<double, double> ContinuousGain(List<Row> rows, Attribute attribute, Attribute targetAttribute, double entropyForAllRows)
        {
            int numRows = rows.Count;

            int numBelowThreshold = 0;
            Dictionary<double, int> numLabelBelowThreshold = new Dictionary<double,int>();

            int numAboveThreshold = 0;
            Dictionary<double, int> numLabelAboveThreshold = new Dictionary<double, int>();

            for (int i = 0; i < targetAttribute.Values.Count; i++)
            {
                numLabelBelowThreshold[(double)i] = 0;
                numLabelAboveThreshold[(double)i] = 0;
            }

            double highestGain = double.MinValue;
            double thresholdWithHighestGain = 0;
            bool doesSameAttributeValueHaveDifferentTargetAttributeValues = false;

            var rowsOrderedByAttribute = rows.OrderBy(r => r.Values[attribute]).ToList();
            
            for (int i = 0; i < rowsOrderedByAttribute.Count; i++)
            {
                numAboveThreshold++;
                numLabelAboveThreshold[rowsOrderedByAttribute[i].Values[targetAttribute]]++;
            }

            for (int i = 0; i < rowsOrderedByAttribute.Count - 1; i++)
            {
                numBelowThreshold++;
                numLabelBelowThreshold[rowsOrderedByAttribute[i].Values[targetAttribute]]++;

                numAboveThreshold--;
                numLabelAboveThreshold[rowsOrderedByAttribute[i].Values[targetAttribute]]--;
                
                /* Since adjacent rows can have the same attribute value,
                 * but not the same target attribute value, we want to wait
                 * until we have reached the last row that contains that
                 * same attribute value before attempting to calculate a threshold.
                 */
                if (rowsOrderedByAttribute[i].Values[attribute] != rowsOrderedByAttribute[i + 1].Values[attribute])
                {
                    /*
                     * While commenting out the line below may seem incredibly inefficient,
                     * since it means we consider every change in attribute value, not every
                     * change in TARGET attribute value, it only adds 200 milliseconds to the
                     * adult dataset, while increasing the accuracy.
                     * 
                     * The increase in accuracy occurs because there is a small number of
                     * instances where this traversal will have passed a change in attribute value, but
                     * not a change in TARGET attribute value without realizing it. The implementation of
                     * a fix would mean keeping separate counting dictionaries in memory for the most recent
                     * attribute value before the current one, which just adds extra mess to the code.
                     * 
                     * In this way, the implementation for handling continuous variables is slightly different from Mitchell's
                     */
                    //if (rowsOrderedByAttribute[i].Values[targetAttribute] != rowsOrderedByAttribute[i + 1].Values[targetAttribute] || doesSameAttributeValueHaveDifferentTargetAttributeValues)
                    {
                        doesSameAttributeValueHaveDifferentTargetAttributeValues = false;

                        double currentThreshold = (rowsOrderedByAttribute[i].Values[attribute] + rowsOrderedByAttribute[i + 1].Values[attribute]) / 2;

                        double gain = entropyForAllRows;

                        gain -= (double)numBelowThreshold / numRows * Entropy(numLabelBelowThreshold.Values.ToList());
                        gain -= (double)numAboveThreshold / numRows * Entropy(numLabelAboveThreshold.Values.ToList());

                        //Console.WriteLine("Gain\t{0}\t{1}\t{2}", attribute.Name, currentThreshold, gain);

                        if (gain > highestGain)
                        {
                            highestGain = gain;
                            thresholdWithHighestGain = currentThreshold;
                        }
                    }
                }
                else if (rowsOrderedByAttribute[i].Values[targetAttribute] != rowsOrderedByAttribute[i + 1].Values[targetAttribute])
                {
                    doesSameAttributeValueHaveDifferentTargetAttributeValues = true;
                }
            }

            return new Tuple<double, double>(highestGain, thresholdWithHighestGain);
        }

        private double Entropy(List<int> labelCounts)
        {
            double entropy = 0;
            int numCounts = labelCounts.Sum();

            foreach (int labelCount in labelCounts)
            {
                double ratio = (double)labelCount / numCounts;
                if (ratio != 0)
                {
                    entropy -= ratio * Math.Log(ratio, 2.0);
                }
            }

            return entropy;
        }

        private void GrowBranches(List<Row> rows, List<Attribute> attributes, Attribute targetAttribute)
        {
            for (int i = 0; i < chosenAttribute.Values.Count; i++)
            {
                double chosenAttributeValue = (double)i;

                List<Row> childRows = rows.Where(r => GetDiscretizedAttributeValue(r.Values[chosenAttribute]) == chosenAttributeValue).ToList();

                if (childRows.Count == 0)
                {
                    double mostCommonTargetAttributeValue = GetMostCommonTargetAttributeValue(rows, targetAttribute);

                    Id3Node child = new Id3Node(mostCommonTargetAttributeValue);
                    branches[chosenAttributeValue] = child;
                }
                else
                {
                    List<Attribute> childAttributes = new List<Attribute>(attributes);
                    if (chosenAttribute.IsDiscrete || chosenAttribute.NumUsesInTree >= MAX_CONT_ATT_USES_PER_TREE)
                    {
                        childAttributes.Remove(chosenAttribute);
                    }
                    else
                    {
                        chosenAttribute.NumUsesInTree++;
                    }

                    for (int j = 0; j < depth; j++) Console.Write(" ");
                    Console.Write(" {0}->", chosenAttribute.Values[(int)chosenAttributeValue]);

                    Id3Node child = new Id3Node(childRows, childAttributes, targetAttribute, depth + 1);
                    branches[chosenAttributeValue] = child;
                }
            }
        }

        private double GetMostCommonTargetAttributeValue(List<Row> rows, Attribute targetAttribute)
        {
            double mostCommonTargetAttributeValue = rows.GroupBy(r => r.Values[targetAttribute])
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .First();

            return mostCommonTargetAttributeValue;
        }

        private double GetDiscretizedAttributeValue(double attributeValue)
        {
            double discretizedAttributeValue;

            if (chosenAttribute.IsDiscrete)
            {
                discretizedAttributeValue = attributeValue;
            }
            else
            {
                if (attributeValue <= chosenAttributeThreshold)
                {
                    discretizedAttributeValue = 0.0;
                }
                else
                {
                    discretizedAttributeValue = 1.0;
                }
            }

            return discretizedAttributeValue;
        }
    }
}
