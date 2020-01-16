using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Options;
using Newtonsoft.Json;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser.Query
{
    public class QueryExecutor
    {
        public static int Run(QueryParameters parameters)
        {
            // parse the scans string
            HashSet<int> scanIds = ParseScanIds(parameters.scans);
            parameters.scanNumbers = scanIds;
            
            ProxiSpectrumReader reader = new ProxiSpectrumReader(parameters);
            List<PROXISpectrum> results = reader.Retrieve();

            if (parameters.stdout)
            {
                StdOutputQueryData(results);
            }
            else 
            {
                string outputFileName;
                // if outputFile has been defined, put output there.
                if (parameters.outputFile != null)
                {
                    outputFileName = Path.GetFullPath(parameters.outputFile);
                }
                // otherwise put output files into the same directory as the raw file input
                else
                {
                    outputFileName = Path.GetFullPath(parameters.rawFilePath);
                }
                string directory = Path.GetDirectoryName(outputFileName);
                outputFileName = Path.Combine(directory ?? throw new NoNullAllowedException(), Path.GetFileNameWithoutExtension(outputFileName) + ".JSON");
                OutputQueryData(results, outputFileName);
            }
            return 0;
        }
        
        
        public static void OutputQueryData(List<PROXISpectrum> outputData, string outputFileName)
        {
            string outputString = JsonConvert.SerializeObject(outputData);
            File.WriteAllText(outputFileName, outputString);
        }
        

        public static void StdOutputQueryData(List<PROXISpectrum> outputData)
        {
            string outputString = JsonConvert.SerializeObject(outputData);
            Console.Write(outputString);
        }
        
        
        

        private static HashSet<int> ParseScanIds(string text)
        {
            if (text.Length == 0) throw new OptionException("Scan ID string invalid, nothing specified", null);
            foreach (char c in text)
            {
                int ic = c;
                if (!((ic == ',') || (ic == '-') || (ic == ' ') || ('0' <= ic && ic <= '9')))
                {
                    throw new OptionException("Scan ID string contains invalid character", null);
                }
            }

            string[] tokens = text.Split(new[] {','}, StringSplitOptions.None);

            HashSet<int> container = new HashSet<int>();

            for (int i = 0; i < tokens.Length; ++i)
            {
                if (tokens[i].Length == 0) throw new OptionException("Scan ID string has invalid format", null);
                string[] rangeBoundaries = tokens[i].Split(new[] {'-'}, StringSplitOptions.None);
                if (rangeBoundaries.Length == 1)
                {
                    int rangeStart;
                    try
                    {
                        rangeStart = Convert.ToInt32(rangeBoundaries[0]);
                    }
                    catch (Exception)
                    {
                        throw new OptionException("Scan ID string has invalid format", null);
                    }

                    container.Add(rangeStart);
                }
                else if (rangeBoundaries.Length == 2)
                {
                    int rangeStart;
                    int rangeEnd;
                    try
                    {
                        rangeStart = Convert.ToInt32(rangeBoundaries[0]);
                        rangeEnd = Convert.ToInt32(rangeBoundaries[1]);
                    }
                    catch (Exception)
                    {
                        throw new OptionException("Scan ID string has invalid format", null);
                    }

                    for (int l = rangeStart; l <= rangeEnd; ++l)
                    {
                        container.Add(l);
                    }
                }
                else throw new OptionException("Scan ID string has invalid format", null);
            }

            return container;
        }
    }
}
