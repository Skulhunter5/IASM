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
        public static readonly Regex registerRegex = new Regex("^([re][abcd]x|[abcd][xhb]|[re](bp|sp|si|di)|r([89]|1[0-5])[dwb]?)$", RegexOptions.Compiled); // TODO: improve to contain actually every register

        public static readonly Regex register_rax_Regex = new Regex("^([re]ax|a[xhb])$");
        public static readonly Regex register_rbx_Regex = new Regex("^([re]bx|b[xhb])$");
        public static readonly Regex register_rcx_Regex = new Regex("^([re]cx|c[xhb])$");
        public static readonly Regex register_rdx_Regex = new Regex("^([re]dx|d[xhb])$");
        public static readonly Regex register_rsp_Regex = new Regex("^([re]sp)$");
        public static readonly Regex register_rbp_Regex = new Regex("^([re]bp)$");
        public static readonly Regex register_rsi_Regex = new Regex("^([re]si)$");
        public static readonly Regex register_rdi_Regex = new Regex("^([re]di)$");

        public static int GetRegisterIdentifier(string text) {
            if(register_rax_Regex.IsMatch(text)) return 0b000;
            else if(register_rcx_Regex.IsMatch(text)) return 0b001;
            else if(register_rdx_Regex.IsMatch(text)) return 0b010;
            else if(register_rbx_Regex.IsMatch(text)) return 0b011;
            else if(register_rsp_Regex.IsMatch(text)) return 0b100;
            else if(register_rbp_Regex.IsMatch(text)) return 0b101;
            else if(register_rsi_Regex.IsMatch(text)) return 0b110;
            else if(register_rdi_Regex.IsMatch(text)) return 0b111;
            else throw new NotImplementedException(); // Unreachable
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