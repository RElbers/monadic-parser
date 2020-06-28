using System;
using System.Collections.Generic;

namespace MonadicParser.Parsing
{
    public abstract class Result<T>
    {
        public abstract bool HasValue { get; }
        public static implicit operator Result<T>(T value) => new Ok<T>(value);
    }

    public static class Result
    {
        #region Linq

        private static Result<T1> Flatten<T1>(this Result<Result<T1>> result) =>
            result switch
            {
                Ok<Result<T1>> ok => ok.Value,
                Fail<Result<T1>> fail => fail.As<T1>(),
                _ => throw new ArgumentOutOfRangeException(nameof(result))
            };

        public static Result<T2> Select<T1, T2>(this Result<T1> result, Func<T1, T2> map) =>
            result switch
            {
                Fail<T1> fail => fail.As<T2>(),
                Ok<T1> ok => new Ok<T2>(map(ok.Value)),
                _ => throw new ArgumentOutOfRangeException(nameof(result))
            };


        private static Result<T3> SelectManyOk<T1, T2, T3>(Ok<T1> ok, Func<T1, Result<T2>> then, Func<T1, T2, T3> map)
        {
            var flat = Flatten(ok.Select(then));
            Result<T3> ret = flat switch
            {
                Ok<T2> ok2 => new Ok<T3>(map(ok.Value, ok2.Value)),
                Fail<T2> fail => fail.As<T3>(),
                _ => throw new ArgumentOutOfRangeException(nameof(flat))
            };
            return ret;
        }

        public static Result<T3> SelectMany<T1, T2, T3>(this Result<T1> result, Func<T1, Result<T2>> then, Func<T1, T2, T3> map) =>
            result switch
            {
                Ok<T1> ok => SelectManyOk(ok, then, map),
                Fail<T1> fail => fail.As<T3>(),
                _ => throw new ArgumentOutOfRangeException()
            };

        public static Result<T1> Where<T1>(this Result<T1> result, Func<T1, bool> predicate) =>
            result switch
            {
                Fail<T1> fail => fail,
                Ok<T1> ok => predicate(ok.Value)
                    ? (Result<T1>) ok
                    : (Result<T1>) new AssertionFailed<T1>(),
                _ => throw new ArgumentOutOfRangeException(nameof(result))
            };

        #endregion
    }

    public class Ok<T> : Result<T>
    {
        public override bool HasValue => true;
        public T Value { get; }

        public Ok(T value)
        {
            Value = value;
        }

        public override string ToString() => $"Ok({Value})";

        protected bool Equals(Ok<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var ok = obj as Ok<T>;
            return ok != null && (Value == null && ok.Value == null ||
                                  Value != null && Value.Equals(ok.Value));
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public static bool operator ==(Ok<T> left, Ok<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Ok<T> left, Ok<T> right)
        {
            return !Equals(left, right);
        }
    }

    public abstract class Fail<T> : Result<T>
    {
        public override bool HasValue => false;
        public abstract Fail<T2> As<T2>();
    }

    // ReSharper disable once InconsistentNaming
    public class UnexpectedToken<T, Token> : Fail<T>
    {
        public Token Expected { get; }
        public Token Actual { get; }

        public UnexpectedToken(Token expected, Token actual)
        {
            Expected = expected;
            Actual = actual;
        }

        public override string ToString() => "Unexpected token\n" +
                                             "  Expected:\n" +
                                             $"    {Expected}\n" +
                                             "  But got:\n" +
                                             $"    {Actual}\n";

        public override Fail<T2> As<T2>() => new UnexpectedToken<T2, Token>(Expected, Actual);
    }

    public class AssertionFailed<T> : Fail<T>
    {
        public override bool HasValue => false;
        public override string ToString() => "Assertion failed during parsing.";

        public override Fail<T2> As<T2>() => new AssertionFailed<T2>();
    }
}