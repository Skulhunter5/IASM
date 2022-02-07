using System.Collections.Generic;

using System; // TEMPORARY
using System.Text.RegularExpressions; // TEMPORARY

namespace IASM {

    class Lexer {
        
        private readonly string _source;
        private readonly string _text;
        /* private int _position; */
        private int _line/* , _column = 1 */;

        public Lexer(string text, string source, int line) {
            _text = text;
            _source = source;
            _line = line;
        }

        /* private char c {
            get {
                if(_position >= _text.Length) return '\0';
                return _text[_position];
            }
        }

        private char GetChar(int offset) {
            if(_position+offset >= _text.Length) return '\0';
            return _text[_position+offset];
        }

        private void Next() {
            _position++;
            _column++;
        }

        private void SkipWhiteSpace() {
            while(char.IsWhiteSpace(c)) Next();
        }

        private int FindWhiteSpace() {
            while(!char.IsWhiteSpace(c) && c != '\0') Next();
            return c != '\0' ? _position : -1;
        }

        public Token CreateToken(Position position, string text) {
            if(Utils.numberRegex.IsMatch(text)) return new Token(TokenType.Number, text, position);
            if(Utils.registerRegex.IsMatch(text)) return new Token(TokenType.Register, text, position);
            return new Token(TokenType.Text, text, position);
        }

        public Token NextToken() {
            SkipWhiteSpace();
            if(_position >= _text.Length) return null;

            Position position = new Position(_source, _line, _column);
            int start = _position;
            
            int index = FindWhiteSpace();
            if(index == -1) return CreateToken(position, _text.Substring(start, _text.Length - start));

            return CreateToken(position, _text.Substring(start, index - start));
        }

        public Token[] run() {
            var tokens = new List<Token>();
            Token token = NextToken();
            while(token != null) {
                tokens.Add(token);
                token = NextToken();
            }
            return tokens.ToArray();
        } */

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