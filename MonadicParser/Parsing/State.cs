using System.Collections.Generic;
using System.Linq;

namespace MonadicParser.Parsing
{
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


        public override string ToString()
        {
            var s = string.Join(", ", Tokens.Skip(Idx));
            return $"[{s}]";
        }
    }
    
}