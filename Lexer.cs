using System.Text;

public enum TokenType { Number, Identifier, FunctionKeyword, PrintKeyword, Plus, Minus, Multiply, Divide, Power, OpenParenthesis, CloseParenthesis, EndOfInput }

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }

    public override string ToString()
    {
        return $"Token({Type}, {Value})";
    }
}

public class Lexer
{
    private readonly string _input;
    private int _position;

    public Lexer(string input)
    {
        _input = input;
        _position = 0;
    }

    public Token NextToken()
    {
        while (_position < _input.Length)
        {
            char currentChar = _input[_position];

            if (char.IsWhiteSpace(currentChar))
            {
                _position++;
                continue;
            }

            if (char.IsDigit(currentChar))
            {
                return TokenizeNumber();
            }

            if (char.IsLetter(currentChar))
            {
                return TokenizeIdentifierOrKeyword();
            }

            if (currentChar == '(')
            {
                _position++;
                return new Token(TokenType.OpenParenthesis, "(");
            }

            if (currentChar == ')')
            {
                _position++;
                return new Token(TokenType.CloseParenthesis, ")");
            }

            if (IsOperator(currentChar))
            {
                return TokenizeOperator();
            }

            throw new Exception($"Unknown token at position {_position}");
        }

        return new Token(TokenType.EndOfInput, "");
    }

    private Token TokenizeNumber()
    {
        StringBuilder sb = new StringBuilder();

        while (_position < _input.Length && char.IsDigit(_input[_position]))
        {
            sb.Append(_input[_position]);
            _position++;
        }

        return new Token(TokenType.Number, sb.ToString());
    }

    private Token TokenizeIdentifierOrKeyword()
    {
        StringBuilder sb = new StringBuilder();

        while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
        {
            sb.Append(_input[_position]);
            _position++;
        }

        string value = sb.ToString();

        if (value == "fn")
        {
            return new Token(TokenType.FunctionKeyword, value);
        }

        if (value == "print!")
        {
            return new Token(TokenType.PrintKeyword, value);
        }

        return new Token(TokenType.Identifier, value);
    }

    private Token TokenizeOperator()
    {
        char currentChar = _input[_position];

        if (currentChar == '+')
        {
            _position++;
            return new Token(TokenType.Plus, "+");
        }

        if (currentChar == '-')
        {
            _position++;
            return new Token(TokenType.Minus, "-");
        }

        // TODO: handle other operators

        throw new Exception($"Unknown operator at position {_position}");
    }

    private static bool IsOperator(char c)
    {
        return c == '+' || c == '-' || c == '*' || c == '/' || c == '^';
    }
}
