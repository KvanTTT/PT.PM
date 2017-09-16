﻿using PT.PM.Common;
using PT.PM.Common.Exceptions;
using PT.PM.Common.Nodes;
using PT.PM.TestUtils;
using PT.PM.Patterns;
using PT.PM.Patterns.Nodes;
using PT.PM.Patterns.PatternsRepository;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace PT.PM.Dsl.Tests
{
    [TestFixture]
    public class DslParseTests
    {
        private DefaultPatternRepository patternsRepository;
        private PatternRootNode[] patterns;

        [SetUp]
        public void Init()
        {
            patternsRepository = new DefaultPatternRepository();
            var patternsConverter = new PatternConverter(new JsonUstNodeSerializer(typeof(UstNode), typeof(PatternVarDef)));
            patterns = patternsConverter.Convert(patternsRepository.GetAll());
        }

        [TestCase(@"HardcodedPassword.ptpm")]
        [TestCase(@"InsecureTransport.ptpm")]
        [TestCase(@"InsecureRandomness.ptpm")]
        [TestCase(@"WeakCryptographicHash.ptpm")]
        [TestCase(@"AndroidPermissionCheck.ptpm")]
        [TestCase(@"MissingBroadcasterPermission.ptpm")]
        [TestCase(@"CookieNotSentOverSSL.ptpm")]
        [TestCase(@"CookieSecurityOverlyBroadDomain.ptpm")]
        [TestCase(@"PasswordInComment.ptpm")]
        [TestCase(@"InadequateRSAPadding.ptpm")]
        [TestCase(@"DebugInfo.ptpm")]
        [TestCase(@"XmlExternalEntity.ptpm")]
        [TestCase(@"AndroidHostnameVerificationDisabled.ptpm")]
        [TestCase(@"KeyManagementNullEncryptionKey.ptpm")]
        [TestCase(@"AttributesCodeInsideElementEvent.ptpm")]
        [TestCase(@"ExtendingSecurityManagerWithoutFinal.ptpm")]
        [TestCase(@"ImproperValidationEmptyMethod.ptpm")]
        [TestCase(@"UsingCloneWithoutCloneable.ptpm")]
        [TestCase(@"PoorLoggingPractice.ptpm")]
        public void Process_Dsl_EqualsToHardcoded(string fileName)
        {
            var data = File.ReadAllText(Path.Combine(TestHelper.TestsDataPath, fileName));
            var logger = new LoggerMessageCounter();
            var processor = new DslProcessor() { Logger = logger, PatternExpressionInsideStatement = false };
            PatternRootNode result = (PatternRootNode)processor.Deserialize(data);
            result.Languages = fileName == @"DebugInfo.ptpm"
                ? new HashSet<Language>() { Language.Php }
                : new HashSet<Language>(LanguageExt.AllPatternLanguages);
            Assert.AreEqual(0, logger.ErrorCount);

            string patternName = Path.GetFileNameWithoutExtension(fileName);
            PatternRootNode defaultPattern = patterns.FirstOrDefault(p => p.DebugInfo.StartsWith(patternName));
            if (defaultPattern == null)
            {
                Assert.Inconclusive($"Pattern {patternName} does not exists in DefaultPatternRepository");
            }

            Assert.IsTrue(result.Equals(defaultPattern));
        }

        [TestCase(@"Range.ptpm")]
        public void Parse_Dsl_WithoutErrors(string fileName)
        {
            var data = File.ReadAllText(Path.Combine(TestHelper.TestsDataPath, fileName));
            var logger = new LoggerMessageCounter();
            var processor = new DslProcessor() { Logger = logger };
            UstNode result = processor.Deserialize(data);
            Assert.AreEqual(0, logger.ErrorCount);
        }

        [Test]
        public void ProcessDsl_SampleWithSyntaxError_HandleErrors()
        {
            var logger = new LoggerMessageCounter();
            var data = "(?i)password(?-i)]> = <[\"\\w*\" || null]>";
            var processor = new DslProcessor() { Logger = logger };
            UstNode result = processor.Deserialize(data);
            Assert.AreEqual(5, logger.ErrorCount);
        }

        [TestCase("<[@pwd:password]> = #; test_call(<[@pwd:^pass]>);")]
        [TestCase("<[@pwd]> = #; test_call(<[@pwd:password]>);")]
        public void ProcessDsl_PatternVarAlreadyDefined_HandleErrors(string data)
        {
            Assert.Throws(typeof(ConversionException), () =>
            {
                var processor = new DslProcessor();
                UstNode result = processor.Deserialize(data);
            });
        }
    }
}
