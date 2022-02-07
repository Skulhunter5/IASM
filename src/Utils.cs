using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IASM {

    class Position {
        public Position(string file, int line, int column) {
            File = file;
            Line = line;
            Column = column;
        }
        public override string ToString()
        {
            return File + ":" + Line + ":" + Column;
        }
        public string File { get; }
        public int Line { get; }
        public int Column { get; }
    }

    abstract class Result {
        public Result(Error error) {
            Error = error;
        }
        public Error Error { get; }
    }

    static class Utils {

        public static readonly Regex numberRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);
        public static readonly Regex registerRegex = new Regex("^([re][abcd]x|[abcd][xhb]|[re](bp|sp|si|di))$", RegexOptions.Compiled); // TODO: improve to contain actually every register

        public static int GetRegisterIdentifier(string text) {
            if(text.Contains("bp")) return 0b101;
            else if(text.Contains("sp")) return 0b101;
            else if(text.Contains("si")) return 0b110;
            else if(text.Contains("di")) return 0b101;
            else if(text.Contains("a")) return 0b000;
            else if(text.Contains("c")) return 0b001;
            else if(text.Contains("d")) return 0b010;
            else if(text.Contains("b")) return 0b011;
            else throw new NotImplementedException();
        }

        public static string[] GetLines(string text) {
            List<string> lines = new List<string>();
            string line = "";
            for(int i = 0; i < text.Length; i++) {
                if(text[i] != '\n') line += text[i];
                else {
                    lines.Add(line);
                    line = "";
                }
            }
            if(line != "") lines.Add(line);
            return lines.ToArray();
        }

    }

}