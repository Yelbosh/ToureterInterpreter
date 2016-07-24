using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
namespace JDYCompiler
{
   internal class LexerAnalyzer
   {
       #region
       /// <summary>
       /// predefined dictionary of reserved words and their enum type
       /// </summary>
       private static readonly Dictionary<string, TokenKind> reserveWords = new Dictionary<string, TokenKind>(){
           /*basic*/
           {"if",TokenKind.IF},
           {"else",TokenKind.ELSE},
           {"while",TokenKind.WHILE},
           {"read",TokenKind.READ},
           {"write",TokenKind.WRITE},
           {"int",TokenKind.INTEGER},
           {"real",TokenKind.FLOAT},
           {"bool",TokenKind.BOOLEAN},
           {"true",TokenKind.TRUE},
           {"false",TokenKind.FALSE},
           /*extension*/
           {"function",TokenKind.FUNCTION},
           {"class",TokenKind.CLASS},
           {"struct",TokenKind.STRUCT},
           {"return",TokenKind.RETURN},
           {"void",TokenKind.VOID},
           {"ref",TokenKind.REF}
       };
       #endregion

       public static Tokens GetTokens(CodeReader codeReader) {
           Tokens tokens = new Tokens();
           int a = 0, b = 0;
           char nextChar;
           while ((nextChar=codeReader.NextChar()) != '\0') {
               Token newToken = null;
               bool Recongnized = true;
               switch (nextChar)
               {
                   case '*': newToken = new Token("*", TokenKind.MULTIPLY); break;
                   case '(': newToken = new Token("(", TokenKind.LPARENT); break;
                   case ')': newToken = new Token(")", TokenKind.RPARENT); break;
                   case '{': newToken = new Token("{", TokenKind.OBRACE); break;
                   case '}': newToken = new Token("}", TokenKind.CBRACE); break;
                   case '[': newToken = new Token("[", TokenKind.OBRAKET); break;
                   case ']': newToken = new Token("]", TokenKind.CBRAKET); break;
                   case ';': newToken = new Token(";", TokenKind.SEMI); break;
                   case ',': newToken = new Token(",", TokenKind.COMMA); break;
                   case '^': newToken = new Token("^", TokenKind.BITXOR); break;
                   case '!': newToken = new Token("!", TokenKind.NOT); break;
                   case '%': newToken = new Token("%", TokenKind.MOD); break;
                   case '~': newToken = new Token("~", TokenKind.BITNOT); break;
                   //for struct member operator
                   case '.': newToken = new Token(".", TokenKind.DOT); break;
                   case '&':
                       char prev = codeReader.NextChar();
                       if (prev == '&')
                       {
                           newToken = new Token("&&", TokenKind.AND);
                       }
                       else {
                           newToken = new Token("&", TokenKind.BITAND);
                           codeReader.PtrMovBack();
                       }
                       break;
                   case '+':
                       prev = codeReader.NextChar();
                       if (prev == '+')
                           newToken = new Token("++", TokenKind.INCREMENT);
                       else if (prev == '=')
                           newToken = new Token("+=", TokenKind.INCREASSGIN);
                       else
                       {
                           newToken = new Token("+", TokenKind.PLUS);
                           codeReader.PtrMovBack();
                       }
                       break;
                   case '-':
                       prev = codeReader.NextChar();
                       if (prev == '-')
                       {
                           newToken = new Token("--", TokenKind.DECREMENT);
                       }
                       else if (prev == '=') {
                           newToken = new Token("-=", TokenKind.DECREASSGIN);
                       }
                       else
                       {
                           newToken = new Token("-", TokenKind.MINUS);
                           codeReader.PtrMovBack();
                       }
                       break;
                   case '|':
                       prev = codeReader.NextChar();
                       if (prev == '|')
                       {
                           newToken = new Token("||", TokenKind.OR);
                       }
                       else {
                           newToken = new Token("|", TokenKind.BITOR);
                           codeReader.PtrMovBack();
                       }
                       break;
          
                   case '/':
                       prev = codeReader.NextChar();
                       if (prev == '*' || prev == '/')
                       {//注释
                           codeReader.SkipComments();
                       }
                       else
                       {
                           newToken = new Token("/", TokenKind.DIVIDE);
                           codeReader.PtrMovBack();
                       }
                       break;
                   case '>':
                       prev = codeReader.NextChar();
                       if (prev == '=')
                           newToken = new Token(">=", TokenKind.GOE);
                       else
                       {
                           newToken = new Token(">", TokenKind.GT);
                           codeReader.PtrMovBack();
                       }
                       break;
                   case '<':
                       prev = codeReader.NextChar();
                       if (prev == '>')
                           newToken = new Token("<>", TokenKind.NEQ);
                       else if (prev == '=')
                           newToken = new Token("<=", TokenKind.LOE);
                       else
                       {
                           codeReader.PtrMovBack();
                           newToken = new Token("<", TokenKind.LT);
                       }
                       break;
                   case '=':
                       prev = codeReader.NextChar();
                       if (prev == '=')
                           newToken = new Token("==", TokenKind.EQ);
                       else
                       {
                           codeReader.PtrMovBack();
                           newToken = new Token("=", TokenKind.ASSIGN);
                       }
                       break;
                   default:
                       TokenKind kind = TokenKind.UNDETERMINED;
                       StringBuilder image = new StringBuilder();
                       if (nextChar == '_' || nextChar >= 'A' && nextChar <= 'Z' || nextChar >= 'a' && nextChar <= 'z' || nextChar == '$')
                       { //parse identifier
                           do
                           {
                               image.Append(nextChar);
                               nextChar = codeReader.NextChar();
                           }
                           while (nextChar == '_' || nextChar >= 'A' && nextChar <= 'Z' || nextChar >= 'a' && nextChar <= 'z' || nextChar == '$' || nextChar >= '0' && nextChar <= '9');
                           codeReader.PtrMovBack();
                           if (reserveWords.ContainsKey(image.ToString())) //if is reserved word
                               kind = reserveWords[image.ToString()];
                           else
                               kind = TokenKind.IDENTIFIER;
                       }
                       else if (nextChar >= '0' && nextChar <= '9' || nextChar == '.')
                       {
                           bool beforeDot = true;
                           bool beforeExpo = true;
                           while (true)
                           {
                               if (nextChar == '.')
                                   if (beforeDot)
                                   {
                                       beforeDot = false;
                                   }
                                   else
                                   {
                                       Recongnized = false;
                                       codeReader.PtrMovBack();
                                   }
                               else if (nextChar == 'e')
                                   if (beforeExpo)
                                   {
                                       beforeExpo = false;
                                       beforeDot = true;
                                   }
                                   else
                                   {
                                       Recongnized = false;
                                       codeReader.PtrMovBack();
                                   }
                               else if (nextChar < '0' || nextChar > '9')
                               {
                                   Recongnized = false;
                                   codeReader.PtrMovBack();
                               }

                               if (Recongnized)
                               {
                                   image.Append(nextChar);
                                   nextChar = codeReader.NextChar();
                               }
                               else
                               {
                                   break;
                               }
                           }
                           kind = beforeDot&&beforeExpo ? TokenKind.INT_CONSTANT : TokenKind.REAL_CONSTANT;
                       }
                       if (image.Length > 0) {
                           newToken = new Token(image.ToString(), kind);
                       }
                       break;
               }

               if (newToken != null)
               {
                   tokens.Add(newToken);
                   newToken.Column = codeReader.Column-newToken.Image.Length;
                   newToken.Row = codeReader.Row;
               }
           }
           tokens.Add(new Token("", TokenKind.EOF){Column=codeReader.Column,Row=codeReader.Row});
           return tokens;
       }
    }

