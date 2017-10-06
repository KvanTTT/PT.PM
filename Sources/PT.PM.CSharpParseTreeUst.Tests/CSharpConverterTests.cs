﻿using System.Linq;
using PT.PM.Common;
using PT.PM.TestUtils;
using NUnit.Framework;
using PT.PM.Common.Nodes.GeneralScope;

namespace PT.PM.CSharpParseTreeUst.Tests
{
    [TestFixture]
    public class CSharpConverterTests
    {
        [TestCase("AllInOne.cs")]
        [TestCase("ConvertError.cs")]
        public void Convert_CSharp_WithoutErrors(string fileName)
        {
            TestUtility.CheckFile(fileName, CSharp.Language, Stage.Ust);
        }

        [Test]
        public void Convert_WebGoatNet_WithoutException()
        {
            string projectKey = "WebGoat.NET-1c6cab";
            TestUtility.CheckProject(
                TestProjects.CSharpProjects.Single(p => p.Key == projectKey), CSharp.Language, Stage.Ust);
        }

        [TestCase("AllInOne.cs")]
        public void Convert_CSharp_BaseTypesExist(string fileName)
        {
            var workflowResults = TestUtility.CheckFile(fileName, CSharp.Language, Stage.Ust);
            var ust = workflowResults.Usts.First();
            bool result = ust.AnyDescendant(descendant =>
            {
                return descendant is TypeDeclaration typeDeclaration &&
                       typeDeclaration.BaseTypes.Any(type => type.TypeText == "IDisposable");
            });
            Assert.IsTrue(result, "Ust doesn't contain type declaration node with IDisposable base type");
        }
    }
}
