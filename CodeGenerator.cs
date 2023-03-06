using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

public class CodeGenerator
{
    private readonly MetadataBuilder _metadataBuilder;
    private readonly BlobBuilder _ilBuilder;
    private readonly Dictionary<string, MethodDefinitionHandle> _functionTable;

    public CodeGenerator()
    {
        // Create new metadata and IL builders.
        _metadataBuilder = new MetadataBuilder();
        _ilBuilder = new BlobBuilder();

        // Create function table to store function definitions.
        _functionTable = new Dictionary<string, MethodDefinitionHandle>();
    }

    public byte[] Generate(Node rootNode)
    {
        // Define a new module and assembly.
        AddAssembly("MathematicalExpressionLanguage");

        // Define the program type.
        var programType = DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);

        // Define the program's entry point method.
        var entryPointMethod = DefineMethod(programType, "Main", MethodAttributes.Public | MethodAttributes.Static, _metadataBuilder.GetOrAddBlob(MetadataTokens.SignatureHeader.GetOrCreate(SignatureKind.Method).PrefixSize(2).UInt16(0x2A).UInt16(0x01).Blob));
        _metadataBuilder.SetEntryPoint(entryPointMethod);

        // Generate IL for the program.
        Generate(rootNode, entryPointMethod);

        // Emit the IL into the metadata and create the final PE image.
        var entryPointHandle = GetMethodHandle(entryPointMethod);
        var peBuilder = new ManagedPEBuilder(new PEHeaderBuilder(), new MetadataRootBuilder(_metadataBuilder), _ilBuilder, entryPointHandle);
        var peBlob = new BlobBuilder();
        var contentIdProvider = new BlobContentIdProvider(new Guid("53AFEF17-CCB0-491B-9BD3-329A7F1AD619"), new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var peContentId = peBuilder.Serialize(peBlob, contentIdProvider.GetId);

        return peBlob.ToArray();
    }

    private void AddAssembly(string name)
    {
        // Add the assembly definition to the metadata.
        _metadataBuilder.AddAssembly(
            _metadataBuilder.GetOrAddString(name),
            new Version(1, 0, 0, 0),
            default(StringHandle),
            default(BlobHandle),
            flags: AssemblyFlags.None,
            hashAlgorithm: AssemblyHashAlgorithm.Sha1);
    }

    private TypeDefinitionHandle DefineType(string name, TypeAttributes attributes)
    {
        // Define a new type in the metadata.
        return _metadataBuilder.AddTypeDefinition(
            attributes,
            _metadataBuilder.GetOrAddString(""),
            _metadataBuilder.GetOrAddString(name),
            baseType: default(EntityHandle));
    }

    private MethodDefinitionHandle DefineMethod(TypeDefinitionHandle type, string name, MethodAttributes attributes, BlobHandle signature)
    {
        // Define a new method in the metadata.
        return _metadataBuilder.AddMethodDefinition(
            attributes,
            MethodImplAttributes.IL,
            _metadataBuilder.GetOrAddString(name),
            signature,
            methodBody: _ilBuilder.Count,
            parameterList: default(ParameterHandle),
            type);
    }

