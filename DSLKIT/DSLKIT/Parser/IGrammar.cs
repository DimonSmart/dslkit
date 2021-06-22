﻿using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using System.Collections.Generic;
using DSLKIT.Parser.ExtendedGrammar;

namespace DSLKIT.Parser
{
    public interface IGrammar
    {
        string Name { get; }
        INonTerminal Root { get; }
        IReadOnlyCollection<ITerminal> Terminals { get; }
        IReadOnlyCollection<INonTerminal> NonTerminals { get; }
        IReadOnlyCollection<Production> Productions { get; }
        IReadOnlyCollection<ExProduction> ExProductions { get; }
        IReadOnlyCollection<RuleSet> RuleSets { get; }
        IReadOnlyDictionary<IExNonTerminal, IList<ITerm>> Firsts { get; }
        IReadOnlyDictionary<IExNonTerminal, IList<ITerm>> Follows { get; }
        TranslationTable TranslationTable { get; }
        ActionAndGotoTable ActionAndGotoTable { get; }
        IEofTerminal Eof { get; }
    }
}