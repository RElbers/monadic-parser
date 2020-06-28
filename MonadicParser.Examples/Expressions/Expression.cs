namespace MonadicParser.Examples.Expressions
{
    // ReSharper disable once UnusedTypeParameter
    public interface IExpression<T>
    {
    }


    public class Add<T> : IExpression<T>
    {
        public IExpression<T> Left { get; }
        public IExpression<T> Right { get; }

        public Add(IExpression<T> left, IExpression<T> right)
        {
            Left = left;
            Right = right;
        }

        public override string ToString() => $"add ( {Left} {Right} )";
    }

    public class Constant<T> : IExpression<T>
    {
        public T Value { get; }

        public Constant(T value)
        {
            Value = value;
        }

        public override string ToString() => $"{Value}";
    }

    public class Mul<T> : IExpression<T>
    {
        public IExpression<T> Left { get; }
        public IExpression<T> Right { get; }

        public Mul(IExpression<T> left, IExpression<T> right)
        {
            Left = left;
            Right = right;
        }

        public override string ToString() => $"mul ( {Left} {Right} )";
    }

    public class Sub<T> : IExpression<T>
    {
        public IExpression<T> Left { get; }
        public IExpression<T> Right { get; }

        public Sub(IExpression<T> left, IExpression<T> right)
        {
            Left = left;
            Right = right;
        }

        public override string ToString() => $"sub ( {Left} {Right} )";
    }

    public class Var<T> : IExpression<T>
    {
        public string Id { get; }

        public Var(string id)
        {
            Id = id;
        }

        public override string ToString() => Id;
    }
}