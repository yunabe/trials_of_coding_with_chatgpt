public class Parser
{
    private readonly List<Token> _tokens;
    private int _position;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    public Node Parse()
    {
        return ParseExpression();
    }

    private Node ParseExpression()
    {
        if (IsFunctionCall())
        {
            return ParseFunctionCall();
        }
        else if (IsNumber())
        {
            return ParseNumber();
        }
        else if (IsIdentifier())
        {
            return ParseIdentifier();
        }
        else if (IsBinaryOperation())
        {
            return ParseBinaryOperation();
        }
        else if (IsParenthesizedExpression())
        {
            return ParseParenthesizedExpression();
        }
        else
        {
            throw new Exception("Unexpected token: " + _tokens[_position]);
        }
    }

    private bool IsFunctionCall()
    {
        return IsIdentifier() && PeekNextToken().Type == TokenType.OpenParenthesis;
    }

    private Node ParseFunctionCall()
    {
        var identifier = ParseIdentifier();
        ConsumeToken(TokenType.OpenParenthesis);
        var arguments = ParseArgumentList().ToArray();
        ConsumeToken(TokenType.CloseParenthesis);
        return new FunctionCallNode(identifier.Value, arguments);
    }

    private bool IsNumber()
    {
        return PeekToken().Type == TokenType.Number;
    }

    private Node ParseNumber()
    {
        var token = ConsumeToken(TokenType.Number);
        return new NumberNode(int.Parse(token.Value));
    }

    private bool IsIdentifier()
    {
        return PeekToken().Type == TokenType.Identifier;
    }

    private Node ParseIdentifier()
    {
        var token = ConsumeToken(TokenType.Identifier);
        return new IdentifierNode(token.Value);
    }

    private bool IsBinaryOperation()
    {
        if (!IsExpression())
        {
            return false;
        }

        var nextToken = PeekNextToken();

        if (nextToken.Type != TokenType.Plus &&
            nextToken.Type != TokenType.Minus &&
            nextToken.Type != TokenType.Multiply &&
            nextToken.Type != TokenType.Divide &&
            nextToken.Type != TokenType.Power)
        {
            return false;
        }

        return true;
    }

    private Node ParseBinaryOperation()
    {
        var left = ParseExpression();
        var token = ConsumeToken(TokenCategory.Operator);
        var right = ParseExpression();
        return new BinaryOperationNode(token.Type, left, right);
    }

    private bool IsParenthesizedExpression()
    {
        return PeekToken().Type == TokenType.OpenParenthesis;
    }

    private Node ParseParenthesizedExpression()
    {
        ConsumeToken(TokenType.OpenParenthesis);
        var expression = ParseExpression();
        ConsumeToken(TokenType.CloseParenthesis);
        return new ParenthesizedExpressionNode(expression);
    }

    private List<Node> ParseArgumentList()
    {
        var list = new List<Node>();

        if (PeekToken().Type == TokenType.CloseParenthesis)
        {
            return list;
        }

        do
        {
            list.Add(ParseExpression());
        } while (ConsumeOptionalToken(TokenType.Comma) != null);

        return list;
    }

    private Token PeekToken()
    {
        if (_position >= _tokens.Count)
        {
            return new Token("", TokenType.EndOfInput);
        }

        return _tokens[_position];
    }

    private Token PeekNextToken()
    {
        if (_position + 1 >= _tokens.Count)
        {
            return new Token("", TokenType.EndOfInput);
        }

        return _tokens[_position + 1];
    }

    private Token ConsumeToken(TokenType type)
    {
        var token = PeekToken();

        if (token.Type != type)
        {
            throw new Exception($"Expected {type}, but found {token.Type}.");
        }

        _position++;
        return token;
    }

    private bool ConsumeOptionalToken(TokenType type)
    {
        var token = PeekToken();

        if (token.Type == type)
        {
            _position++;
            return true;
        }
        else
        {
            return false;
        }
    }
}

public abstract class Node
{
    public abstract dynamic Evaluate();
}

public class FunctionCallNode : Node
{
    public string Identifier { get; }
    public Node[] Arguments { get; }

    public FunctionCallNode(string identifier, Node[] arguments)
    {
        Identifier = identifier;
        Arguments = arguments;
    }

    public override dynamic Evaluate()
    {
        // TODO: Implement function call evaluation.
        throw new NotImplementedException();
    }
}

public class NumberNode : Node
{
    public int Value { get; }

    public NumberNode(int value)
    {
        Value = value;
    }

    public override dynamic Evaluate()
    {
        return Value;
    }
}

public class IdentifierNode : Node
{
    public string Value { get; }

    public IdentifierNode(string value)
    {
        Value = value;
    }

    public override dynamic Evaluate()
    {
        // TODO: Implement identifier evaluation.
        throw new NotImplementedException();
    }
}

public class BinaryOperationNode : Node
{
    public TokenType Operator { get; }
    public Node Left { get; }
    public Node Right { get; }

    public BinaryOperationNode(TokenType @operator, Node left, Node right)
    {
        Operator = @operator;
        Left = left;
        Right = right;
    }

    public override dynamic Evaluate()
    {
        var leftValue = Left.Evaluate();
        var rightValue = Right.Evaluate();

        switch (Operator)
        {
            case TokenType.Plus:
                return leftValue + rightValue;
            case TokenType.Minus:
                return leftValue - rightValue;
            case TokenType.Multiply:
                return leftValue * rightValue;
            case TokenType.Divide:
                return leftValue / rightValue;
            case TokenType.Power:
                return Math.Pow(leftValue, rightValue);
            default:
                throw new Exception($"Invalid operator: {Operator}.");
        }
    }
}

public class ParenthesizedExpressionNode : Node
{
    public Node Expression { get; }

    public ParenthesizedExpressionNode(Node expression)
    {
        Expression = expression;
    }

    public override dynamic Evaluate()
    {
        return Expression.Evaluate();
    }
}