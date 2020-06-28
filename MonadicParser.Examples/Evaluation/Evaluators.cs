using System.Globalization;
using MonadicParser.Examples.Expressions;

namespace MonadicParser.Examples.Evaluation
{
    public class EvaluateDual : Algebra<double, Dual>
    {

        protected override Dual Eval(Var<double> var) => new Dual(1, 1);
        protected override Dual Eval(Constant<double> constant) => new Dual(constant.Value, 0);
        protected override Dual Eval(Add<double> expr) => Eval(expr.Left) + Eval(expr.Right);
        protected override Dual Eval(Sub<double> expr) => Eval(expr.Left) - Eval(expr.Right);
        protected override Dual Eval(Mul<double> expr) => Eval(expr.Left) * Eval(expr.Right);
    }

    public class PrettyPrint : Algebra<double, string>
    {
        protected override string Eval(Var<double> var) => var.Id;
        protected override string Eval(Constant<double> constant) => constant.Value.ToString(CultureInfo.InvariantCulture);
        protected override string Eval(Add<double> expr) => $"({Eval(expr.Left)} + {Eval(expr.Right)})";
        protected override string Eval(Sub<double> expr) => $"({Eval(expr.Left)} - {Eval(expr.Right)})";
        protected override string Eval(Mul<double> expr)
        {
            var l = Eval(expr.Left);
            var r = Eval(expr.Right);
            if (l.Equals(r))
            {
                return $"{l}^2";
            }
            return $"({Eval(expr.Left)} * {Eval(expr.Right)})";
        }
    }
}