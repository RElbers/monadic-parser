using System.Collections.Generic;
using MonadicParser.Parsing;
using Xunit;
using static MonadicParser.Parsing.Parser;

namespace MonadicParser.Tests
{
    public class ParseTest
    {
        [Fact]
        public void TestSymbol()
        {
            var p = Symbol("add");

            var tokens = new[] {"add", "sub"};
            var result = p.Run(tokens);

            Assert.Equal("add", result);
        }


        [Fact]
        public void TestWrap()
        {
            var p = Wrap("(", Symbol("x"), ")");

            var tokens = new[] {"(", "x", ")"};
            var result = p.Run(tokens);

            Assert.Equal("x", result);
        }


        [Fact]
        public void ParseMaybe_Ok()
        {
            var p =
                Maybe(Symbol("ok"));

            var tokens = new[] {"ok"};
            var result = p.Run(tokens);
            Assert.Equal("ok", result);
        }

        [Fact]
        public void ParseMaybe_Fail()
        {
            var p =
                Maybe(Symbol("ok"));

            var tokens = new[] {"fail"};
            var result = p.Run(tokens);
            
            switch (result)
            {
                case Ok<string> ok:
                    Assert.Null(ok.Value);
                    break;
                default:
                    Assert.True(false);
                    break;
            }
        }

        [Fact]
        public void ParseOr_Left()
        {
            var p =
                Symbol("add").Or(Symbol("sub"));

            var tokens = new[] {"add"};
            var result = p.Run(tokens);

            Assert.Equal("add", result);
        }

        [Fact]
        public void ParseOr_Right()
        {
            var p =
                Symbol("add").Or(Symbol("sub"));

            var tokens = new[] {"sub"};
            var result = p.Run(tokens);

            Assert.Equal("sub", result);
        }

        [Fact]
        public void ParseAny()
        {
            var p = Any(Symbol("add"));

            var tokens = new[] {"add", "add", "add", "sub", "add"};
            var result = p.Run(tokens);


            switch (result)
            {
                case Ok<IList<string>> ok:
                    Assert.Equal(new[] {"add", "add", "add"}, ok.Value);
                    break;
                default:
                    Assert.True(false);
                    break;
            }
        }

        [Fact]
        public void ParseAny_Zero()
        {
            var p =
                Any(Symbol("sub"));

            var tokens = new[] {"add", "add", "add", "sub", "add"};
            var result = p.Run(tokens);

            switch (result)
            {
                case Ok<IList<string>> ok:
                    Assert.Equal(new string[0], ok.Value);
                    break;
                default:
                    Assert.True(false);
                    break;
            }
        }

        [Fact]
        public void ParseSome()
        {
            var p = Any(Symbol("add"));

            var tokens = new[] {"add", "sub", "add"};
            var result = p.Run(tokens);

            switch (result)
            {
                case Ok<IList<string>> ok:
                    Assert.Equal(new[] {"add"}, ok.Value);
                    break;
                default:
                    Assert.True(false);
                    break;
            }
            
        }

        [Fact]
        public void ParseSome_Zero()
        {
            var p = Some(Symbol("add"));

            var tokens = new[] {"sub", "add", "add", "sub", "add"};
            var result = p.Run(tokens);

            Assert.False(result.HasValue);
        }
    }
}