using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JDYCompiler
{
    class JDYCompiler
    {
        #region JDYComplier状态信息
        public const bool LexerFinished = true;
        public const bool GrammerFinished = true;
        public const bool SemanticFinished = false;
        #endregion

        #region JDYCompiler开关
        public static bool DoLexerAnalysis = true;
        public static bool DoGrammerAnalysis = true;
        public static bool DoSemanticAnalysis = true;
        public static bool DoCodeGenerate = true;
        public static bool PrintWithSrcCode = true;
        public static bool PrintLexerResult = true;
        public static bool PrintGrammerTree = true;
        public static bool PrintSemanticResult = false;
        public static bool TraceCode = true;
        #endregion

        #region JDYInterpreter语法分析参数
        public const int LookAhead = 2;//按照LL2分析,预取两个TOKEN.
        #endregion

        #region internal class: ResultPrinter
        internal sealed class ResultPrinter
        {
            public GrammerTreeNode Root { get; set; }
            public Tokens Tokens { get; set; }
            public CodeReader CodeReader { get; set; }

            public void Print()
            {
                try
                {
                    if (PrintLexerResult)
                    {
                        Console.WriteLine("---Lexer Result as Follow---");
                        printLexerResult();
                        Console.WriteLine("---Lexer Result End---\r\n\r\n");
                    }
                   
                    if (PrintGrammerTree)
                    {
                        Console.WriteLine("---Grammer Tree as Follow---");
                        printGrammerTree(Root,"");
                        Console.WriteLine("---Grammer Tree End---\r\n\r\n");
                    }
                    
                    if (PrintSemanticResult)
                    {

                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
            private void printLexerResult() {
                if (PrintWithSrcCode)
                {
                    for (int i = 0; i < CodeReader.Row; i++)
                    {
                        Console.WriteLine("{0}: {1}", i + 1, CodeReader.Lines[i]);
                        this.Tokens.ForEach(c =>
                        {
                            if (c.Row == i + 1)
                                Console.WriteLine(c.ToString());
                        });
                    }
                }
                else
                {
                    this.Tokens.ForEach(c => Console.WriteLine(c.ToString()));
                }
            }
            private void printGrammerTree(GrammerTreeNode node,string padding) {
                if (node.Type == TreeNodeType.NONTERMINAL)
                {
                    if (node.TerminalType != null)
                        Console.WriteLine("{0}{1}: {2}", padding,node.NonterminalType.ToString() , node.TerminalType.Image);
                    else 
                        Console.WriteLine("{0}{1}", padding, node.NonterminalType.ToString());
                    
                }
                else {
                    if (node.TerminalType.Kind == TokenKind.INT_CONSTANT)
                        Console.WriteLine("{0}{1}{2}", padding, "Const: ", node.IntValue);
                    else if (node.TerminalType.Kind == TokenKind.REAL_CONSTANT)
                        Console.WriteLine("{0}{1}{2}", padding, "Const: ", node.RealValue);
                    else if (node.TerminalType.Kind == TokenKind.TRUE || node.TerminalType.Kind == TokenKind.FALSE)
                        Console.WriteLine("{0}{1}{2}", padding, "Const: ", node.TerminalType.Image);
                    else if (node.TerminalType.Kind == TokenKind.IDENTIFIER)
                        Console.WriteLine("{0}Id: {1}", padding, node.TerminalType.Image);
                    else if (new TokenKind[] { TokenKind.INTEGER, TokenKind.FLOAT, TokenKind.VOID, TokenKind.BOOLEAN }.Contains(node.TerminalType.Kind))
                        Console.WriteLine("{0}{1}{2}", padding, "Type: ", node.TerminalType.Image);
                    else if (new TokenKind[] { TokenKind.READ, TokenKind.WRITE, TokenKind.RETURN, TokenKind.IF, TokenKind.WHILE }.Contains(node.TerminalType.Kind))
                        Console.WriteLine("{0}{1}{2}", padding, "Keyword: ", node.TerminalType.Image);
                    else
                        Console.WriteLine("{0}{1}{2}", padding, "Operator: ", node.TerminalType.Image);
                }
                padding = string.Concat(padding, "   ") ;
                foreach (GrammerTreeNode child in node.Children)
                    printGrammerTree(child, padding);
            }
        }
        #endregion


        static void Main(string[] args)
        {
            try
            {
                printStatus();
                waitCommandLoop();
            }
            catch (Exception e) {
                Console.WriteLine("Opps, 发生了异常,程序将退出.\n异常信息:{0}", e.Message);
            }
            Console.ReadKey();
        }
        static void printStatus() {
            string padding = "                ";
            Console.WriteLine("JDYCompiler CMM语言解释器 Console版==");
            Console.WriteLine("**************************************");
            Console.WriteLine("*词法分析模块: {0}{1}*", LexerFinished ? "已完成" : "未完成", padding);
            Console.WriteLine("*语法分析模块: {0}{1}*", GrammerFinished ? "已完成" : "未完成", padding);
            Console.WriteLine("*语义分析模块: {0}{1}*", SemanticFinished ? "已完成" : "未完成", padding);
            Console.WriteLine("**************************************");
        }
        static void waitCommandLoop() {
            Console.WriteLine("Usage:输入demo{n}展示分析demo代码的结果. 输入help获得帮助信息");
            string command;
            //while(true){
                //command=Console.ReadLine();
                command = "demo1";
                if (command.ToLower() == "help") {
                    help();
                }
                else if (command.ToLower() == "quit")
                {
                    //break;
                }
                else if(Regex.IsMatch(command.ToLower(),@"^demo[0-9]$")){
                    DoGrammerAnalysis = true;
                    PrintGrammerTree = true;
                    Analysis(string.Format(@"样例代码\{0}.cmm", command.ToLower()));
                }
                else
                {
                    command = Regex.Replace(command, @"[ ]+", " ");
                    string[] seg = command.Split(' ');
                    if (seg[0] == "lex")
                    {
                        DoGrammerAnalysis = false;
                        PrintGrammerTree = false;
                        Analysis(seg[1]);
                    }
                    else if (seg[0] == "gram") {
                        DoGrammerAnalysis = true ;
                        PrintGrammerTree = true;
                        Analysis(seg[1]);
                    }
                    else
                    {
                        Console.WriteLine("未识别的命令.请输入help获得帮助");
                    }
                //}
            }
        }
        static void help(){
            Console.WriteLine("\nhelp 获得使用信息");
            Console.WriteLine("demo{n}\t\t分析第n个demo代码的结果,如输入\"demo1\"分析目录下的demo1.cmm)");
            Console.WriteLine("lex [Src_Path]  对指定文件进行词法解析");
            Console.WriteLine("gram [Src_Path] 对指定文件进行词法语法解析");
            Console.WriteLine("quit \t\t退出JDYCompiler\n");
        }
        

        static void Analysis(string path) {
            try
            {
                CodeReader codeReader = new CodeReader(path);
                Tokens tokens = null;
                GrammerTreeNode root = null;
                ResultPrinter printer = null;

                if (DoLexerAnalysis)
                    tokens = LexerAnalyzer.GetTokens(codeReader);
                if (DoGrammerAnalysis)
                    root = GrammerAnalyzer.Parse((Tokens)tokens.Clone());
                if (DoSemanticAnalysis) ;
                if (DoCodeGenerate) {
                    CodeGenerator coder = new CodeGenerator(root);
                    string tem = coder.codeGen();
                    coder.print();
                    CodeGenerator.symbolTable.print();
                }

                ///some test
                
                //Console.WriteLine("{0}", SymTab.interpreteDimenExpression(root.Children[0].Children[0].Children[4].Children[0].Children[0].Children[2]));

                printer = new ResultPrinter() { CodeReader = codeReader, Tokens = tokens, Root = root };
                printer.Print();

            }
            catch (ParseException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e) {
                Console.WriteLine("opps, 发生了异常.异常信息: \n{0}", e.Message);
            }
        }           

    }



}
