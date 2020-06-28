using MonadicParser.Examples.Expressions;
using MonadicParser.Parsing;
using static MonadicParser.Parsing.Parser;

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
                    return new UnexpectedToken<(double, State<string>), string>("double", token);
                return (n, state);
            });

        public static Parser<IExpression<double>, string> GetExpressionParser()
        {
            var add =
                from _1 in Symbol("(")
                from lhs in Lazy(GetExpressionParser)
                from _0 in Symbol("+")
                from rhs in Lazy(GetExpressionParser)
                from _2 in Symbol(")")
                select (IExpression<double>) new Add<double>(lhs, rhs);

            var sub =
                from _1 in Symbol("(")
                from lhs in Lazy(GetExpressionParser)
                from _0 in Symbol("-")
                from rhs in Lazy(GetExpressionParser)
                from _2 in Symbol(")")
                select (IExpression<double>) new Sub<double>(lhs, rhs);

            var mul =
                from _1 in Symbol("(")
                from lhs in Lazy(GetExpressionParser)
                from _0 in Symbol("*")
                from rhs in Lazy(GetExpressionParser)
                from _2 in Symbol(")")
                select (IExpression<double>) new Mul<double>(lhs, rhs);

            var @var =
                from id in Next<string>()
                where id != "+" &&
                      id != "-" &&
                      id != "*" &&
                      id != "(" &&
                      id != ")"
                select (IExpression<double>) new Var<double>(id);

            var @const =
                from d in Double()
                select (IExpression<double>) new Constant<double>(d);

            return @const + @var + add + sub + mul;
        }
    }
}