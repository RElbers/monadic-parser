using System;
using MonadicParser.Examples.Expressions;

namespace MonadicParser.Examples.Evaluation
{
    public abstract class Algebra<T, TReturn>
    {
        public TReturn Eval(IExpression<T> expression)
        {
            var x = expression switch
            {
                Add<T> add => Eval(add),
                Constant<T> constant => Eval(constant),
                Mul<T> mul => Eval(mul),
                Sub<T> sub => Eval(sub),
                Var<T> @var => Eval(@var),
                null => throw new NullReferenceException(nameof(expression)),
                _ => throw new ArgumentOutOfRangeException(nameof(expression))
            };

            return x;
        }

        protected abstract TReturn Eval(Var<T> expr);
        protected abstract TReturn Eval(Sub<T> expr);
        protected abstract TReturn Eval(Constant<T> expr);
        protected abstract TReturn Eval(Mul<T> expr);
        protected abstract TReturn Eval(Add<T> expr);
    }
}