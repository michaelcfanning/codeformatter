﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using CommandLine;

namespace CodeFormatter
{
    [Verb("format", HelpText = "Apply code formatting rules and analyzers to specified project.")]
    public class FormatOptions
    {
        [Option(
            't', "target",
            HelpText = "Project, solution or response file to drive code formatting.",
            Required = true)]
        public IEnumerable<string> FormatTargets { get; set; }

        [Option(
            "fileFilter",
            HelpText = "Only apply changes to files with specified name(s).",
            Separator = ',')]
        public IEnumerable<string> Files { get; set; }

        [Option(
            'l', "lang",
            HelpText = "Specifies the language to use when a response file is specified, e.g., 'C#', 'Visual Basic', ... (default: 'C#').")]
        public string Language { get; set; }

        [Option(
            'c', "config",
            HelpText = "Comma-separated list of preprocessor configurations the formatter should run under.",
            Separator = ',')]
        public IEnumerable<string> PreprocessorConfigurations { get; set; }

        [Option(
            "copyright",
            HelpText = "Specifies file containing copyright header.")]
        public string CopyrightHeader { get; set; }

        [Option(
            "enable",
            HelpText = "Comma-separated list of rules to enable",
            Separator = ',')]
        public IEnumerable<string> EnabledRules { get; set; }

        [Option(
            "disable",
            HelpText = "Comma-separated list of rules to disable",
            Separator = ',')]
        public IEnumerable<string> DisabledRules { get; set; }

        [Option(
            'v', "verbose",
            HelpText = "Verbose output.")]
        public bool Verbose { get; set; }

        [Option(
            "definedotnetformatter",
            HelpText = "Define DOTNET_FORMATTER in order to allow #if !DOTNET_FORMATTER constructs in code (to opt out of reformatting).")]
        public bool DefineDotNetFormatter { get; set; }

        [Option(
            "useanalyzers",
            HelpText = "TEMPORARY: invoke built-in analyzers rather than rules to perform reformatting.")]
        public bool UseAnalyzers { get; set; }

        private ImmutableDictionary<string, bool> _ruleMap;
        public ImmutableDictionary<string, bool> RuleMap
        {
            get
            {
                return _ruleMap ?? BuildRuleMapFromOptions();
            }
        }

        private ImmutableDictionary<string, bool> BuildRuleMapFromOptions()
        {
            _ruleMap = ImmutableDictionary<string, bool>.Empty;
            if (EnabledRules != null)
            {
                foreach (string rule in EnabledRules)
                {
                    _ruleMap = _ruleMap.SetItem(rule, true);
                }
            }

            if (DisabledRules != null)
            {
                foreach (string rule in DisabledRules)
                {
                    _ruleMap = _ruleMap.SetItem(rule, false);
                }
            }
            return _ruleMap;
        }
    }
}
