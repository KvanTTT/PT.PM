﻿using Esprima;
using Esprima.Ast;
using PT.PM.Common;
using PT.PM.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PT.PM.JavaScriptParseTreeUst
{
    public class JavaScriptEsprimaParser : ILanguageParser
    {
        public ILogger Logger { get; set; } = DummyLogger.Instance;

        public JavaScriptType JavaScriptType { get; set; } = JavaScriptType.Undefined;

        public Language Language => JavaScript.Language;

        public bool IsActive => true;

        public int Offset { get; set; }

        public CodeFile OrigFile { get; set; }

        public ParseTree Parse(CodeFile sourceCodeFile)
        {
            if (sourceCodeFile.Code == null)
            {
                return null;
            }

            var errorHandler = new JavaScriptEsprimaErrorHandler(sourceCodeFile)
            {
                Logger = Logger,
                Offset = Offset,
                OrigFile = OrigFile
            };
            try
            {
                var parserOptions = new ParserOptions(sourceCodeFile.FullName)
                {
                    Range = true,
                    Comment = true,
                    Tolerant = true,
                    ErrorHandler = errorHandler
                };
                var parser = new Esprima.JavaScriptParser(sourceCodeFile.Code, parserOptions);

                var stopwatch = Stopwatch.StartNew();
                Program ast = parser.ParseProgram(JavaScriptType == JavaScriptType.Strict);
                stopwatch.Stop();
                TimeSpan parserTimeSpan = stopwatch.Elapsed;

                var scanner = new Scanner(sourceCodeFile.Code, parserOptions);
                errorHandler.Scanner = scanner;
                errorHandler.Logger = DummyLogger.Instance; // Ignore errors on tokenization because of the first stage
                var comments = new List<Comment>();

                stopwatch.Restart();
                Token token = null;
                do
                {
                    comments.AddRange(scanner.ScanComments());
                    try
                    {
                        token = scanner.Lex();
                    }
                    catch (InvalidOperationException ex)
                    {
                        // TODO: handle the end of the stream without exception
                        Logger.LogError(new ParsingException(sourceCodeFile, ex));
                        break;
                    }
                }
                while (token.Type != TokenType.EOF);
                stopwatch.Stop();
                TimeSpan lexerTimeSpan = stopwatch.Elapsed;

                // TODO: comments handling without scanner, https://github.com/sebastienros/esprima-dotnet/issues/39
                var result = new JavaScriptEsprimaParseTree(ast, comments)
                {
                    LexerTimeSpan = lexerTimeSpan,
                    ParserTimeSpan = parserTimeSpan
                };
                result.SourceCodeFile = sourceCodeFile;

                return result;
            }
            catch (Exception ex) when (!(ex is ThreadAbortException))
            {
                ParsingException exception = ex is ParserException parserException
                    ? errorHandler.CreateException(parserException.Index, parserException.Message)
                    : ex is ParsingException parsingException
                    ? parsingException
                    : new ParsingException(sourceCodeFile, ex);
                Logger.LogError(exception);
                return null;
            }
        }
    }
}
