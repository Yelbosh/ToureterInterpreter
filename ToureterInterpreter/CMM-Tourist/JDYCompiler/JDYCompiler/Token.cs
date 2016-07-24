using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JDYCompiler
{
    internal  enum TokenKind
    {
        /* operator*/
        PLUS, MINUS, MULTIPLY, DIVIDE,MOD, GT,GOE, LT,LOE, EQ, NEQ, ASSIGN, LPARENT, RPARENT, OBRACE, CBRACE, OBRAKET, CBRAKET, SEMI, COMMA, AND, OR, NOT, BITAND, BITOR, BITXOR,BITNOT,INCREMENT,DECREMENT,INCREASSGIN,DECREASSGIN,
        /*reserved words */
        IF, ELSE, WHILE, READ, WRITE, INTEGER, FLOAT, BOOLEAN, TRUE, FALSE,
        /*extension*/
        FUNCTION, CLASS, STRUCT,RETURN,VOID,REF,DOT,//for member operator
        /* tag */
        INT_CONSTANT, REAL_CONSTANT, IDENTIFIER, EOF,
        /*error tag*/
        UNDETERMINED
        
    }

   internal sealed class Token:ICloneable
    {
       //token内容
       public string Image { get; private set; }

       public int Column { get;  set; }

       public int Row { get;  set; }

       public TokenKind Kind { get; private set; }

       public Token(string image, TokenKind kind)
       {
           this.Image = image;
           this.Kind = kind;
       }

       public override string ToString()
       {
           string type="";
           if (this.Kind < TokenKind.INT_CONSTANT && this.Kind > TokenKind.OR)
               type = " reserved word:";
           else if (this.Kind == TokenKind.INT_CONSTANT)
               type = " int const:";
           else if (this.Kind == TokenKind.REAL_CONSTANT)
               type = " real const:";
           else if (this.Kind == TokenKind.IDENTIFIER)
               type = " ID:";
           else if (this.Kind == TokenKind.EOF)
               type = " EOF";
           return string.Format("     {0}:{1} {2}", this.Row, type, this.Image);
       }

       public object Clone()
       {
           Token newToken = new Token(this.Image, this.Kind) { Row = this.Row, Column = this.Column };
           return newToken;
       }
    }

    internal sealed class Tokens:List<Token>,ICloneable{
        private int index=0;
        private Token[] buffer = new Token[JDYCompiler.LookAhead];

        public Tokens(IEnumerable<Token> tokens=null){
            if(tokens!=null)
                this.AddRange(tokens);
        }


        /// <summary>
        /// consume
        /// </summary>
        /// <param name="tokenKind"></param>
        /// <returns></returns>
        public Token Consume(TokenKind tokenKind) {
            Token first = this.First();
            if (tokenKind == first.Kind) {
                this.RemoveAt(0);
                index = index + 1 >= JDYCompiler.LookAhead ? 0 : index + 1;
                this.buffer[index] = first;
                return first;
            }
            throw new ParseException(first, tokenKind);
        }
        /// <summary>
        /// consume
        /// </summary>
        /// <param name="tokenKinds"></param>
        /// <returns></returns>
        public Token Consume(TokenKind[] tokenKinds) {
            Token first = this.First();
            foreach (TokenKind tokenKind in tokenKinds)
                if (tokenKind == first.Kind)
                {
                    this.RemoveAt(0);
                    index = index + 1 >= JDYCompiler.LookAhead ? 0 : index + 1;
                    this.buffer[index] = first;
                    return first;
                }
            throw new ParseException(first, tokenKinds);
        }

        public bool TestNextToken(TokenKind tokenKind,int lookAhead=0) { //nonconsume
            return this[lookAhead].Kind == tokenKind;
        }

        public bool TestNextToken(TokenKind[] tokenKinds, int lookAhead = 0)
        { //nonconsume
            Token first = this[lookAhead];
            foreach (TokenKind tokenKind in tokenKinds)
                if (tokenKind == first.Kind)
                {
                    return true;
                }
            return false;
        }

        public object Clone()
        {
            return new Tokens(this.Select(c =>(Token) c.Clone()));
        }

        
    }
}
