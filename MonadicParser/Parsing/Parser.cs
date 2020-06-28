using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace MonadicParser.Parsing
{
    public class Parser<T, Token>
    {
        private readonly Func<State<Token>, Result<(T, State<Token>)>> _parse;

        /// <summary>
        /// Constructor for parsers.
        /// </summary>
        public Parser(Func<State<Token>, Result<(T, State<Token>)>> parse)
        {
            _parse = parse;
        }

        /// <summary>
        /// Applies the parser to the tokens. 
        /// </summary>
        public Result<(T, State<Token>)> Parse(State<Token> state) => _parse(state);

        /// <summary>
        /// Applies the parser to the tokens. Returns just the result and 'forgets' the remaining state. 
        /// </summary>
        public Result<T> Run(IList<Token> tokens)
        {
            var state = new State<Token>(tokens);
            var result = Parse(state);

            return result switch
            {
                Fail<(T, State<Token>)> fail => fail.As<T>(),
                Ok<(T, State<Token>)> ok => ok.Value.Item1,
                _ => throw new ArgumentOutOfRangeException(nameof(result))
            };
        }

        public static Parser<T, Token> operator +(Parser<T, Token> l, Parser<T, Token> r) => l.Or(r);
    }

    /// <summary>
    ///    Helper class to bind the Token parameter. 
    /// </summary>
    public static class BindToken<Token>
    {
        /// <summary>
        ///    Helper function to lift a value to a parser with only 1 type parameter. 
        /// </summary>
        public static Parser<T1, Token> Lift<T1>(T1 a)
        {
            return Parser.Lift<T1, Token>(a);
        }
    }

    public static class Parser
    {
        #region Linq

        /*
         * Extension methods to enable Linq syntax.
         */

        /// <summary>
        ///    Apply a function to a <c>Parser</c> to change the value of a successful parse. 
        /// </summary>
        public static Parser<T2, Token> Select<T1, T2, Token>(this Parser<T1, Token> parser, Func<T1, T2> map)
            => new Parser<T2, Token>(state1 =>
                from x in parser.Parse(state1)
                select (map(x.Item1), x.Item2));


        /// <summary>
        ///    Perform a sequence of parsers and apply a function to the final result. 
        /// </summary>
        public static Parser<T3, Token> SelectMany<T1, T2, T3, Token>(this Parser<T1, Token> parser, Func<T1, Parser<T2, Token>> then, Func<T1, T2, T3> map)
            => new Parser<T3, Token>(state1 =>
                from x in parser.Parse(state1)
                from y in then(x.Item1).Parse(x.Item2)
                select (map(x.Item1, y.Item1), y.Item2));


        /// <summary>
        ///    Filter the results via a predicate.
        ///    Succeeds only if the predicate applied to the result of the <c>Parser</c> evaluates to true. 
        /// </summary>
        public static Parser<T1, Token> Where<T1, Token>(this Parser<T1, Token> parser, Func<T1, bool> predicate)
            => new Parser<T1, Token>(state1 =>
                from x in parser.Parse(state1)
                where predicate(x.Item1)
                select x);

        #endregion

        #region static

        /// <summary>
        ///   Parser combinator for sums.
        ///   Returns the result of the first parser if it succeeds, otherwise returns the result of the second parser.  
        /// </summary>
        public static Parser<T1, Token> Or<T1, Token>(this Parser<T1, Token> parserL, Parser<T1, Token> parserR)
            => new Parser<T1, Token>(state =>
            {
                var resultL = parserL.Parse(state);
                if (resultL.HasValue)
                    return resultL;

                var resultR = parserR.Parse(state);
                return resultR;
            });

        /// <summary>
        ///    Helper function to lift a value to a <c>Parser</c>. 
        /// </summary>
        public static Parser<T1, Token> Lift<T1, Token>(T1 thing) => new Parser<T1, Token>(state => (thing, state));

        /// <summary>
        ///   Yields the next token.  
        /// </summary>
        public static Parser<Token, Token> Next<Token>()
            => new Parser<Token, Token>(state =>
            {
                var token = state.Consume();
                return (token, state);
            });
        
        /// <summary>
        ///    Parser for a specific token.
        /// </summary>
        public static Parser<Token, Token> Symbol<Token>(Token expected)
            => new Parser<Token, Token>(state =>
            {
                var token = state.Consume();
                if (token != null && token.Equals(expected))
                    return (token, state);
                return new UnexpectedToken<(Token, State<Token>), Token>(expected, token);
            });

        /// <summary>
        ///    Helper function needed to create lazy recursive parsers.
        /// </summary>
        public static Parser<T1, Token> Lazy<T1, Token>(Func<Parser<T1, Token>> f)
            => new Parser<T1, Token>(state => f().Parse(state));

        /// <summary>
        ///    Parser combinator to wrap another <c>Parser</c> with tokens.
        /// </summary>
        public static Parser<T1, Token> Wrap<T1, Token>(Token before, Parser<T1, Token> parser, Token after)
            =>
                from _0 in Symbol(before)
                from _1 in parser
                from _2 in Symbol(after)
                select _1;


        /// <summary>
        ///    Parser combinator for lists.
        ///    Succeeds if there are 0 or more results.
        /// </summary>
        public static Parser<IList<T1>, Token> Any<T1, Token>(Parser<T1, Token> parser) => AtLeast(0, parser);

        /// <summary>
        ///    Parser combinator for lists.
        ///    Succeeds if there are 1 or more results.
        /// </summary>
        public static Parser<IList<T1>, Token> Some<T1, Token>(Parser<T1, Token> parser) => AtLeast(1, parser);

        /// <summary>
        ///    Parser combinator for lists.
        ///    Fails if there are less then <c>n</c> results.
        /// </summary>
        public static Parser<IList<T1>, Token> AtLeast<T1, Token>(int n, Parser<T1, Token> parser)
            => new Parser<IList<T1>, Token>(state =>
            {
                var results = new List<T1>();
                while (true)
                {
                    var result = parser.Parse(state);
                    switch (result)
                    {
                        case Fail<(T1, State<Token>)> fail:
                        {
                            if (results.Count < n)
                                // Fail if we parsed too little
                                return fail.As<(IList<T1>, State<Token>)>();

                            // Succeed if we parsed enough
                            return (results, state);
                        }

                        case Ok<(T1, State<Token>)> ok:
                            var (x, s) = ok.Value;
                            results.Add(x);
                            state = s;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(result));
                    }
                }
            });

        /// <summary>
        ///    Parser combinator for lists.
        ///    Fails if there are more then <c>n</c> results.
        /// </summary>
        public static Parser<IList<T1>, Token> AtMost<T1, Token>(int n, Parser<T1, Token> parser)
            => new Parser<IList<T1>, Token>(state =>
            {
                var results = new List<T1>();
                while (true)
                {
                    var result = parser.Parse(state);
                    switch (result)
                    {
                        case Fail<(T1, State<Token>)> fail:
                        {
                            if (results.Count > n)
                                // Fail if we parsed too much
                                return fail.As<(IList<T1>, State<Token>)>();

                            // Succeed if we parsed enough
                            return (results, state);
                        }

                        case Ok<(T1, State<Token>)> ok:
                            var (x, s) = ok.Value;
                            results.Add(x);
                            state = s;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(result));
                    }
                }
            });


        /// <summary>
        ///    Parser combinator for optionals.
        ///    Either the inner <c>Parser</c> succeeds and the corresponding result is returned or it fails and <c>(null, state)</c> is returned.
        /// </summary>
        public static Parser<T1?, Token> Maybe<T1, Token>(Parser<T1, Token> parser) where T1 : class
            => new Parser<T1?, Token>(state =>
            {
                var x = parser.Parse(state);

                if (x.HasValue)
                    return x!;
                return (null, state);
            });

        #endregion


        #region Debug

        /// <summary>
        ///    Parser combinator which prints a <c>String</c> if the parser succeeds.
        /// </summary>
        public static Parser<T1, Token> Log<T1, Token>(this Parser<T1, Token> parser, string message) => Do(parser, () => Console.WriteLine(message));


        /// <summary>
        ///    Parser combinator which calls an <c>Action</c> if the parser succeeds.
        /// </summary>
        public static Parser<T1, Token> Do<T1, Token>(this Parser<T1, Token> parser, Action function)
            => new Parser<T1, Token>(state =>
            {
                var result = parser.Parse(state);
                function();
                return result;
            });

        #endregion
    }
}