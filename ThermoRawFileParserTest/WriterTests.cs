﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.PeerToPeer.Collaboration;
using System.Xml.Serialization;
using IO.Mgf;
using MzLibUtil;
using NUnit.Framework;
using ThermoRawFileParser;
using ThermoRawFileParser.Writer;
using ThermoRawFileParser.Writer.MzML;
using UsefulProteomicsDatabases;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class WriterTests
    {
        [Test]
        public void TestExtensionsNull()
        {
            // Get temp path for writing the test files
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFilePath);

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small.RAW");
            var parseInput = new ParseInput();
            parseInput.RawFilePath = testRawFile;
            parseInput.OutputDirectory = tempFilePath;

            //empty filename
            parseInput.OutputFormat = OutputFormat.MGF;
            RawFileParser.Parse(parseInput);
            Assert.IsTrue(File.Exists(Path.Combine(tempFilePath, "small.mgf")));

            parseInput.OutputFormat = OutputFormat.MzML;
            RawFileParser.Parse(parseInput);
            Assert.IsTrue(File.Exists(Path.Combine(tempFilePath, "small.mzML")));
            File.Delete(Path.Combine(tempFilePath, "small.mzML"));

            parseInput.OutputFormat = OutputFormat.IndexMzML;
            RawFileParser.Parse(parseInput);
            Assert.IsTrue(File.Exists(Path.Combine(tempFilePath, "small.mzML")));

            parseInput.Gzip = true;
            RawFileParser.Parse(parseInput);
            Assert.IsTrue(File.Exists(Path.Combine(tempFilePath, "small.mzML.gz")));
            File.Delete(Path.Combine(tempFilePath, "small.mzML.gz"));

            parseInput.OutputFormat = OutputFormat.MGF;
            RawFileParser.Parse(parseInput);
            Assert.IsTrue(File.Exists(Path.Combine(tempFilePath, "small.mgf.gz")));

            parseInput.OutputFormat = OutputFormat.MzML;
            RawFileParser.Parse(parseInput);
            Assert.IsTrue(File.Exists(Path.Combine(tempFilePath, "small.mzML.gz")));

            Directory.Delete(tempFilePath, true);
        }

        [Test]
        public void TestExtensionsFull()
        {
            // Get temp path for writing the test files
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFilePath);

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small.RAW");
            var parseInput = new ParseInput();
            parseInput.RawFilePath = testRawFile;

            List<OutputFormat> formats = new List<OutputFormat>();
            string userInput;
            string expectedOutput;

            foreach (string line in File.ReadLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/ExtensionTest.tsv")).Skip(1))
            {
                string[] words = line.Split('\t');

                switch (words[0].ToLower())
                {
                    case "mgf": formats = new List<OutputFormat> { OutputFormat.MGF }; break;
                    case "mzml": formats = new List<OutputFormat> { OutputFormat.MzML, OutputFormat.IndexMzML }; break;
                }

                switch (words[1].ToLower())
                {
                    case "true": parseInput.Gzip = true; break;
                    default: parseInput.Gzip = false; break;
                }

                userInput = words[2];
                expectedOutput = words[3];

                parseInput.OutputFile = Path.Combine(tempFilePath, userInput);

                foreach (var format in formats)
                {
                    parseInput.OutputFormat = format;
                    RawFileParser.Parse(parseInput);
                    Assert.IsTrue(File.Exists(Path.Combine(tempFilePath, expectedOutput)));
                    File.Delete(Path.Combine(tempFilePath, expectedOutput));
                }

            }

            Directory.Delete(tempFilePath, true);
        }

        [Test]
        public void TestMgf()
        {
            // Get temp path for writing the test MGF
            var tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small.RAW");
            var parseInput = new ParseInput(testRawFile, null, tempFilePath, OutputFormat.MGF);

            RawFileParser.Parse(parseInput);

            var mgfData = Mgf.LoadAllStaticData(Path.Combine(tempFilePath, "small.mgf"));
            Assert.AreEqual(34, mgfData.NumSpectra);
        }

        [Test]
        public void TestFolderMgfs()
        {
            // Get temp path for writing the test MGF
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            Directory.CreateDirectory(tempFilePath);

            var testRawFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/TestFolderMgfs");
            var parseInput = new ParseInput(null, testRawFolder, tempFilePath, OutputFormat.MGF);

            RawFileParser.Parse(parseInput);

            var numFiles = Directory.GetFiles(tempFilePath, "*.mgf");
            Assert.AreEqual(numFiles.Length, 2);

            var mgfData = Mgf.LoadAllStaticData(Path.Combine(tempFilePath, "small1.mgf"));
            Assert.AreEqual(34, mgfData.NumSpectra);

            var mgfData2 = Mgf.LoadAllStaticData(Path.Combine(tempFilePath, "small2.mgf"));
            Assert.AreEqual(34, mgfData2.NumSpectra);

            Directory.Delete(tempFilePath, true);
        }

        [Test]
        public void TestMzml()
        {
            // Get temp path for writing the test mzML
            var tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small.RAW");
            var parseInput = new ParseInput(testRawFile, null, tempFilePath, OutputFormat.MzML);

            RawFileParser.Parse(parseInput);

            // Deserialize the mzML file
            var xmlSerializer = new XmlSerializer(typeof(mzMLType));
            var testMzMl = (mzMLType) xmlSerializer.Deserialize(new FileStream(
                Path.Combine(tempFilePath, "small.mzML"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Assert.AreEqual("48", testMzMl.run.spectrumList.count);
            Assert.AreEqual(48, testMzMl.run.spectrumList.spectrum.Length);

            Assert.AreEqual("1", testMzMl.run.chromatogramList.count);
            Assert.AreEqual(1, testMzMl.run.chromatogramList.chromatogram.Length);

            Assert.AreEqual(48, testMzMl.run.chromatogramList.chromatogram[0].defaultArrayLength);
        }

        [Test]
        public void TestProfileMzml()
        {
            // Get temp path for writing the test mzML
            var tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small.RAW");
            var parseInput = new ParseInput(testRawFile, null, tempFilePath, OutputFormat.MzML);

            parseInput.NoPeakPicking = new HashSet<int> { 1, 2 };

            RawFileParser.Parse(parseInput);

            // Deserialize the mzML file
            var xmlSerializer = new XmlSerializer(typeof(mzMLType));
            var testMzMl = (mzMLType)xmlSerializer.Deserialize(new FileStream(
                Path.Combine(tempFilePath, "small.mzML"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Assert.AreEqual("48", testMzMl.run.spectrumList.count);
            Assert.AreEqual(48, testMzMl.run.spectrumList.spectrum.Length);

            Assert.AreEqual("1", testMzMl.run.chromatogramList.count);
            Assert.AreEqual(1, testMzMl.run.chromatogramList.chromatogram.Length);

            Assert.AreEqual(48, testMzMl.run.chromatogramList.chromatogram[0].defaultArrayLength);
        }

        [Test]
        public void TestMSLevels()
        {
            // Get temp path for writing the test mzML
            var tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small.RAW");
            var parseInput = new ParseInput(testRawFile, null, tempFilePath, OutputFormat.MzML);

            parseInput.MsLevel = new HashSet<int> { 1 };

            RawFileParser.Parse(parseInput);

            // Deserialize the mzML file
            var xmlSerializer = new XmlSerializer(typeof(mzMLType));
            var testMzMl = (mzMLType)xmlSerializer.Deserialize(new FileStream(
                Path.Combine(tempFilePath, "small.mzML"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Assert.AreEqual("14", testMzMl.run.spectrumList.count);
            Assert.AreEqual(14, testMzMl.run.spectrumList.spectrum.Length);

            Assert.AreEqual(48, testMzMl.run.chromatogramList.chromatogram[0].defaultArrayLength);
        }

        [Test]
        public void TestIndexedMzML()
        {
            // Get temp path for writing the test mzML
            var tempFilePath = Path.GetTempPath();

            Console.WriteLine(tempFilePath);

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small.RAW");
            var parseInput = new ParseInput(testRawFile, null, tempFilePath, OutputFormat.IndexMzML);

            RawFileParser.Parse(parseInput);

            // Deserialize the mzML file
            var xmlSerializer = new XmlSerializer(typeof(indexedmzML));
            var testMzMl = (indexedmzML) xmlSerializer.Deserialize(new FileStream(
                Path.Combine(tempFilePath, "small.mzML"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Assert.AreEqual("48", testMzMl.mzML.run.spectrumList.count);
            Assert.AreEqual(48, testMzMl.mzML.run.spectrumList.spectrum.Length);

            Assert.AreEqual("1", testMzMl.mzML.run.chromatogramList.count);
            Assert.AreEqual(1, testMzMl.mzML.run.chromatogramList.chromatogram.Length);

            Assert.AreEqual(2, testMzMl.indexList.index.Length);
            Assert.AreEqual("spectrum", testMzMl.indexList.index[0].name.ToString());
            Assert.AreEqual(48, testMzMl.indexList.index[0].offset.Length);
            Assert.AreEqual("chromatogram", testMzMl.indexList.index[1].name.ToString());
            Assert.AreEqual(1, testMzMl.indexList.index[1].offset.Length);
        }

        [Test]
        public void TestMzML_MS2()
        {
            // Get temp path for writing the test mzML
            var tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small2.RAW");
            var parseInput = new ParseInput(testRawFile, null, tempFilePath, OutputFormat.MzML);

            RawFileParser.Parse(parseInput);

            // Deserialize the mzML file
            var xmlSerializer = new XmlSerializer(typeof(mzMLType));
            var testMzMl = (mzMLType)xmlSerializer.Deserialize(new FileStream(
                Path.Combine(tempFilePath, "small2.mzML"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Assert.AreEqual(95, testMzMl.run.spectrumList.spectrum.Length);

            var precursor = testMzMl.run.spectrumList.spectrum[16].precursorList.precursor[0].selectedIonList.selectedIon[0];
            var selectedMz = Double.Parse(precursor.cvParam.Where(cv => cv.accession == "MS:1000744").First().value);
            Assert.IsTrue(selectedMz - 604.7592 < 0.001);

            var selectedZ = int.Parse(precursor.cvParam.Where(cv => cv.accession == "MS:1000041").First().value);
            Assert.AreEqual(selectedZ , 2);

            //var selectedI = Double.Parse(precursor.cvParam.Where(cv => cv.accession == "MS:1000042").First().value);
            //Assert.IsTrue(selectedI - 10073 < 1);

            Assert.AreEqual(95, testMzMl.run.chromatogramList.chromatogram[0].defaultArrayLength);
        }
    }
}