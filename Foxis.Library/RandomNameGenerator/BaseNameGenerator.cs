using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Foxis.Library.RandomNameGenerator
{
    /// <summary>
    /// Taken from https://github.com/m4bwav/DotNetRandomNameGenerator
    /// </summary>
    public abstract class BaseNameGenerator
    {
        private const string ResourcePathPrefix = "Foxis.Library.Resources.";
        protected readonly Random RandGen;

        protected BaseNameGenerator()
        {
            RandGen = new Random();
        }

        protected BaseNameGenerator(Random randGen)
        {
            RandGen = randGen;
        }

        private static Stream ReadResourceStreamForFileName(string resourceFileName)
        {
            var resourceName = ResourcePathPrefix + resourceFileName;
            var stream = typeof(BaseNameGenerator).GetTypeInfo().Assembly
                .GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new Exception($"Resource {resourceName} doesn't exist!");
            }
            return stream;
        }

        protected static string[] ReadResourceByLine(string resourceFileName)
        {
            var stream = ReadResourceStreamForFileName(resourceFileName);

            var list = new List<string>();

            var streamReader = new StreamReader(stream);
            string? str;

            while ((str = streamReader.ReadLine()) != null)
            {
                if (str != string.Empty)
                    list.Add(str);
            }

            return list.ToArray();
        }
    }
}