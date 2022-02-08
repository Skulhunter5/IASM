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

        public static readonly Regex numberRegex = new Regex("^-?[0-9]+$", RegexOptions.Compiled);
        public static readonly Regex registerRegex = new Regex("^([re][abcd]x|[abcd][xhb]|[re](bp|sp|si|di))$", RegexOptions.Compiled); // TODO: improve to contain actually every register

        public static readonly Regex JccRegex = new Regex("^(jn?([abglczsop]|[abgl]?e)|jp[eo]?)$", RegexOptions.Compiled);
        public static readonly Regex CMOVccRegex = new Regex("^(cmovn?([abglczsop]|[abgl]?e)|jp[eo]?)$", RegexOptions.Compiled);

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