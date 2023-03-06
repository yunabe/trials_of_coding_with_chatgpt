using System.Text;

public class CodeGenerator
{
    private StringBuilder _output;

    public string GenerateCode(Node node)
    {
        _output = new StringBuilder();
        Visit(node);
        return _output.ToString();
    }

    private void Visit(Node node)
    {
        if (node is NumberNode numberNode)
        {
            VisitNumberNode(numberNode);
        }
        else if (node is IdentifierNode identifierNode)
        {
            VisitIdentifierNode(identifierNode);
        }
        else if (node is BinaryOperationNode binaryOperationNode)
        {
            VisitBinaryOperationNode(binaryOperationNode);
        }
        else if (node is ParenthesizedExpressionNode parenthesizedExpressionNode)
        {
            VisitParenthesizedExpressionNode(parenthesizedExpressionNode);
        }
        else if (node is FunctionCallNode functionCallNode)
        {
            VisitFunctionCallNode(functionCallNode);
        }
        else
        {
            throw new Exception($"Invalid node type: {node.GetType().Name}.");
        }
    }

    private void VisitNumberNode(NumberNode node)
    {
        _output.Append(node.Value);
    }

    private void VisitIdentifierNode(IdentifierNode node)
    {
        _output.Append(node.Value);
    }

    private void VisitBinaryOperationNode(BinaryOperationNode node)
    {
        Visit(node.Left);
        _output.Append(TokenToOperator(node.Operator));
        Visit(node.Right);
    }

    private void VisitParenthesizedExpressionNode(ParenthesizedExpressionNode node)
    {
        _output.Append("(");
        Visit(node.Expression);
        _output.Append(")");
    }

    private void VisitFunctionCallNode(FunctionCallNode node)
    {
        _output.Append(node.Identifier);
        _output.Append("(");

        for (int i = 0; i < node.Arguments.Length; i++)
        {
            Visit(node.Arguments[i]);

            if (i < node.Arguments.Length - 1)
            {
                _output.Append(", ");
            }
        }

        _output.Append(")");
    }

    private string TokenToOperator(TokenType tokenType)
    {
        switch (tokenType)
        {
            case TokenType.Plus:
                return "+";
            case TokenType.Minus:
                return "-";
            case TokenType.Multiply:
                return "*";
            case TokenType.Divide:
                return "/";
            case TokenType.Power:
                return "^";
            default:
                throw new Exception($"Invalid operator token type: {tokenType}.");
        }
    }
}