    private void Generate(Node node, MethodDefinitionHandle method)
    {
        if (node is NumberNode numberNode)
        {
            // Generate IL to push a constant value onto the stack.
            _ilBuilder.Emit(OpCodes.Ldc_I4, numberNode.Value);
        }
        else if (node is BinaryOperationNode binaryOperationNode)
        {
            // Generate IL to evaluate the left and right operands.
            Generate(binaryOperationNode.Left, method);
            Generate(binaryOperationNode.Right, method);

            // Generate IL to perform the binary operation.
            switch (binaryOperationNode.Operator)
            {
                case TokenType.Plus:
                    _ilBuilder.Emit(OpCodes.Add);
                    break;
                case TokenType.Minus:
                    _ilBuilder.Emit(OpCodes.Sub);
                    break;
                case TokenType.Multiply:
                    _ilBuilder.Emit(OpCodes.Mul);
                    break;
                case TokenType.Divide:
                    _ilBuilder.Emit(OpCodes.Div);
                    break;
                case TokenType.Power:
                    _ilBuilder.Emit(OpCodes.Call, GetMethodHandle(typeof(Math).GetMethod("Pow", new[] { typeof(double), typeof(double) })));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(binaryOperationNode));
            }
        }
        else if (node is IdentifierNode identifierNode)
        {
            // Look up the value of the identifier and generate IL to push it onto the stack.
            if (_functionTable.TryGetValue(identifierNode.Value, out var functionHandle))
            {
                // The identifier refers to a function, so call the function.
                _ilBuilder.Emit(OpCodes.Call, functionHandle);
            }
            else
            {
                // The identifier refers to a variable, so load it from the local variables and push it onto the stack.
                var localIndex = DefineLocal(typeof(int));
                _ilBuilder.Emit(OpCodes.Ldloc, localIndex);
            }
        }
        else if (node is FunctionCallNode functionCallNode)
        {
            // Generate IL to evaluate the function arguments.
            foreach (var argumentNode in functionCallNode.Arguments)
            {
                Generate(argumentNode, method);
            }

            // Look up the handle for the function and generate IL to call it.
            if (_functionTable.TryGetValue(functionCallNode.Identifier, out var functionHandle))
            {
                _ilBuilder.Emit(OpCodes.Call, functionHandle);
            }
            else
            {
                throw new ArgumentException($"Function '{functionCallNode.Identifier}' not found.");
            }
        }
        else if (node is ParenthesizedExpressionNode parenthesizedExpressionNode)
        {
            // Generate IL to evaluate the parenthesized expression.
            Generate(parenthesizedExpressionNode.Expression, method);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(node));
        }
    }

    private int DefineLocal(Type type)
    {
        // Define a new local variable and return its index.
        var localIndex = _ilBuilder.Count;
        _ilBuilder.LocalSignature()
            .Byte(0x07) // local variable type (element type value 7 = int32)
            .EndParameters();
        _ilBuilder.Emit(OpCodes.Stloc, localIndex);
        _ilBuilder.Emit(OpCodes.Ldloc, localIndex);

        return localIndex;
    }

    private MethodDefinitionHandle GetMethodHandle(MethodInfo methodInfo)
    {
        // Get the method definition handle for the specified method.
        var blobBuilder = new BlobBuilder();
        var parameterCount = methodInfo.GetParameters().Length;
        var signatureWriter = new SignatureWriter(blobBuilder);

        signatureWriter.WriteMethodSignature(
            methodInfo.CallingConvention,
            returnType => returnType.FromSystemType(methodInfo.ReturnType),
            parameters =>
            {
                for (int i = 0; i < parameterCount; i++)
                {
                    var parameterInfo = methodInfo.GetParameters()[i];
                    parameters.AddParameter().FromSystemType(parameterInfo.ParameterType);
                }
            });
        var methodSignature = _metadataBuilder.GetOrAddBlob(blobBuilder);

        return _metadataBuilder.AddMethodDefinition(
            MethodAttributes.Public | MethodAttributes.Static,
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            _metadataBuilder.GetOrAddString(methodInfo.Name),
            methodSignature,
            MethodBody.None,
            parameterList: new ParameterHandle[parameterCount],
            declaringType: GetTypeDefHandle(typeof(Math)));
    }

    private TypeDefinitionHandle GetTypeDefHandle(Type type)
    {
        var typeName = type.FullName.Replace(".", "/");
        var typeDef = _metadataBuilder.GetTypeDefinition(
            _metadataBuilder.GetOrAddString(type.Namespace),
            _metadataBuilder.GetOrAddString(typeName)
        );

        if (typeDef.IsNil)
        {
            typeDef = _metadataBuilder.AddTypeDefinition(
                new TypeDefinitionAttributes(),
                _metadataBuilder.GetOrAddString(type.Namespace),
                _metadataBuilder.GetOrAddString(typeName),
                GetTypeRefHandle(typeof(object)),
                default(EntityHandle),
                Array.Empty<FieldDefinitionHandle>()
            );
        }

        return typeDef;
    }

    private TypeReferenceHandle GetTypeRefHandle(Type type)
    {
        string typeName;
        string namespaceName;

        if (type.IsGenericType)
        {
            var nameParts = type.GetGenericTypeDefinition().FullName.Split('.');
            typeName = string.Join("/", nameParts.Take(nameParts.Length - 1));
            namespaceName = type.Namespace;
        }
        else
        {
            typeName = type.FullName?.Replace(".", "/");
            namespaceName = type.Namespace;
        }

        // If type is part of mscorlib, use the cached handle.
        if (type.Assembly == typeof(object).Assembly)
        {
            var cacheKey = typeName + namespaceName;
            if (_typeRefHandleCache.TryGetValue(cacheKey, out var handle))
            {
                return handle;
            }
        }

        // Create type reference if it does not exist yet.
        var assemblyRef = _metadataBuilder.AddAssemblyReference(GetAssemblyName(type.Assembly));
        var typeRef = _metadataBuilder.GetTypeReference(assemblyRef, _metadataBuilder.GetOrAddString(namespaceName), _metadataBuilder.GetOrAddString(typeName));

        if (type.Assembly == typeof(object).Assembly)
        {
            var cacheKey = typeName + namespaceName;
            _typeRefHandleCache[cacheKey] = typeRef;
        }

        return typeRef;
    }

    private string GetAssemblyName(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (_assemblyNameCache.TryGetValue(name, out var assemblyName))
        {
            return assemblyName;
        }

        var bytes = assembly.GetName().GetPublicKey();
        if (bytes != null)
        {
            assemblyName = _metadataBuilder.GetOrAddAssemblyReference(
                _metadataBuilder.GetOrAddString(name),
                new Version(1, 0, 0, 0),
                _metadataBuilder.GetOrAddString("neutral"),
                _metadataBuilder.GetOrAddBlob(bytes),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle)
            );
        }
        else
        {
            assemblyName = _metadataBuilder.GetOrAddAssemblyReference(
                _metadataBuilder.GetOrAddString(name),
                new Version(1, 0, 0, 0),
                _metadataBuilder.GetOrAddString("neutral"),
                publicKeyOrToken: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle)
            );
        }

        _assemblyNameCache[name] = assemblyName;
        return assemblyName;
    }

    private void EmitFunctionDefinition(FunctionDefinitionNode node)
    {
        var returnType = node.ReturnType ?? typeof(void);
        var methodName = node.Identifier.Value;
        var typeDef = GetTypeDefHandle(node.GetType());
        var methodDef = DefineMethod(
            typeDef,
            methodName,
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            GetMethodSignature(returnType, node.ParameterTypes)
        );
        Generate(node.Body, methodDef);
    }

    private BlobHandle GetMethodSignature(Type returnType, Type[] parameterTypes)
    {
        var signature = new BlobBuilder();
        var returnTypeEncoder = new TypeEncoder(signature);

        returnTypeEncoder.Type(returnType);

        var parameters = new ParameterTypeEncoder(signature);
        parameters.Count(parameterTypes.Length);

        foreach (var parameterType in parameterTypes)
        {
            var parameterEncoder = parameters.AddParameter();
            parameterEncoder.Type(parameterType);
        }

        return signature.ToBlobHandle();
    }

    private void EmitPrintStatement(PrintStatementNode node, MethodDefinitionHandle method)
    {
        Generate(node.Expression, method);
        var consoleWriteLine = typeof(Console).GetMethod("WriteLine", new[] { typeof(object) });
        var consoleWriteLineHandle = GetMethodHandle(consoleWriteLine);
        _ilBuilder.Emit(OpCodes.Call, consoleWriteLineHandle);
    }

    private void EmitBinaryOperation(BinaryOperationNode node, MethodDefinitionHandle method)
    {
        Generate(node.Left, method);
        Generate(node.Right, method);

        switch (node.Operator)
        {
            case TokenType.Plus:
                _ilBuilder.Emit(OpCodes.Add);
                break;
            case TokenType.Minus:
                _ilBuilder.Emit(OpCodes.Sub);
                break;
            case TokenType.Multiply:
                _ilBuilder.Emit(OpCodes.Mul);
                break;
            case TokenType.Divide:
                _ilBuilder.Emit(OpCodes.Div);
                break;
            case TokenType.Power:
                var doubleType = GetTypeDefHandle(typeof(double));
                var mathType = GetTypeDefHandle(typeof(Math));
                var powMethod = mathType.Resolve().Methods.First(m =>
                    m.Name == nameof(Math.Pow) && m.Parameters.Count == 2 &&
                    m.Parameters[0].ParameterType.Name == doubleType.Resolve().Name);
                var powMethodHandle = GetMethodHandle(powMethod);
                _ilBuilder.Emit(OpCodes.Call, powMethodHandle);
                break;
            default:
                throw new NotSupportedException($"Operator {node.Operator} is not supported.");
        }
    }

    private void EmitParenthesizedExpression(ParenthesizedExpressionNode node, MethodDefinitionHandle method)
    {
        Generate(node.Expression, method);
    }

    private void EmitNumber(NumberNode node, MethodDefinitionHandle method)
    {
        _ilBuilder.Emit(OpCodes.Ldc_I4, node.Value);
    }

    private void EmitIdentifier(string name, MethodDefinitionHandle method)
    {
        var variableIndex = DefineLocal(typeof(int));
        _ilBuilder.Emit(OpCodes.Ldloc, variableIndex);
    }

    private void EmitAssignment(string name, MethodDefinitionHandle method)
    {
        // TODO: Implement assignment.
        throw new NotImplementedException();
    }

    private void EmitFunctionCall(FunctionCallNode node, MethodDefinitionHandle method)
    {
        var arguments = node.Arguments.Reverse().ToArray();

        foreach (var argument in arguments)
        {
            Generate(argument, method);
        }

        var functionDef = _functionTable[node.Identifier];
        _ilBuilder.Emit(OpCodes.Call, functionDef);
    }

    private MethodDefinitionHandle DefineMethod(TypeDefinitionHandle type, string name, MethodAttributes attributes, BlobHandle signature)
    {
        var method = _metadataBuilder.AddMethodDefinition(
            attributes,
            MethodImplAttributes.IL,
            _metadataBuilder.GetOrAddString(name),
            signature,
            MethodBody.NotInitialized);
        _metadataBuilder.AddMethodSemantics(method, MethodKind.Ordinary, MethodSemanticsAttributes.None);
        _metadataBuilder.AddTypeMethod(type, method);

        return method;
    }
}