   internal class CodeReader {

       public int Column { get { return col+1 ; } }
       public int Row { get { return row + 1; } }
       public char Current { get { return this.codeBuf[readPointer]; } }
       public  List<string> Lines{get;private set;}

       private int col;
       private int row;
       private int readPointer;
       private StreamReader srcFile;
       private string codeBuf;
       public CodeReader()
       {
           this.readPointer = 0;
           this.row = 0;
           this.col = 0;
         
       }
       public CodeReader(string SrcPath) {
           if (File.Exists(SrcPath)) 
               this.srcFile = new StreamReader(SrcPath);
           else
               throw new FileNotFoundException(string.Format("代码文件{0}不存在,请检查路径",SrcPath));
           this.codeBuf = srcFile.ReadToEnd();
           this.codeBuf = this.codeBuf.Replace("\r\n", "\n");
           this.readPointer = 0;
           this.row = 0;
           this.col = 0;
           this.Lines = new List<string>(codeBuf.Split('\n'));
       }
       public void PtrMovBack() {
           if (this.col ==1)
           {
               this.row--;
               this.col = Lines[row].Length - 1;
               while (codeBuf[--readPointer] != '\n') ;

           }
           else
           {
               readPointer--;
               col--;
           }
       }
       public void SkipComments() {
           this.PtrMovBack();
           if (this.NextChar() == '*')
           {
               char current;
               do
               {
                   do
                   {
                       current = this.NextChar();
                   }
                   while (current != '*' && current != '\0');
                   if ((current=NextChar()) == '/' || current == '\0')
                   {
                       break;
                   }
               } while (true);
           }
           else 
           {
               this.SkipALine();
           }
       }
       protected void SkipALine() {
          readPointer= this.codeBuf.IndexOf('\n', readPointer);
       }
       public char NextChar() {
           while ( readPointer < codeBuf.Length&&this.codeBuf[readPointer] == '\n' )
           {
               readPointer++;
               row += 1;
               col = 0;
           }

           if (readPointer < codeBuf.Length)
           {
               this.col++;
               return this.codeBuf[readPointer++];
           }
           else
               return '\0';
       }

       internal void SetSrcCode(string sourceCode)
       {
           this.codeBuf = sourceCode;
           this.Lines = new List<string>(codeBuf.Split('\n'));
       }
   }
}
