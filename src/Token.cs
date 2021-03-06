namespace IASM {

    enum TokenType {
        Text,
        Register,
        Number,
    }

    class Token {

        public Token(TokenType tokenType, string text, Position position) {
            TokenType = tokenType;
            Text = text;
            Position = position;
        }

        public TokenType TokenType { get; }
        public string Text { get; }
        public Position Position { get; }

        public override string ToString() {
            return "'" + Text + "' (" + (TokenType == TokenType.Text ? "TEXT" : (TokenType == TokenType.Register ? "REG" : (TokenType == TokenType.Number ? "NUM" : "INVALID"))) + ") at " + Position;
        }

    }

}