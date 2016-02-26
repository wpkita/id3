using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Id3
{
    internal class Program
    {
        private static List<Attribute> _attributes;
        private static List<Row> _rows;
        private static Attribute _targetAttribute;

        private static List<Row> _testRows;

        private static void Main(string[] args)
        {
            var timer = new Stopwatch();
            timer.Start();

            ReadFromSetupFile("datasets/setup.txt");
            ReadFromTrainFile("datasets/train.txt");

            var tree = new Id3Node(_rows, _attributes, _targetAttribute, 0);

            ReadFromTestFile("datasets/test.txt");

            Score(tree, _testRows);

            timer.Stop();
            Console.WriteLine("Time elapsed: {0}.{1}", timer.Elapsed.Seconds, timer.Elapsed.Milliseconds);

            Console.ReadLine();
        }

        private static void ReadFromSetupFile(string filePath)
        {
            _attributes = new List<Attribute>();
            string line;

            // Read the file and display it line by line.
            var file = new StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {
                var attributeData = line.Split(',');

                var attributeName = attributeData[0];
                var columnNumber = int.Parse(attributeData[1]);
                var isTargetAttribute = attributeData[2] != "0";
                var isDiscrete = attributeData[3] != "continuous";
                var attributeValues = new List<string>();

                if (isDiscrete)
                {
                    for (var i = 3; i < attributeData.Length; i++)
                    {
                        attributeValues.Add(attributeData[i]);
                    }
                }

                if (isTargetAttribute)
                {
                    _targetAttribute = new Attribute(attributeName, columnNumber, isDiscrete, attributeValues);
                }
                else
                {
                    var attribute = new Attribute(attributeName, columnNumber, isDiscrete, attributeValues);
                    _attributes.Add(attribute);
                }
            }

            file.Close();
        }

        private static void ReadFromTrainFile(string filePath)
        {
            _rows = new List<Row>();
            string line;

            // Read the file and display it line by line.
            var file = new StreamReader(filePath);
            while (!string.IsNullOrWhiteSpace(line = file.ReadLine()))
            {
                var rowData = line.Split(',');
                var rowValues = new Dictionary<Attribute, double>();

                foreach (var attribute in _attributes)
                {
                    var rowValue = rowData[attribute.ColumnNumber];
                    if (attribute.IsDiscrete)
                    {
                        rowValues[attribute] = attribute.Values.IndexOf(rowValue);
                    }
                    else
                    {
                        rowValues[attribute] = double.Parse(rowValue);
                    }
                }

                var targetAttributeValue = rowData[_targetAttribute.ColumnNumber];
                rowValues[_targetAttribute] = _targetAttribute.Values.IndexOf(targetAttributeValue);

                var row = new Row(rowValues);
                _rows.Add(row);
            }

            file.Close();
        }

        private static void ReadFromTestFile(string filePath)
        {
            _testRows = new List<Row>();
            string line;

            // Read the file and display it line by line.
            var file = new StreamReader(filePath);
            while (!string.IsNullOrWhiteSpace(line = file.ReadLine()))
            {
                var hasQuestionMark = false;
                var rowData = line.Split(',');
                var rowValues = new Dictionary<Attribute, double>();

                foreach (var attribute in _attributes)
                {
                    var rowValue = rowData[attribute.ColumnNumber];
                    if (rowValue == "?") hasQuestionMark = true;
                    if (attribute.IsDiscrete)
                    {
                        rowValues[attribute] = attribute.Values.IndexOf(rowValue);
                    }
                    else
                    {
                        rowValues[attribute] = double.Parse(rowValue);
                    }
                }

                var targetAttributeValue = rowData[_targetAttribute.ColumnNumber];
                rowValues[_targetAttribute] = _targetAttribute.Values.IndexOf(targetAttributeValue);

                var row = new Row(rowValues);
                if (!hasQuestionMark) _testRows.Add(row);
            }

            file.Close();
        }

        private static void Score(Id3Node tree, List<Row> testRows)
        {
            var numRows = testRows.Count;
            var numCorrect = 0;

            foreach (var row in testRows)
            {
                var assignedLabel = tree.Classify(row);

                if (assignedLabel == row.Values[_targetAttribute])
                {
                    numCorrect++;
                }
            }

            var percentCorrect = (double) numCorrect/numRows*100;

            Console.WriteLine("\n{0:00.000}%: {1} out of {2}", percentCorrect, numCorrect, numRows);
        }
    }
}
