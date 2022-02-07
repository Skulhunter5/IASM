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
                        // REX.W + 89 /r
                        Append((byte) 0b01001000);
                        Append((byte) 0x89);
                        Append((byte) (0b11000000 + (Utils.GetRegisterIdentifier(tokens[1].Text) << 3) + Utils.GetRegisterIdentifier(tokens[2].Text)));
                    } else if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Number) {
                        // REX.W + B8+rd io
                        Append((byte) 0b01001000);
                        Append((byte) (0xB8 + Utils.GetRegisterIdentifier(tokens[1].Text)));
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
                        // REX.W + 3B /r
                        Append((byte) 0b01001000);
                        Append((byte) 0x3B);
                        Append((byte) (0b11000000 + (Utils.GetRegisterIdentifier(tokens[1].Text) << 3) + Utils.GetRegisterIdentifier(tokens[2].Text)));
                    } else throw new NotImplementedException();

                } else if(tokens[0].Text == "je") {
                    if(tokens.Length < 2) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 2) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[2]));

                    // TODO: add placeholder and decide which jump type (close, near, maybe even far) to use later when the actual distance to jump is known

                    Append((byte) 0x0f);
                    Append((byte) 0x84);
                    fillLabelContracts.Add(new FillLabelContract(_bytes.Count, tokens[1].Text, -_bytes.Count - 4));
                    Append((int) 0);

                    /* // JE rel8
                    byte tmp = (byte) _bytes.Count;
                    Append((byte) 0x74);
                    Console.WriteLine((sbyte) (labels.GetValueOrDefault(tokens[1].Text) - tmp - 2));
                    Append((sbyte) (labels.GetValueOrDefault(tokens[1].Text) - tmp - 2)); */
                } else if(tokens[0].Text == "xor") {
                    if(tokens.Length < 3) return new AssembleResult(null, 0, 0, 0, new ExpectedInstructionError(new Position(_source, i+1, line.Length+1)));
                    if(tokens.Length > 3) return new AssembleResult(null, 0, 0, 0, new UnexpectedInstructionError(tokens[3]));

                    if(tokens[1].TokenType == TokenType.Register && tokens[2].TokenType == TokenType.Register) {
                        // REX.W + 33 /r
                        Append((byte) 0b01001000);
                        Append((byte) 0x33);
                        Append((byte) (0b11000000 + (Utils.GetRegisterIdentifier(tokens[1].Text) << 3) + Utils.GetRegisterIdentifier(tokens[2].Text)));
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