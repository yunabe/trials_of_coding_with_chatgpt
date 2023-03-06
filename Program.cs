using System;
using System.IO;

public class Program
{
    static void Main(string[] args)
    {
        var inputFile = "";
        var outputFile = "";

        // Parse command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-i" && i + 1 < args.Length)
            {
                inputFile = args[i + 1];
            }
            else if (args[i] == "-o" && i + 1 < args.Length)
            {
                outputFile = args[i + 1];
            }
        }

        // Ensure required arguments are set
        if (string.IsNullOrEmpty(inputFile) || string.IsNullOrEmpty(outputFile))
        {
            Console.WriteLine("Usage: Compiler -i <input file> -o <output file>");
            return;
        }

        // Read input file
        var sourceCode = File.ReadAllText(inputFile);

        // Create lexer and tokenize input
        var lexer = new Lexer(sourceCode);
        var tokens = new List<Token>();
        while (true) {
            var token = lexer.NextToken();
            tokens.Add(token);
            if (token.Type == TokenType.EndOfInput) {
                break;
            }
        }

        // Create parser and generate parse tree
        var parser = new Parser(tokens);
        var rootNode = parser.Parse();

        // Create code generator and generate code
        var codeGenerator = new CodeGenerator();
        var generatedCode = codeGenerator.GenerateCode(rootNode);

        // Write compiled code to output file
        File.WriteAllText(outputFile, generatedCode);

        Console.WriteLine("Compilation successful!");
    }
}
