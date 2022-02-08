using System;
using System.Collections.Generic;
using System.Buffers.Binary;

namespace IASM {

    class AssembleResult : Result {

        public AssembleResult(byte[] bytes, int headersSize, int codeSize, int dataSize, Error error) : base(error) {
            Bytes = bytes;
            HeadersSize = headersSize;
            CodeSize = codeSize;
            DataSize = dataSize;
        }

        public byte[] Bytes { get; }
        public int HeadersSize { get; }
        public int CodeSize { get; }
        public int DataSize { get; }

    }

    class FillLabelContract {

        public FillLabelContract(int position, string label, int offset) {
            Position = position;
            Label = label;
            Offset = offset;
        }

        public int Position { get; }
        public string Label { get; }
        public int Offset { get; }

    }

    class IASMAssembler {

        private readonly string _source, _text;
        private List<byte> _bytes;

        public IASMAssembler(string source, string text) {
            _source = source;
            _text = text;
        }

        private void Append(byte[] bytes) {
            _bytes.AddRange(bytes);
        }

        private void Append(string str) {
            char[] chars = str.ToCharArray();
            for(int i = 0; i < str.Length; i++) {
                _bytes.Add((byte) chars[i]);
            }
        }

        private void Append(sbyte b) {
            _bytes.Add((byte) b);
        }

        private void Append(byte b) {
            _bytes.Add(b);
        }
        
        private void Append(short value) {
            _bytes.Add((byte) value);
            _bytes.Add((byte) (value >> 8));
        }
        
        private void Append(ushort value) {
            _bytes.Add((byte) value);
            _bytes.Add((byte) (value >> 8));
        }

        private void Append(int value) {
            _bytes.Add((byte) value);
            _bytes.Add((byte) (value >> 8));
            _bytes.Add((byte) (value >> 16));
            _bytes.Add((byte) (value >> 24));
        }

        private void Append(uint value) {
            _bytes.Add((byte) value);
            _bytes.Add((byte) (value >> 8));
            _bytes.Add((byte) (value >> 16));
            _bytes.Add((byte) (value >> 24));
        }

        private void Append(ulong value) {
            _bytes.Add((byte) value);
            _bytes.Add((byte) (value >> 8));
            _bytes.Add((byte) (value >> 16));
            _bytes.Add((byte) (value >> 24));
            _bytes.Add((byte) (value >> 32));
            _bytes.Add((byte) (value >> 40));
            _bytes.Add((byte) (value >> 48));
            _bytes.Add((byte) (value >> 56));
        }

        private void Replace(int pos, int value) {
            _bytes[pos] = (byte) value;
            _bytes[pos+1] = (byte) (value >> 8);
            _bytes[pos+2] = (byte) (value >> 16);
            _bytes[pos+3] = (byte) (value >> 24);
        }

        private void r64_rm64(byte opcode, string regA, string regB) { // TODO: check if it is always encoded like this or if I have to create the functions through how they are actually done (likely)
            Append(Constants.REXW);
            Append(opcode);
            Append((byte) (0b11000000 + (Constants.GetRegisterCode(regA) << 3) + Constants.GetRegisterCode(regB)));
        }

        private void r64_imm32SE__digit(byte opcode, byte digit, string reg, int imm32) {
            Append(Constants.REXW);
            Append(opcode);
            Append((byte) (0b11000000 + (digit << 3) + Constants.GetRegisterCode(reg)));
            Append(imm32);
        }

        public AssembleResult run() {
            _bytes = new List<byte>();

            Dictionary<string, int> labels = new Dictionary<string, int>();
            List<FillLabelContract> fillLabelContracts = new List<FillLabelContract>();

            string[] lines = Utils.GetLines(_text);
            for(int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                Token[] tokens = new Lexer(line, _source, i+1).run();

                if(tokens.Length == 0) continue;

                if(tokens[0].Text == "mov") {
                    if(tokens.Length < 3) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 3) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[3]));

