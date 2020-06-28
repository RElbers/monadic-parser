using System;
using MonadicParser.Examples.Expressions;
using MonadicParser.Parsing;
using static MonadicParser.Parsing.Parser;

// ReSharper disable ConvertToLocalFunction

namespace MonadicParser.Examples
{
    public static class ExpressionParser
    {
        private static Parser<double, string> Double()
            => new Parser<double, string>(state =>
            {
                var token = state.Consume();
                var ok = double.TryParse(token, out var n);
                if (!ok)
                    return new UnexpectedToken<(double, State<string>), string>("a double", token);
                return (n, state);
            });

        private static Parser<IExpression<double>, string> BinOp(string opSymbol, Func<IExpression<double>, IExpression<double>, IExpression<double>> ctor) =>
            from _1 in Symbol("(")
            from lhs in Lazy(InfixParser)
            from _0 in Symbol(opSymbol)
            from rhs in Lazy(InfixParser)
            from _2 in Symbol(")")
            select ctor(lhs, rhs);

        public static Parser<IExpression<double>, string> InfixParser()
        {
            var add = BinOp("+", (x, y) => new Add<double>(x, y));
            var sub = BinOp("-", (x, y) => new Sub<double>(x, y));
            var mul = BinOp("*", (x, y) => new Mul<double>(x, y));

            var @var =
                from id in Next<string>()
                select (IExpression<double>) new Var<double>(id);


            var @const =
                from d in Double()
                select (IExpression<double>) new Constant<double>(d);


            return add + sub + mul + @const + @var;
        }
    }
}