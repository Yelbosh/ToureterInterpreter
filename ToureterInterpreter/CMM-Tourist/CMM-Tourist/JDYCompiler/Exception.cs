using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JDYCompiler
{
    internal class ParseException:Exception
    {
        public Token CurrentToken { get; private set; }
        public TokenKind[] Expected { get; private set; } 
        public ParseException(Token theToken, TokenKind[] expected) {
            this.CurrentToken = theToken;
            this.Expected = expected;
        }
       new public string Message { get {
            return string.Format("Unexpected token: '{0}', at Row: {1}, Colunm: {2}. {3} is/are expected",CurrentToken.Image,CurrentToken.Row,CurrentToken.Column,Expected.Aggregate("",(c,d)=>c+"'"+d.ToString()+"' ",e=>e)) ;
        }
        }
        public ParseException(Token theToken, TokenKind expected) {
            this.CurrentToken = theToken;
            this.Expected = new TokenKind[] { expected };
        }
    }
}
