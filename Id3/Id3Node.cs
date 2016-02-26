using System;
using System.Collections.Generic;
using System.Linq;

namespace Id3
{
    public class Id3Node
    {
        private const int MaxContAttUsesPerTree = 15;
        private static int _nodeCount;
        private readonly Dictionary<double, Id3Node> _branches = new Dictionary<double, Id3Node>();
        private Attribute _chosenAttribute;
        private double _chosenAttributeThreshold;
        private readonly int _depth;
        private readonly bool _isLeaf;
        private readonly double _leafLabel;

        public Id3Node(double leafLabel)
        {
            _isLeaf = true;
            this._leafLabel = leafLabel;
        }

        public Id3Node(List<Row> rows, List<Attribute> attributes, Attribute targetAttribute, int depth)
        {
            this._depth = depth;

            Console.WriteLine("{0} to node {1}", rows.Count, _nodeCount++);

            var areAllTargetAttributeValuesTheSame = rows.Select(r => r.Values[targetAttribute])
                .Distinct()
                .Count() == 1;
            if (areAllTargetAttributeValuesTheSame)
            {
                _isLeaf = true;
                _leafLabel = rows.First().Values[targetAttribute];
            }
            else if (attributes.Count == 0)
            {
                _isLeaf = true;
                _leafLabel = GetMostCommonTargetAttributeValue(rows, targetAttribute);
            }
            else
            {
                ChooseAttribute(rows, attributes, targetAttribute);
                GrowBranches(rows, attributes, targetAttribute);
            }
        }

        public double Classify(Row row)
        {
            var assignedLabel = _isLeaf
                ? _leafLabel
                : _branches[GetDiscretizedAttributeValue(row.Values[_chosenAttribute])].Classify(row);

            return assignedLabel;
        }

        private void ChooseAttribute(List<Row> rows, IEnumerable<Attribute> attributes, Attribute targetAttribute)
        {
            var attributeGains = new Dictionary<Attribute, double>();
            var attributeThresholds = new Dictionary<Attribute, double>();

            var entropyForAllRows = Entropy(rows.GroupBy(r => r.Values[targetAttribute]).Select(g => g.Count()).ToList());

            //for (int i = 0; i < depth; i++) Console.Write(" ");
            //Console.WriteLine("ENTROPY:\t{0}", entropyForAllRows);

            foreach (var attribute in attributes)
            {
                if (attribute.IsDiscrete)
                {
                    attributeGains[attribute] = DiscreteGain(rows, attribute, targetAttribute, entropyForAllRows);
                }
                else
                {
                    var continuousGainInfo = ContinuousGain(rows, attribute, targetAttribute, entropyForAllRows);
                    attributeGains[attribute] = continuousGainInfo.Item1;
                    attributeThresholds[attribute] = continuousGainInfo.Item2;
                }
            }

            var highestGain = attributeGains.Values.Max();
            _chosenAttribute = attributeGains.First(ag => ag.Value == highestGain).Key;

            for (var i = 0; i < _depth; i++) Console.Write(" ");
            Console.WriteLine(":Chose {0}", _chosenAttribute.Name);

            if (!_chosenAttribute.IsDiscrete)
            {
                _chosenAttributeThreshold = attributeThresholds[_chosenAttribute];
            }
        }

