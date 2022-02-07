namespace IASM {

    abstract class Error {}

    abstract class GeneralError : Error {
        public override string ToString() {
            return "[Error]: ";
        }
    }

    // General errors

    sealed class UnexpectedInstructionError : GeneralError {
        public UnexpectedInstructionError(Token token) : base() {
            Token = token;
        }

        public Token Token { get; }

        public override string ToString() {
            return base.ToString() + "Unexpected instruction: " + Token;
        }
    }

    sealed class ExpectedInstructionError : GeneralError {
        public ExpectedInstructionError(Position position) : base() {
            Position = position;
        }

        public Position Position { get; }

        public override string ToString() {
            return base.ToString() + "Expected instruction: at " + Position;
        }
    }

}