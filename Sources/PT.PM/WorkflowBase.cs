﻿using PT.PM.AntlrUtils;
using PT.PM.UstPreprocessing;
using PT.PM.Common;
using PT.PM.Common.CodeRepository;
using PT.PM.Matching;
using PT.PM.Patterns;
using PT.PM.Patterns.PatternsRepository;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

namespace PT.PM
{
    public abstract class WorkflowBase<StageType, ResultType, PatternsDataStructure> : ILoggable
        where StageType : struct, IConvertible
        where ResultType : WorkflowResultBase<StageType>
        where PatternsDataStructure : CommonPatternsDataStructure
    {
        protected ILogger logger = DummyLogger.Instance;
        protected int maxStackSize;
        private int maxTimespan;
        private int memoryConsumptionMb;

        protected Language[] languages;

        protected StageHelper<StageType> stageHelper;

        public StageType Stage { get; set; }

        public ISourceCodeRepository SourceCodeRepository { get; set; }

        public IPatternsRepository PatternsRepository { get; set; }

        public Dictionary<Language, ParserConverterSet> ParserConverterSets { get; set; } = new Dictionary<Language, ParserConverterSet>();

        public IPatternConverter<PatternsDataStructure> PatternConverter { get; set; }

        public IUstPatternMatcher<PatternsDataStructure> UstPatternMatcher { get; set; }

        public IUstPreprocessor UstPreprocessor { get; set; } = new UstPreprocessor();

        public LanguageDetector LanguageDetector { get; set; } = new ParserLanguageDetector();

        public bool IsIncludeIntermediateResult { get; set; }

        public ILogger Logger
        {
            get { return logger; }
            set
            {
                logger = value;
                if (SourceCodeRepository != null)
                {
                    SourceCodeRepository.Logger = Logger;
                }
                foreach (var languageParser in ParserConverterSets)
                {
                    if (languageParser.Value.Parser != null)
                    {
                        languageParser.Value.Parser.Logger = logger;
                    }
                    if (languageParser.Value.Converter != null)
                    {
                        languageParser.Value.Converter.Logger = logger;
                    }
                }
                if (PatternsRepository != null)
                {
                    PatternsRepository.Logger = logger;
                }
                if (PatternConverter != null)
                {
                    PatternConverter.Logger = logger;
                }
                if (UstPreprocessor != null)
                {
                    UstPreprocessor.Logger = Logger;
                }
                if (UstPatternMatcher != null)
                {
                    UstPatternMatcher.Logger = logger;
                }
                if (LanguageDetector != null)
                {
                    LanguageDetector.Logger = logger;
                }

                if (logger != null)
                {
                    logger.SourceCodeRepository = SourceCodeRepository;
                }
            }
        }

        public int ThreadCount { get; set; }

        public int MaxStackSize
        {
            get
            {
                return maxStackSize;
            }
            set
            {
                maxStackSize = value;
                foreach (var languageParser in ParserConverterSets)
                {
                    var antlrParser = languageParser.Value.Parser as AntlrParser;
                    if (antlrParser != null)
                    {
                        antlrParser.MaxStackSize = maxStackSize;
                    }
                }
            }
        }

        public Language[] Languages
        {
            get
            {
                if (languages == null)
                {
                    languages = ParserConverterSets.Keys.Select(key => key).ToArray();
                }
                return languages;
            }
        }

        public abstract ResultType Process();

        public WorkflowBase(StageType stage)
        {
            Stage = stage;
            stageHelper = new StageHelper<StageType>(stage);
        }

        protected ParseTree ReadAndParse(string fileName, ResultType workflowResult)
        {
            ParseTree result = null;
            var stopwatch = new Stopwatch();
            string file = fileName;
            if (stageHelper.IsContainsRead)
            {
                stopwatch.Restart();
                SourceCodeFile sourceCodeFile = SourceCodeRepository.ReadFile(fileName);
                stopwatch.Stop();

                Logger.LogInfo("File {0} has been read (Elapsed: {1}).", fileName, stopwatch.Elapsed.ToString());

                workflowResult.AddProcessedCharsCount(sourceCodeFile.Code.Length);
                workflowResult.AddProcessedLinesCount(TextHelper.GetLinesCount(sourceCodeFile.Code));
                workflowResult.AddReadTime(stopwatch.ElapsedTicks);
                workflowResult.AddResultEntity(sourceCodeFile);

                file = sourceCodeFile.RelativePath;
                if (stageHelper.IsContainsParse)
                {
                    stopwatch.Restart();
                    Language? detectedLanguage = LanguageDetector.DetectIfRequired(sourceCodeFile.Name, sourceCodeFile.Code, Languages);
                    if (detectedLanguage == null)
                    {
                        Logger.LogInfo($"Input languages set is empty or {sourceCodeFile.Name} language has not been detected");
                        return result;
                    }
                    result = ParserConverterSets[(Language)detectedLanguage].Parser.Parse(sourceCodeFile);
                    stopwatch.Stop();
                    Logger.LogInfo("File {0} has been parsed (Elapsed: {1}).", fileName, stopwatch.Elapsed.ToString());
                    workflowResult.AddParseTime(stopwatch.ElapsedTicks);
                }

                var antlrParseTree = result as AntlrParseTree;
                if (antlrParseTree != null)
                {
                    workflowResult.AddLexerTime(antlrParseTree.LexerTimeSpan.Ticks);
                    workflowResult.AddParserTicks(antlrParseTree.ParserTimeSpan.Ticks);
                }
            }
            return result;
        }
    }
}
