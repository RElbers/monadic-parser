# Monadic parsing using LINQ in C#.
Language-Integrated Query (LINQ) is a language feature of C# which enables one to generate SQL statements in C# by writing LINQ statements. LINQ can be extended to work for user defined types by implementing several methods, namely `Select` and `SelectMany`. `Select` is equivalent to the `map : (T1 -> T2) -> F<T1> -> F<T2>` operator for the functor `F`.

```csharp
public static F<T2> Select<T1, T2>(this F<T1> x, Func<T1, T2> map);
```

`SelectMany` is equivalent to `bind : F<T1> -> (T1 -> F<T2>) -> F<T2>` followed by another function applied to the results `T1` and `T2`. 
```csharp
 public static F<T3> SelectMany<T1, T2, T3>(this F<T1> x, Func<T1, F<T2>> then, Func<T1, T2, T3> map);
```

With a representation of `map` and `bind`, LINQ provides a convenient syntax for dealing with monads. Specifically, we can use LINQ to define monadic parsers.  

## `Result<T>`
First, we define a class which represents the result of a parsing. Instead of relying on nullable types, using a dedicated class allows us to add more information when parsing fails. For example, when an unexpected token is encountered or when a parsing assertion fails.  The `Result<T>` class has 2 implementations, `Ok<T>` which contains a `T Value` attribute and `Fail<T>` which does not. Indeed, the `Result<T>` class also forms a monad and it comes with definitions for `Select`, `SelectMany` and `Where`, which can be used with LINQ expressions.


```csharp 
public abstract class Result<T> 
{ 
        public abstract bool HasValue { get; }
        public static implicit operator Result<T>(T value) => new Ok<T>(value);
}

public class Ok<T> : Result<T> 
{ 
        public override bool HasValue => true;
        public T Value { get; }

        public Ok(T value)
        {
            Value = value;
        }
}

public abstract class Fail<T> : Result<T> 
{
        public override bool HasValue => false;
        public abstract Fail<T2> As<T2>();
}
```

## `State<T>`
Then, we have struct representing the current state of a parser, State<T>. It contains the list of tokens and an index pointing to the current token. It comes with a method for consuming a token, which returns the current token and increments the index. It is a struct because we need call-by-value semantics for the index.

```csharp
public struct State<T>
{
    private IList<T> Tokens { get; }
    private int Idx { get; set; }

    public State(IList<T> tokens)
    {
        Tokens = tokens;
        Idx = 0;
    }

    public T Consume()
    {
        return Tokens[Idx++];
    }
}
```

## `Parser<T, Token>`
Finally, there is the  actual `Parser<T, Token>` class, which has 2 generic arguments. The first represents the type of the result which parser returns, the second represents the type for the tokens. 

A parser for things is a function from strings to lists of pairs of things and strings. Or more specifically for our case, a `Parser` for `T`'s is a function from a `State<Token>` to a `Result` of a pair of `T` and `State<Token>`.
Let's break that down a bit. A parser takes a `State<Token>`, which contains the index for at which `Token` we start parsing. And it returns a pair of `T`, which is the thing that has been parsed, and `State<Token>`, which is the state remaining after parsing. The pair `(T, State<Token>)` is wrapped in a `Result`, because the parsing might fail.
 
We will be defining parser combinators, which are functions which take one or more parsers and returns another parser. Because a parser is really just a function, we will be using an attribute to represent the parser which is initialized in the constructor. Parsers can thus be created by passing a lambda expression to constructor.    

```csharp
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
```

Most of the functionality of the Parser is done in extension methods in the static Parser class. As parsers also describe a monad, we can define `Select`, `SelectMany` for parsers. And since we also implemented these functions for `Result<T>`, implementing them for parsers is quite straight forward.

```
public static Parser<T2, Token> Select<T1, T2, Token>(this Parser<T1, Token> parser, Func<T1, T2> map)
	=> new Parser<T2, Token>(state1 =>
		from x in parser.Parse(state1)
		select (map(x.Item1), x.Item2));

public static Parser<T3, Token> SelectMany<T1, T2, T3, Token>(this Parser<T1, Token> parser, Func<T1, Parser<T2, Token>> then, Func<T1, T2, T3> map)
	=> new Parser<T3, Token>(state1 =>
		from x in parser.Parse(state1)
		from y in then(x.Item1).Parse(x.Item2)
		select (map(x.Item1, y.Item1), y.Item2));
```

We also need another combinator to represent options. The `Or` combinator takes 2 parsers. It first tries the first parser and if it succeeds, that result is returned, otherwise the result of the second parser is returned.
Because C# allows for (limited) operator overloading, we can also create an overload for '+': `parserA + parserB => parserA.Or(ParserB)`.   

```csharp
public static Parser<T1, Token> Or<T1, Token>(this Parser<T1, Token> parserL, Parser<T1, Token> parserR)
    => new Parser<T1, Token>(state =>
    {
        var resultL = parserL.Parse(state);
        if (resultL.HasValue)
            return resultL;

        var resultR = parserR.Parse(state);
        return resultR;
    });
```

## Example
Consider the following grammar:

```
Expr := <double> | <variable> | (Expr * Expr) | (Expr - Expr) | (Expr + Expr) 
```


The parser follows quite simply from the grammar.  Note that we need the `Lazy` combinator for recursive calls to the parser and that we need to cast the result to the `IExpression<double>` base type. 

```csharp
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

public static Parser<double, string> Double()
    => new Parser<double, string>(state =>
    {
        var token = state.Consume();
        var ok = double.TryParse(token, out var n);
        if (!ok)
            return new UnexpectedToken<(double, State<string>), string>("double", token);
        return (n, state);
    });
```

Implementing a parser for left-recursive grammars is left as an exercise to the reader.
