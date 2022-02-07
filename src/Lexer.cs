using System.Text.RegularExpressions;

namespace IASM {

    class Lexer {
        
        private readonly string _source;
        private readonly string _text;
        private int _line;

        public Lexer(string text, string source, int line) {
            _text = text;
            _source = source;
            _line = line;
        }

        public Token CreateToken(string text, int column) {
            Position position = new Position(_source, _line, column);
            if(Utils.numberRegex.IsMatch(text)) return new Token(TokenType.Number, text, position);
            if(Utils.registerRegex.IsMatch(text)) return new Token(TokenType.Register, text, position);
            return new Token(TokenType.Text, text, position);
        }

        private static readonly Regex tokenRegex = new Regex("(?<=(?:^|\\s))(\\S+)(?=(?:$|\\s))", RegexOptions.Compiled);
        //private static readonly Regex tokenRegex2 = new Regex("(?<=(?:^|\\s))(\".*\"|\\S+)(?=(?:$|\\s))", RegexOptions.Compiled);
        public Token[] run() {
            MatchCollection matches = tokenRegex.Matches(_text);
            Token[] tokens = new Token[matches.Count];
            for(int i = 0; i < matches.Count; i++) tokens[i] = CreateToken(matches[i].Value, matches[i].Index+1);
            return tokens;
        }

    }

}