using System;
using System.Collections.Generic;

namespace IASM {

    static class Constants {

        public static readonly byte REXW = (byte) 0b01001000;

        public static readonly byte JccBaseOpcode = (byte) 0x80;
        public static readonly byte CMOVccBaseOpcode = (byte) 0x40;
        public static readonly byte SETccBaseOpcode = (byte) 0x90;
        public static readonly Dictionary<string, byte> ccOffsets = new Dictionary<string, byte>() {
            {"a", 0x07},
            {"ae", 0x03},
            {"b", 0x02},
            {"be", 0x06},
            {"c", 0x02},
            {"e", 0x04},
            {"g", 0x0F},
            {"ge", 0x0D},
            {"l", 0x0C},
            {"le", 0x0E},
            {"na", 0x06},
            {"nae", 0x02},
            {"nb", 0x03},
            {"nbe", 0x07},
            {"nc", 0x03},
            {"ne", 0x05},
            {"ng", 0x0E},
            {"nge", 0x0C},
            {"nl", 0x0D},
            {"nle", 0x0F},
            {"no", 0x01},
            {"np", 0x0B},
            {"ns", 0x09},
            {"nz", 0x05},
            {"o", 0x00},
            {"p", 0x0A},
            {"pe", 0x0A},
            {"po", 0x0B},
            {"s", 0x08},
            {"z", 0x04},
        };

        public static byte ccOffset(string cc) {
            if(!Constants.ccOffsets.TryGetValue(cc, out byte ccOffset)) throw new NotImplementedException(); // Unreachable
            return ccOffset;
        }

        public static int GetRegisterIdentifier(string text) {
            if(text.Contains("bp")) return 0b101;
            else if(text.Contains("sp")) return 0b100;
            else if(text.Contains("si")) return 0b110;
            else if(text.Contains("di")) return 0b111;
            else if(text.Contains("a")) return 0b000;
            else if(text.Contains("c")) return 0b001;
            else if(text.Contains("d")) return 0b010;
            else if(text.Contains("b")) return 0b011;
            else throw new NotImplementedException();
        }

    }

}