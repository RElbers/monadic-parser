using System;
using System.Collections.Generic;
using System.Linq;
using MonadicParser.Examples.Evaluation;
using MonadicParser.Examples.Expressions;
using MonadicParser.Parsing;

namespace MonadicParser.Examples
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var input = args.Length == 0
                ? "(7*((x+5)*(x+5)))"
                : string.Join(" ", args);

            Console.WriteLine($"Parsing: {input}");

            // Tokenize
            var tokens =
                from t in new[] {input}
                from t1 in t.Split(' ')
                from t2 in t1.SplitAndKeep('(')
                from t3 in t2.SplitAndKeep(')')
                from t4 in t3.SplitAndKeep('+')
                from t5 in t4.SplitAndKeep('*')
                from t6 in t5.SplitAndKeep('-')
                from t7 in t6.SplitAndKeep('^')
                select t7;


            var tokenArray = tokens.ToArray();
            Console.WriteLine($"Input was tokenized to: [{string.Join(", ", tokenArray)}]");
            
            var parser = ExpressionParser.InfixParser();
            var result = parser.Run(tokenArray);

            // Evaluate expression
            switch (result)
            {
                case Ok<IExpression<double>> ok:
                    var printer = new PrettyPrint();
                    var str = printer.Eval(ok.Value);
                    Console.WriteLine($"Parsed as: {str}");
                    
                    var toDual = new EvaluateDual();
                    var dual = toDual.Eval(ok.Value);
                    Console.WriteLine($"Evaluated to: {dual.Value} for x=1.");
                    Console.WriteLine($"With derivative: {dual.Deriv} for x=1.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }
    }

    public static class StringExt
    {
        /// <summary>
        /// Help function for splitting strings and keeping the delimiter.
        /// Used for tokenizing the input. 
        /// </summary>
        public static IEnumerable<string> SplitAndKeep(this string s, params char[] delims)
        {
            int start = 0, index;

            while ((index = s.IndexOfAny(delims, start)) != -1)
            {
                if (index - start > 0)
                    yield return s.Substring(start, index - start);
                yield return s.Substring(index, 1);
                start = index + 1;
            }

            if (start < s.Length)
            {
                yield return s.Substring(start);
            }
        }
    }
}