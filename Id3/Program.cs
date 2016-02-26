/*
 * Only a few things have changed algorithmically since you last saw the code:
 * 1) Using doubles instead of strings for values in memory.
 * 2) The ContinuousGain function in the ID3Node class (further documentation can be found there).
 * 3) Continuous attributes can now be used up to specified number of times in a tree
 *    before being removed from consideration. If I allowed an unlimited number of uses,
 *    I eventually received a StackOverflow error.
 *    
 * To use different datasets, you can change the value of the 'dataset' variable below
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Id3
{
    class Program
    {
        static List<Attribute> attributes;
        static List<Row> rows;
        static Attribute targetAttribute;

        static List<Row> testRows;

        static void Main(string[] args)
        {
            string datasetPath = @"..\..\..\..\datasets\";
            string dataset = "adult"; // e.g. iris, adult, tennis

            Stopwatch timer = new Stopwatch();
            timer.Start();

            ReadFromSetupFile(String.Format(@"{0}{1}\setup.txt", datasetPath, dataset));
            ReadFromTrainFile(String.Format(@"{0}{1}\train.txt", datasetPath, dataset));

            Id3Node tree = new Id3Node(rows, attributes, targetAttribute, 0);

            ReadFromTestFile(String.Format(@"{0}{1}\test.txt", datasetPath, dataset));

            Score(tree, testRows);

            timer.Stop();
            Console.WriteLine("Time elapsed: {0}.{1}", timer.Elapsed.Seconds, timer.Elapsed.Milliseconds);
            
            Console.ReadLine();
        }

        static void ReadFromSetupFile(string filePath)
        {
            attributes = new List<Attribute>();
            string line;

            // Read the file and display it line by line.
            StreamReader file = new System.IO.StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {
                string[] attributeData = line.Split(',');

                string attributeName = attributeData[0];
                int columnNumber = int.Parse(attributeData[1]);
                bool isTargetAttribute = attributeData[2] != "0";
                bool isDiscrete = attributeData[3] != "continuous";
                List<string> attributeValues = new List<string>();

                if (isDiscrete)
                {
                    for (int i = 3; i < attributeData.Length; i++)
                    {
                        attributeValues.Add(attributeData[i]);
                    }
                }

                if (isTargetAttribute)
                {
                    targetAttribute = new Attribute(attributeName, columnNumber, isDiscrete, attributeValues);
                }
                else
                {
                    Attribute attribute = new Attribute(attributeName, columnNumber, isDiscrete, attributeValues);
                    attributes.Add(attribute);
                }
            }

            file.Close();
        }

        static void ReadFromTrainFile(string filePath)
        {
            rows = new List<Row>();
            string line;

            // Read the file and display it line by line.
            StreamReader file = new System.IO.StreamReader(filePath);
            while (!String.IsNullOrWhiteSpace(line = file.ReadLine()))
            {
                string[] rowData = line.Split(',');
                Dictionary<Attribute, double> rowValues = new Dictionary<Attribute, double>();

                foreach (Attribute attribute in attributes)
                {
                    string rowValue = rowData[attribute.ColumnNumber];
                    if (attribute.IsDiscrete)
                    {
                        rowValues[attribute] = (double)attribute.Values.IndexOf(rowValue);
                    }
                    else
                    {
                        rowValues[attribute] = double.Parse(rowValue);
                    }
                }

                string targetAttributeValue = rowData[targetAttribute.ColumnNumber];
                rowValues[targetAttribute] = (double)targetAttribute.Values.IndexOf(targetAttributeValue);

                Row row = new Row(rowValues);
                rows.Add(row);
            }

            file.Close();
        }

        static void ReadFromTestFile(string filePath)
        {
            testRows = new List<Row>();
            string line;

            // Read the file and display it line by line.
            StreamReader file = new System.IO.StreamReader(filePath);
            while (!String.IsNullOrWhiteSpace(line = file.ReadLine()))
            {
                bool hasQuestionMark = false;
                string[] rowData = line.Split(',');
                Dictionary<Attribute, double> rowValues = new Dictionary<Attribute, double>();

                foreach (Attribute attribute in attributes)
                {
                    string rowValue = rowData[attribute.ColumnNumber];
                    if (rowValue == "?") hasQuestionMark = true;
                    if (attribute.IsDiscrete)
                    {
                        rowValues[attribute] = (double)attribute.Values.IndexOf(rowValue);
                    }
                    else
                    {
                        rowValues[attribute] = double.Parse(rowValue);
                    }
                }

                string targetAttributeValue = rowData[targetAttribute.ColumnNumber];
                rowValues[targetAttribute] = (double)targetAttribute.Values.IndexOf(targetAttributeValue);

                Row row = new Row(rowValues);
                if (!hasQuestionMark)testRows.Add(row);
            }

            file.Close();
        }

        static void Score(Id3Node tree, List<Row> testRows)
        {
            int numRows = testRows.Count;
            int numCorrect = 0;

            foreach (Row row in testRows)
            {
                double assignedLabel = tree.Classify(row);

                if (assignedLabel == row.Values[targetAttribute])
                {
                    numCorrect++;
                }
            }

            double percentCorrect = (double)numCorrect / numRows * 100;

            Console.WriteLine("\n{0:00.000}%: {1} out of {2}", percentCorrect, numCorrect, numRows);
        }
    }
}