        private double DiscreteGain(List<Row> rows, Attribute attribute, Attribute targetAttribute,
            double entropyForAllRows)
        {
            var numberOfRows = rows.Count;
            var gain = entropyForAllRows;

            for (var i = 0; i < attribute.Values.Count; i++)
            {
                double attributeValue = i;
                var rowsWithAttributeValue = rows.Where(r => r.Values[attribute] == attributeValue);
                var numberOfRowsWithAttributeValue = rowsWithAttributeValue.Count();
                gain -= (double) numberOfRowsWithAttributeValue/numberOfRows*
                        Entropy(
                            rowsWithAttributeValue.GroupBy(r => r.Values[targetAttribute])
                                .Select(g => g.Count())
                                .ToList());
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

        private static Tuple<double, double> ContinuousGain(ICollection<Row> rows, Attribute attribute,
            Attribute targetAttribute,
            double entropyForAllRows)
        {
            var numRows = rows.Count;

            var numBelowThreshold = 0;
            var numLabelBelowThreshold = new Dictionary<double, int>();

            var numAboveThreshold = 0;
            var numLabelAboveThreshold = new Dictionary<double, int>();

            for (var i = 0; i < targetAttribute.Values.Count; i++)
            {
                numLabelBelowThreshold[i] = 0;
                numLabelAboveThreshold[i] = 0;
            }

            var highestGain = double.MinValue;
            double thresholdWithHighestGain = 0;

            var rowsOrderedByAttribute = rows.OrderBy(r => r.Values[attribute]).ToList();

            foreach (var t in rowsOrderedByAttribute)
            {
                numAboveThreshold++;
                numLabelAboveThreshold[t.Values[targetAttribute]]++;
            }

            for (var i = 0; i < rowsOrderedByAttribute.Count - 1; i++)
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
                        var currentThreshold = (rowsOrderedByAttribute[i].Values[attribute] +
                                                rowsOrderedByAttribute[i + 1].Values[attribute])/2;

                        var gain = entropyForAllRows;

                        gain -= (double) numBelowThreshold/numRows*Entropy(numLabelBelowThreshold.Values.ToList());
                        gain -= (double) numAboveThreshold/numRows*Entropy(numLabelAboveThreshold.Values.ToList());

                        //Console.WriteLine("Gain\t{0}\t{1}\t{2}", attribute.Name, currentThreshold, gain);

                        if (gain > highestGain)
                        {
                            highestGain = gain;
                            thresholdWithHighestGain = currentThreshold;
                        }
                    }
                }
                else if (rowsOrderedByAttribute[i].Values[targetAttribute] !=
                         rowsOrderedByAttribute[i + 1].Values[targetAttribute])
                {
                }
            }

            return new Tuple<double, double>(highestGain, thresholdWithHighestGain);
        }

        private static double Entropy(List<int> labelCounts)
        {
            double entropy = 0;
            var numCounts = labelCounts.Sum();

            foreach (var labelCount in labelCounts)
            {
                var ratio = (double) labelCount/numCounts;
                if (ratio != 0)
                {
                    entropy -= ratio*Math.Log(ratio, 2.0);
                }
            }

            return entropy;
        }

        private void GrowBranches(List<Row> rows, List<Attribute> attributes, Attribute targetAttribute)
        {
            for (var i = 0; i < _chosenAttribute.Values.Count; i++)
            {
                double chosenAttributeValue = i;

                var childRows =
                    rows.Where(r => GetDiscretizedAttributeValue(r.Values[_chosenAttribute]) == chosenAttributeValue)
                        .ToList();

                if (childRows.Count == 0)
                {
                    var mostCommonTargetAttributeValue = GetMostCommonTargetAttributeValue(rows, targetAttribute);

                    var child = new Id3Node(mostCommonTargetAttributeValue);
                    _branches[chosenAttributeValue] = child;
                }
                else
                {
                    var childAttributes = new List<Attribute>(attributes);
                    if (_chosenAttribute.IsDiscrete || _chosenAttribute.NumUsesInTree >= MaxContAttUsesPerTree)
                    {
                        childAttributes.Remove(_chosenAttribute);
                    }
                    else
                    {
                        _chosenAttribute.NumUsesInTree++;
                    }

                    for (var j = 0; j < _depth; j++) Console.Write(" ");
                    Console.Write(" {0}->", _chosenAttribute.Values[(int) chosenAttributeValue]);

                    var child = new Id3Node(childRows, childAttributes, targetAttribute, _depth + 1);
                    _branches[chosenAttributeValue] = child;
                }
            }
        }

        private static double GetMostCommonTargetAttributeValue(IEnumerable<Row> rows, Attribute targetAttribute)
        {
            var mostCommonTargetAttributeValue = rows.GroupBy(r => r.Values[targetAttribute])
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .First();

            return mostCommonTargetAttributeValue;
        }

        private double GetDiscretizedAttributeValue(double attributeValue)
        {
            double discretizedAttributeValue;

            if (_chosenAttribute.IsDiscrete)
            {
                discretizedAttributeValue = attributeValue;
            }
            else
            {
                discretizedAttributeValue = attributeValue <= _chosenAttributeThreshold ? 0.0 : 1.0;
            }

            return discretizedAttributeValue;
        }
    }
}
