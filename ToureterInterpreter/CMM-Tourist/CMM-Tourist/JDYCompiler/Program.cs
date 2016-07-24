using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace JDYCompiler
{
    public enum ToureterRunState { WAIT_INPUT,RUNNING,ENDED}
    public interface IInterpreter {
        void InputSource(string sourceCode);
        string LexicalAnalyze();
        string GrammerAnalyze();
        string SemanticAnalyze();
        string GetIntermediaCode();
        void ConsoleInput(string input);
        string Interprete();
        string GetConsoleOutput();
        ToureterRunState GetToureterRunningState();
        void AsynchronStart();
    }
    class JDYCompiler:IInterpreter
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

            public CodeReader CodeReader { get; set; }

            public void Print(TextBox error, RichTextBox console, TextBox LexicalAnalysis, TextBox SyntaxAnalysis, TextBox SemanticAnalysis, TextBox IntermediateCode)
            {
                try
                {
                    if (PrintLexerResult)
                    {
                        //Console.WriteLine("---Lexer Result as Follow---");
                        LexicalAnalysis.Text = "---Lexer Result as Follow---" + "\n";

                      //  printLexerResult(LexicalAnalysis);

                        //Console.WriteLine("---Lexer Result End---\r\n\r\n");
                        LexicalAnalysis.Text += "---Lexer Result End---\r\n\r\n" + "\n";

                    }

                    if (PrintGrammerTree)
                    {
                        //Console.WriteLine("---Grammer Tree as Follow---");
                        LexicalAnalysis.Text = "---Lexer Result as Follow---" + "\n";
                      //  printGrammerTree(Root, "");
                        Console.WriteLine("---Grammer Tree End---\r\n\r\n");
                    }

                    if (PrintSemanticResult)
                    {

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            public string printLexerResult(Tokens LexicalAnalysis)
            {
                StringBuilder sb = new StringBuilder();
                if (PrintWithSrcCode)
                {
                    int k;
                    for (int i = 0; i < CodeReader.Row; i++)
                    {
                        sb.Append(string.Format("{0}: {1}\n", i + 1, CodeReader.Lines[i]));
                      
                        //LexicalAnalysis.Text += (i + 1) + ": " + CodeReader.Lines[i] + "\n";
                        LexicalAnalysis.ForEach(c =>
                        {
                            if (c.Row == i + 1)
                               sb.Append((c.ToString()+"\n"));
                               // LexicalAnalysis.Text += c.ToString() + "\n";
                        });
                    }
                }
                else
                {
                    LexicalAnalysis.ForEach(c => sb.Append((c.ToString() + "\n")));
                    //this.Tokens.ForEach(c => LexicalAnalysis.Text += c.ToString() + "\n" );
                }
                return sb.ToString();
            }// LexicalAnalysis.text 修改
            public string printGrammerTree(GrammerTreeNode node,string padding) {
                StringBuilder sb = new StringBuilder();
                if (node.Type == TreeNodeType.NONTERMINAL)
                {
                    if (node.TerminalType != null)
                        sb.Append(string.Format("{0}{1}: {2}\n", padding,node.NonterminalType.ToString() , node.TerminalType.Image));
                    else 
                        sb.Append(string.Format("{0}{1}\n", padding, node.NonterminalType.ToString()));
                    
                }
                else {
                    if (node.TerminalType.Kind == TokenKind.INT_CONSTANT)
                         sb.Append(string.Format("{0}{1}{2}\n", padding, "Const: ", node.IntValue));
                    else if (node.TerminalType.Kind == TokenKind.REAL_CONSTANT)
                         sb.Append(string.Format("{0}{1}{2}\n", padding, "Const: ", node.RealValue));
                    else if (node.TerminalType.Kind == TokenKind.TRUE || node.TerminalType.Kind == TokenKind.FALSE)
                         sb.Append(string.Format("{0}{1}{2}\n", padding, "Const: ", node.TerminalType.Image));
                    else if (node.TerminalType.Kind == TokenKind.IDENTIFIER)
                         sb.Append(string.Format("{0}Id: {1}\n", padding, node.TerminalType.Image));
                    else if (new TokenKind[] { TokenKind.INTEGER, TokenKind.FLOAT, TokenKind.VOID, TokenKind.BOOLEAN }.Contains(node.TerminalType.Kind))
                         sb.Append(string.Format("{0}{1}{2}\n", padding, "Type: ", node.TerminalType.Image));
                    else if (new TokenKind[] { TokenKind.READ, TokenKind.WRITE, TokenKind.RETURN, TokenKind.IF, TokenKind.WHILE }.Contains(node.TerminalType.Kind))
                        sb.Append(string.Format("{0}{1}{2}\n", padding, "Keyword: ", node.TerminalType.Image));
                    else
                         sb.Append(string.Format("{0}{1}{2}\n", padding, "Operator: ", node.TerminalType.Image));
                }
                padding = string.Concat(padding, "   ") ;
                foreach (GrammerTreeNode child in node.Children)
                    sb.Append(printGrammerTree(child, padding));
                return sb.ToString();
            }
        }
        #endregion

        private CodeReader codeReader;
        private Tokens lexiResult;
        private GrammerTreeNode gramResult;
        private string semanResult;
        private ResultPrinter printer = new ResultPrinter();
        private string middleCode;
        private string mInputBuffer;
        private string mOutputBuffer;
        private ToureterRunState mRunningState;
        public void InputSource(string sourceCode) {
            codeReader = new CodeReader();
            printer.CodeReader = codeReader;
            codeReader.SetSrcCode(sourceCode);
        }
        public string LexicalAnalyze() {
            this.lexiResult = LexerAnalyzer.GetTokens(this.codeReader);
            return printer.printLexerResult(this.lexiResult);
        }
        public string GrammerAnalyze() {
            this.gramResult = GrammerAnalyzer.Parse((Tokens)lexiResult.Clone());
            return printer.printGrammerTree(this.gramResult,"");
        }
        public string SemanticAnalyze() {
           return CodeGenerator.symbolTable.st_print();
        }
        public string GetIntermediaCode() {
            CodeGenerator.symbolTable = new SymTab();
            CodeGenerator coder = new CodeGenerator(this.gramResult);
             middleCode = coder.codeGen();
            return middleCode;
        }
        public string Interprete() {
            CodeInterpreter interpreter = new CodeInterpreter();
            string result=interpreter.interprete(middleCode);
            return result;
        }
       public void AsynchronStart() {
            //this.mInputBuffer = "";
            //this.mOutputBuffer = "";
            //this.mRunningState = ToureterRunState.WAIT_INPUT;
         
        }
        public void ConsoleInput(string input) {
            this.mInputBuffer = input;
            this.mOutputBuffer = "徐洁斌你好帅";

            this.mRunningState = ToureterRunState.ENDED;
            
        }
        public string GetConsoleOutput() {
            string tmp = this.mOutputBuffer;
            this.mOutputBuffer = "";
            return tmp;
        }
        public ToureterRunState GetToureterRunningState() {
            return this.mRunningState; 
        }
        public static void MainG(TextBox error, RichTextBox console, TextBox LexicalAnalysis, TextBox SyntaxAnalysis, TextBox SemanticAnalysis, TextBox IntermediateCode)//
        {
            try
            {
                printStatus(console);
                waitCommandLoop(error,console,LexicalAnalysis,SyntaxAnalysis,SemanticAnalysis,IntermediateCode);
            }
            catch (Exception e) {
                //Console.WriteLine("Opps, 发生了异常,程序将退出.\n异常信息:{0}", e.Message);

                error.Text += "Opps, 发生了异常,程序将退出.\n异常信息: " + e.Message;
    
            }
            Console.ReadKey();
        }
        static void printStatus(RichTextBox console)
        {
            string padding = "                \n";
            //Console.WriteLine("JDYCompiler CMM语言解释器 Console版==");
            console.AppendText("**************************************\n");
            string isdone = LexerFinished ? "已完成" : "未完成";
            console.AppendText("词法分析模块: " + isdone + padding);
            
            isdone = GrammerFinished ? "已完成" : "未完成";
            console.AppendText("词法分析模块: " + isdone + padding);

            isdone = SemanticFinished ? "已完成" : "未完成";
            console.AppendText("语义分析模块: " + isdone + padding);

            console.AppendText("**************************************\n"); 
        }
        static void waitCommandLoop(TextBox error, RichTextBox console, TextBox LexicalAnalysis, TextBox SyntaxAnalysis, TextBox SemanticAnalysis, TextBox IntermediateCode)
        {
            //Console.WriteLine("Usage:输入demo{n}展示分析demo代码的结果. 输入help获得帮助信息");
            console.AppendText("Usage:输入demo{n}展示分析demo代码的结果. 输入help获得帮助信息" + "\n");            
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
                    Analysis(string.Format(@"样例代码\{0}.cmm", command.ToLower()), error, console, LexicalAnalysis, SyntaxAnalysis, SemanticAnalysis, IntermediateCode);
                }
                else
                {
                    command = Regex.Replace(command, @"[ ]+", " ");
                    string[] seg = command.Split(' ');
                    if (seg[0] == "lex")
                    {
                        DoGrammerAnalysis = false;
                        PrintGrammerTree = false;
                        Analysis(seg[1], error, console, LexicalAnalysis, SyntaxAnalysis, SemanticAnalysis, IntermediateCode);
                    }
                    else if (seg[0] == "gram") {
                        DoGrammerAnalysis = true ;
                        PrintGrammerTree = true;
                        Analysis(seg[1], error, console, LexicalAnalysis, SyntaxAnalysis, SemanticAnalysis, IntermediateCode);
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
        static void Analysis(string path, TextBox error, RichTextBox console, TextBox LexicalAnalysis, TextBox SyntaxAnalysis, TextBox SemanticAnalysis, TextBox IntermediateCode)
        {
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
                    coder.codeGen();
                    coder.print();
                    CodeGenerator.symbolTable.print();
                }

                ///some test
                
                //Console.WriteLine("{0}", SymTab.interpreteDimenExpression(root.Children[0].Children[0].Children[4].Children[0].Children[0].Children[2]));

              //  printer = new ResultPrinter() { CodeReader = codeReader, Tokens = tokens, Root = root };
                printer.Print(error, console, LexicalAnalysis, SyntaxAnalysis, SemanticAnalysis, IntermediateCode);

            }
            catch (ParseException e)
            {
                //Console.WriteLine(e.Message);
                error.Text += e.Message + "\n";
            }
            catch (Exception e) {
                //Console.WriteLine("opps, 发生了异常.异常信息: \n{0}", e.Message);
                error.Text += "opps, 发生了异常.异常信息: " + e.Message + "\n";
            }
        }           

    }



}
