# .NET Core compiler by ChatGPT

## The language spec

```
// this is comment
fn sum(x, y, z) {
  ret = x + y
  ret = ret + z
  // function returns a single value
  return ret
}

one = 1  // top-level statement
// print is a special operator.
// this prints "sum(sum(2.0, 1.5), 1) = 4.5". 4.5 is the result of the expression.
print! sum(sum(2.0, 1.5), one)
```

### BNF by ChatGPT

```
<expression> ::= <function-call> | <number> | <identifier> | <binary-operation> | <parenthesized-expression>
<function-call> ::= <identifier> "(" <arguments> ")"
<arguments> ::= <expression> ("," <expression>)*
<binary-operation> ::= <expression> <operator> <expression>
<operator> ::= "+" | "-" | "*" | "/" | "^"
<assignment> ::= <identifier> "=" <expression>
<statement> ::= <assignment> | <function-definition> | <print-statement>
<function-definition> ::= "fn" <identifier> "(" <arguments-list> ")" <block>
<arguments-list> ::= ( <identifier> ("," <identifier>)* )?
<block> ::= "{" <statement>* "}"
<print-statement> ::= "print!" <expression>
<program> ::= <statement>*
<identifier> ::= <letter> (<letter> | <digit>)*
<number> ::= <digit>+
<digit> ::= "0" | "1" | ... | "9"
<letter> ::= "A" | "B" | ... | "Z" | "a" | "b" | ... | "z"
<parenthesized-expression> ::= "(" <expression> ")"
```
