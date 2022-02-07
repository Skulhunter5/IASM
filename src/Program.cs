using System;
using System.IO;

namespace IASM {

    class Program {

        static void Main(string[] args) {
            string source = "res/test.asm";
            //string source = "\\\\wsl$\\Ubuntu-20.04\\shared\\testIons.asm";
            source = Path.GetFullPath(source);
            string asm = File.ReadAllText(source).Replace("\r\n", "\n");
            IASMAssembler assembler = new IASMAssembler(source, asm);
            var result = assembler.run();
            if(result.Error != null) {
                Console.Error.WriteLine(result.Error);
                Environment.ExitCode = 1;
                return;
            }
            Console.WriteLine("Total size: " + (result.HeadersSize + result.CodeSize + result.DataSize) + " bytes\n" + "- Headers: " + result.HeadersSize + "\n- Code: " + result.CodeSize + "\n- Data: " + result.DataSize);
            // Write
            using (FileStream fs = new FileStream("res/test", FileMode.OpenOrCreate)) {
                using (BinaryWriter bw = new BinaryWriter(fs)) {
                    bw.Write(result.Bytes);
                    /* bw.Write(header);
                    bw.Write(phtEntry);
                    bw.Write(asmCode);
                    bw.Write("Hello, world!\n"); */
                }
            }
            //File.WriteAllBytes("\\\\wsl$\\Ubuntu-20.04\\shared\\testIons", result.Bytes);
        }

    }

}
