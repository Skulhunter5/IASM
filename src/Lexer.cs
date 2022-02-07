using System.Collections.Generic;

namespace IASM {

    class Lexer {
        
        private readonly string _source;
        private readonly string _text;
        private int _position;
        private int _line, _column = 1;

        public Lexer(string text, string source, int line) {
            _text = text;
            _source = source;
            _line = line;
        }

        private char c {
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

        public Word[] GetWords() {
            List<Word> words = new List<Word>();

            while(c != '\0') {
                SkipWhiteSpace();
                if(_position >= _text.Length) break;

                Position position = new Position(_source, _line, _column);
                int start = _position;
                
                int index = FindWhiteSpace();
                if(index == -1) words.Add(new Word(position, _text.Substring(start, _text.Length - start)));
                else words.Add(new Word(position, _text.Substring(start, index - start)));
            }

            return words.ToArray();
        }

        public Word NextWord() {
            SkipWhiteSpace();
            if(_position >= _text.Length) return null;

            Position position = new Position(_source, _line, _column);
            int start = _position;
            
            int index = FindWhiteSpace();
            if(index == -1) return new Word(position, _text.Substring(start, _text.Length - start));

            return new Word(position, _text.Substring(start, index - start));
        }

        public Word[] run() {
            var words = new List<Word>();
            Word word = NextWord();
            while(word != null) {
                words.Add(word);
                word = NextWord();
            }
            return words.ToArray();
        }

    }

}