                    if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Register) {
                        // REX.W + 8B /r
                        r64_rm64(0x8B, tokens[1].Text, tokens[2].Text);
                    } else if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Number) {
                        // REX.W + B8+rd io
                        // - REX.W
                        Append(Constants.REXW);
                        // - opcode+rd
                        Append((byte) (0xB8 + Constants.GetRegisterCode(tokens[1].Text)));
                        // - imm64
                        Append(ulong.Parse(tokens[2].Text));
                    } else throw new NotImplementedException();

                } else if(tokens[0].Text == "syscall") {
                    if(tokens.Length > 1) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[1]));

                    // 0F 05
                    Append((byte) 0x0f);
                    Append((byte) 0x05);
                } else if(tokens[0].Text.EndsWith(":")) {
                    labels.Add(tokens[0].Text.Substring(0, tokens[0].Text.Length-1), _bytes.Count);
                } else if(tokens[0].Text == "cmp") {
                    if(tokens.Length < 3) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 3) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[3]));

                    if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Register) {
                        // REX.W + 3B /r :: CMP r64, r/m64
                        r64_rm64(0x3B, tokens[1].Text, tokens[2].Text);
                    } else if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Number) {
                        // REX.W + 81 /7 id :: CMP r/m64, imm32(se)
                        r64_imm32SE__digit(0x81, 0b111, tokens[1].Text, int.Parse(tokens[2].Text));
                    } else throw new NotImplementedException();

                } else if(Utils.JccRegex.IsMatch(tokens[0].Text)) {
                    if(tokens.Length < 2) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 2) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[2]));

                    // TODO: add placeholder and decide which jump type (close, near, maybe even far) to use later when the actual distance to jump is known

                    // 0F cc:code cd :: Jcc rel32
                    // - opcode
                    Append((byte) 0x0f);
                    // - cc:code
                    string cc = tokens[0].Text.Substring(1, tokens[0].Text.Length-1);
                    Append((byte) (Constants.JccBaseOpcode + Constants.ccOffset(cc)));
                    // cd :: rel32
                    fillLabelContracts.Add(new FillLabelContract(_bytes.Count, tokens[1].Text, -_bytes.Count - 4));
                    Append((int) 0);

                } else if(tokens[0].Text == "xor") {
                    if(tokens.Length < 3) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 3) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[3]));

                    if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Register) {
                        // REX.W + 33 /r :: XOR r64, r/m64
                        r64_rm64(0x33, tokens[1].Text, tokens[2].Text);
                    } else throw new NotImplementedException();

                } else if(tokens[0].Text == "add") {
                    if(tokens.Length < 3) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 3) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[3]));
                    
                    if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Register) {
                        // REX.W + 03 /r :: ADD r64, r/m64
                        r64_rm64(0x03, tokens[1].Text, tokens[2].Text);
                    } else if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Number) {
                        // REX.W + 81 /0 id :: ADD r/m64, imm32(se)
                        r64_imm32SE__digit(0x81, 0b000, tokens[1].Text, int.Parse(tokens[2].Text));
                    } else throw new NotImplementedException();

                } else if(tokens[0].Text == "sub") {
                    if(tokens.Length < 3) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 3) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[3]));
                    
                    if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Register) {
                        // REX.W + 2B /r :: ADD r64, r/m64
                        r64_rm64(0x2B, tokens[1].Text, tokens[2].Text);
                    } else if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Number) {
                        // REX.W + 81 /0 id :: ADD r/m64, imm32(se)
                        r64_imm32SE__digit(0x81, 0b101, tokens[1].Text, int.Parse(tokens[2].Text));
                    } else throw new NotImplementedException();

                } else if(tokens[0].Text == "push") {
                    if(tokens.Length < 2) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 2) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[2]));

                    if(tokens[1].TokenType == TokenType.Register) {
                        // 50+rd
                        Append((byte) (0x50 + Constants.GetRegisterCode(tokens[1].Text)));
                    } else if(tokens[1].TokenType == TokenType.Number) {
                        // 68 id
                        Append((byte) 0x68);
                        Append(int.Parse(tokens[1].Text));
                    } else throw new NotImplementedException();

                } else if(tokens[0].Text == "pop") {
                    if(tokens.Length < 2) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 2) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[2]));

                    if(tokens[1].TokenType == TokenType.Register) {
                        // 58+rd
                        Append((byte) (0x58 + Constants.GetRegisterCode(tokens[1].Text)));
                    } else throw new NotImplementedException();

                } else throw new NotImplementedException();

            }

            foreach(FillLabelContract contract in fillLabelContracts) {
                if(!labels.TryGetValue(contract.Label, out int value)) {
                    Console.WriteLine("[ERROR]: Unknown label: '" + contract.Label + "'");
                    Environment.Exit(1);
                }
                Replace(contract.Position, value + contract.Offset);
            }

            //for(int i = 0; i < _bytes.Count; i++) Console.WriteLine(i + ": " + _bytes[i]);

            // HEADERS

            ulong size_Elf64Header = 64;
            byte[] header = new byte[size_Elf64Header];
            ushort size_Elf64_PhtEntry = 56;
            ulong headersSize = size_Elf64Header + size_Elf64_PhtEntry;
            ulong codeSize = (ulong) _bytes.Count;
            int j = 0;
            // char[4] elfMagicNumber
            header[j++] = 0x7F;
            header[j++] = (byte) 'E';
            header[j++] = (byte) 'L';
            header[j++] = (byte) 'F';
            // u8 bitAmount
            header[j++] = 2;
            // u8 endian
            header[j++] = 1;
            // u8 elfVersion1
            header[j++] = 1;
            // u8 osAbi
            header[j++] = 0;
            // u8 abiVersion
            header[j++] = 0;
            // u8[7] unused
            j += 7;
            // u16 objFileType
            header[j++] = 3; // 3 = shared file, 2 = executable
            header[j++] = 0;
            // u16 arch
            header[j++] = 0x3E;
            header[j++] = 0;
            // u32 elfVersion2
            header[j++] = 1;
            header[j++] = 0;
            header[j++] = 0;
            header[j++] = 0;
            // u64 entryPointOffset
            BinaryPrimitives.WriteUInt64LittleEndian(header.AsSpan(j, 8), 0x400000 + headersSize);
            j += 8;
            // u64 phtOffset
            BinaryPrimitives.WriteUInt64LittleEndian(header.AsSpan(j, 8), size_Elf64Header);
            j += 8;
            // u64 shtOffset
            j += 8;
            // u32 processorFlags
            j += 4;
            // u16 headerSize
            header[j++] = 64;
            header[j++] = 0;
            // u16 phtEntrySize
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(j, 2), size_Elf64_PhtEntry);
            j += 2;
            // u16 numPhtEntries
            header[j++] = 1;
            header[j++] = 0;
            // u16 shtEntrySize
            j += 2;
            // u16 numShtEntries
            j += 2;
            // u16 namesSht
            j += 2;
            // phtEntry
            j = 0;
            byte[] phtEntry = new byte[size_Elf64_PhtEntry];
            // u32 segmentType
            phtEntry[j++] = 1;
            phtEntry[j++] = 0;
            phtEntry[j++] = 0;
            phtEntry[j++] = 0;
            // u32 flags
            phtEntry[j++] = 7; // 0: execute, 1: write, 2: read
            phtEntry[j++] = 0;
            phtEntry[j++] = 0;
            phtEntry[j++] = 0;
            // u64 offset
            BinaryPrimitives.WriteUInt64LittleEndian(phtEntry.AsSpan(j, 8), headersSize);
            j += 8;
            // u64 vaddr
            BinaryPrimitives.WriteUInt64LittleEndian(phtEntry.AsSpan(j, 8), 0x400000 + headersSize);
            j += 8;
            // u64 paddr
            BinaryPrimitives.WriteUInt64LittleEndian(phtEntry.AsSpan(j, 8), 0x400000 + headersSize);
            j += 8;
            // u64 sizeInFile
            BinaryPrimitives.WriteUInt64LittleEndian(phtEntry.AsSpan(j, 8), codeSize);
            j += 8;
            // u64 sizeInMem
            BinaryPrimitives.WriteUInt64LittleEndian(phtEntry.AsSpan(j, 8), codeSize);
            j += 8;
            // u64 align
            BinaryPrimitives.WriteUInt64LittleEndian(phtEntry.AsSpan(j, 8), 0x1000);
            j += 8;

            List<byte> bytes = new List<byte>();

            bytes.AddRange(header);
            bytes.AddRange(phtEntry);
            bytes.AddRange(_bytes);

            return new AssembleResult(bytes.ToArray(), (int) headersSize, _bytes.Count, 0, null);
        }

    }

}