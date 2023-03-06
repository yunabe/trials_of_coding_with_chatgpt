using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.IO;

public class CodeGenerator
{
    public static void Generate(Node rootNode, string outputFileName)
    {
        // Step 1: Set up the metadata builder and blob builder for creating the .NET assembly.

        var metadataBuilder = new MetadataBuilder();
        var ilBuilder = new BlobBuilder();

        metadataBuilder.AddAssembly(
            metadataBuilder.GetOrAddString(outputFileName),
            new Version(1, 0, 0, 0),
            metadataBuilder.GetOrAddString(""),
            publicKeyOrToken: default,
            flags: AssemblyFlags.None,
            hashAlgorithm: AssemblyHashAlgorithm.Sha1);

        // Step 2: Define the main method inside a new class.

        var programType = metadataBuilder.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass,
            metadataBuilder.GetOrAddString("ExpressionCompiler"),
            metadataBuilder.GetOrAddString("Program"),
            baseType: metadataBuilder.GetOrAddTypeSpecification(
                metadataBuilder.GetOrAddTypeReference(
                    metadataBuilder.GetOrAddAssemblyReference(
                        metadataBuilder.GetOrAddString("mscorlib"),
                        new Version(4, 0, 0, 0),
                        default,
                        publicKeyOrToken: default,
                        default,
                        AssemblyFlags.PublicKey),
                    metadataBuilder.GetOrAddString("System.Object"))));

        var mainMethod = metadataBuilder.AddMethodDefinition(
            MethodAttributes.Public | MethodAttributes.Static,
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            metadataBuilder.GetOrAddString("Main"),
            metadataBuilder.GetOrAddBlob(new BlobBuilder().MethodSignature(
                CallingConvention.Default,
                0,
                metadataBuilder.GetOrAddType(metadataBuilder.GetOrAddString("System.Void")),
                new[] { metadataBuilder.GetOrAddType(metadataBuilder.GetOrAddString("System.String[]")) })),
            0);

        var methodBody = new MethodBodyStreamEncoder(ilBuilder);
        var il = new InstructionEncoder(ilBuilder, methodBody);

        // Step 3: Emit IL instructions based on the parse tree.

        EmitNode(rootNode, il);

        il.OpCode(ILOpCode.Ret);

        var methodBodyOffset = methodBody.AddMethodBody(il);
        metadataBuilder.AddMethodBody(mainMethod, methodBodyOffset);

        // Step 4: Write the assembly to disk.

        using var peStream = File.Create(outputFileName);
        WritePEImage(peStream, metadataBuilder, ilBuilder, mainMethod);
    }

    private static void EmitNode(Node node, InstructionEncoder il)
    {
        switch (node)
        {
            case NumberNode numberNode:
                il.LoadConstant(numberNode.Value);
                break;

            case BinaryOperationNode binaryOperationNode:
                EmitNode(binaryOperationNode.Left, il);
                EmitNode(binaryOperationNode.Right, il);

                switch (binaryOperationNode.Operator)
                {
                    case TokenType.Plus:
                        il.OpCode(ILOpCode.Add);
                        break;

                    case TokenType.Minus:
                        il.OpCode(ILOpCode.Sub);
                        break;

                    case TokenType.Multiply:
                        il.OpCode(ILOpCode.Mul);
                        break;

                    case TokenType.Divide:
                        il.OpCode(ILOpCode.Div);
                        break;

                    case TokenType.Power:
                        il.OpCode(ILOpCode.Call);
                        var mathPowMethodHandle = il.Method(
                            System.Math.Pow,
                            isReadOnly: true,
                            constrainedType: default);

                        il.Token(mathPowMethodHandle, mathPowMethodHandle.GetMethodSignature());
                        break;
                }

                break;

            case ParenthesizedExpressionNode parenthesizedExpressionNode:
                EmitNode(parenthesizedExpressionNode.Expression, il);
                break;

            case IdentifierNode identifierNode:
                throw new NotImplementedException("Identifier evaluation not implemented.");

            case FunctionCallNode functionCallNode:
                foreach (var argument in functionCallNode.Arguments)
                {
                    EmitNode(argument, il);
                }

                switch (functionCallNode.Identifier)
                {
                    case "print!":
                        il.OpCode(ILOpCode.Call);
                        var consoleWriteLineMethodHandle = il.Method(
                            typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) }),
                            isReadOnly: false,
                            constrainedType: default);

                        il.Token(consoleWriteLineMethodHandle, consoleWriteLineMethodHandle.GetMethodSignature());
                        break;

                    default:
                        throw new NotImplementedException($"Function {functionCallNode.Identifier} not implemented.");
                }

                break;

            default:
                throw new NotImplementedException($"Node of type {node.GetType()} not implemented.");
        }
    }

    private static void WritePEImage(Stream peStream, MetadataBuilder metadataBuilder, BlobBuilder ilBuilder, MethodDefinitionHandle entryPointHandle)
    {
        var peHeaderBuilder = new PEHeaderBuilder(
            PEHeaders.PEHeader.Magic.PE32,
            PEHeaders.PEHeader.Machine.I386,
            new System.Collections.Immutable.PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage,
                majorLinkerVersion: 5,
                minorLinkerVersion: 0,
                systemVersion: new Version(5, 1),
                fileSize: ilBuilder.Count + metadataBuilder.GetMetadataBlobBuilder().Count + 128,
                timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                formatVersion: new Version(2, 0)
            ));

        var peBuilder = new ManagedPEBuilder(
            peHeaderBuilder,
            new MetadataRootBuilder(metadataBuilder),
            ilBuilder,
            entryPoint: entryPointHandle,
            flags: new CorFlags(isILOnly: true, requires32Bit: true, strongNameSigned: false, trackDebugData: true),
            deterministicIdProvider: persistentIdentifier => BlobContentId.FromHash(persistentIdentifier));

        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);

        peBlob.WriteContentTo(peStream);
        peStream.Flush();
    }
}
