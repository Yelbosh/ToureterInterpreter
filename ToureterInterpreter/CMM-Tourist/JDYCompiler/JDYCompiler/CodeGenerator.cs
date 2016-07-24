using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;

namespace JDYCompiler
{
    internal sealed class CodeGenerator
    {
        //public static members
        #region
        public static GrammerTreeNode root;//the tree root

        public static int location = 0;//memory location
        public static int tmpOffset = 0;//relative location for the tmp vars
        public static int pc = 7;
        public static int mp = 6;
        public static int gp = 5;
        public static int ac = 0;
        public static int ac1 = 1;

        public static int emitLoc = 0;
        public static int highEmitLoc = 0;
        public static int rtnInsLoc;//to store the backup loc
        //symbol table for struct and function
        public static SymTab globalTable;
        //current symbol table for symbol 
        public static SymTab symbolTable;
        //symbol list
        public static List<SymTab> tableList;
        #endregion
        private string codeString;//中间代码的string表示
        public CodeGenerator(GrammerTreeNode node)
        {
            //initialize the static 
            root = node;
            globalTable = new SymTab();
            tableList = new List<SymTab>();
            //add the global table to the tablelist
            tableList.Add(globalTable);
            codeString = "";
        }
        //return the gen result in string
        public string codeGen() {
            emitComment("*This is the interminal code:\r\n");
            emitComment("Standard prelude:\r\n");
            emitRM("LD", mp, 0, ac, "load maxaddress from location 0");
            emitRM("ST", ac, 0, ac, "clear location 0");
            emitComment("End of standard prelude.");
            emitSkip(1);
            cGen();//invoke the driver
            int crnLoc = emitSkip(0);
            emitBackup(rtnInsLoc);
            emitRM("LDC",ac,crnLoc,0,"Load the return address");
            emitBackup(rtnInsLoc+1);
            emitRM("ST",ac,1,gp,"store the return address");
            emitRestore();
            emitRO("HALT",0,0,0,"end of the program"); 
            return this.codeString;
        }

        //print the result to file
        public void print() {
            StreamWriter writer = new StreamWriter("code.tm");
            writer.Write(codeString);//write the result to the selected file
            writer.Flush();//remember to flush the outputstream
            writer.Close();
        }

        //code generate driver
        public void cGen() {
            if (root != null) {
                if (root.Type == TreeNodeType.NONTERMINAL && root.NonterminalType == NonterminalType.START)
                {
                    foreach (GrammerTreeNode node in root.Children)
                        genStmt(node,"");
                }
            }
        }
        ///generate the statement Nonterminal
        ///GrammmerTreeNode type is as follows:
        ///         level 1:
        ///START,
        ///STRUCT,CLASS,
        ///         level 2:
        ///STMT_SEQUENCE,STMT, DECLARE_STMT, IF_STMT, WHILE_STMT, 
        ///ASSGIN_STMT, RETURN_STMT, WRITE_STMT,READ_STMT,FUNCTION_CALL_STMT, 
        ///FUNCTION_DEFINE_STMT,
        ///STRUCT_DEC_STMT,STRUCT_ASSIGN_STMT
        public void genStmt(GrammerTreeNode node,string funName) {
            if (node.Type == TreeNodeType.NONTERMINAL) {
                switch (node.NonterminalType) { 
                    case NonterminalType.CLASS:
                        genClass(node,funName);
                        break;
                    case NonterminalType.STRUCT:
                        genStruct(node, funName);
                        break;
                    case NonterminalType.FUNCTION_DEFINE_STMT:
                        genFunctionDefineStmt(node, funName);
                        break;
                    case NonterminalType.STMT_SEQUENCE:
                        genStmtSequence(node, funName);
                        break;
                    case NonterminalType.STMT:
                        genStmt(node.Children[0], funName);
                        break;
                    case NonterminalType.IF_STMT:
                        genIfStmt(node, funName);
                        break;
                    case NonterminalType.WHILE_STMT:
                        genWhileStmt(node, funName);
                        break;
                    case NonterminalType.DECLARE_STMT:
                        genDeclareStmt(node, funName);
                        break;
                    case NonterminalType.READ_STMT:
                        genReadStmt(node, funName);
                        break;
                    case NonterminalType.WRITE_STMT:
                        genWriteStmt(node, funName);
                        break;
                    case NonterminalType.FUNCTION_CALL_STMT:
                        genFunctionCallStmt(node, funName);
                        break;
                    case NonterminalType.RETURN_STMT:
                        genReturnStmt(node, funName);
                        break;
                    case NonterminalType.ASSGIN_STMT:
                        genAssignStmt(node, funName);
                        break;
                    case NonterminalType.STRUCT_DEC_STMT:
                        genStructDecStmt(node, funName);
                        break;
                    case NonterminalType.STRUCT_ASSIGN_STMT:
                        genStructAssignStmt(node, funName);
                        break;
                }
            }
        }

        ///the driver for the genStmt function
        ///genClass
        public void genClass(GrammerTreeNode node, string funName)
        {
            foreach (GrammerTreeNode subNode in node.Children) {
                if ((subNode.Type == TreeNodeType.NONTERMINAL) && (subNode.NonterminalType == NonterminalType.FUNCTION_DEFINE_STMT)) {
                    genFunctionDefineStmt(subNode, funName);
                }
            }
        }
        public void genStruct(GrammerTreeNode node, string funName)
        {
            //we set the start location of struct is -1
            globalTable.insertStruct(node,-1);
        }
        public void genFunctionDefineStmt(GrammerTreeNode node, string funName)
        {
            emitComment("->Function Definition: " + node.Children[1].TerminalType.Image);
            globalTable.insertFunction(node,emitLoc);
            //build a symbol table for this function
            symbolTable = new SymTab(globalTable);
            tableList.Add(symbolTable);//add it to the table list

            //reset the location
            location = 0;
            //for the start switch
            if (node.Children[1].TerminalType.Image.Equals("main"))
            {
                int currentLoc = emitSkip(0);
                emitBackup(2);
                emitRM_Abs("LDA", pc, currentLoc, "jmp to main");
                emitRestore();

                //push the gp to stack
                emitRM("LDC", ac, 0, 0, "Load constant 0");
                emitRM("ST", ac, 0, gp, "store to stack");
                location++;
                rtnInsLoc = emitSkip(2);//skip for return and store it
                location++;
            }
            else {
                location += 2;//increment 2
                BucketList temL = globalTable.lookUp(node.Children[1].TerminalType.Image);
                location += ((List<Hashtable>)temL.hashTable["params"]).Count;
            }
            if((node.Children.Last().Type == TreeNodeType.NONTERMINAL)&&(node.Children.Last().NonterminalType == NonterminalType.STMT_SEQUENCE)){
                genStmtSequence(node.Children.Last(), node.Children[1].TerminalType.Image);//generate the stmt sequence
            }
            emitComment("<-Function Definition: " + node.Children[1].TerminalType.Image + "\r\n");
        }
        public void genStmtSequence(GrammerTreeNode node, string funName)
        {
            if (node != null)
            {
                foreach (GrammerTreeNode subNode in node.Children)
                    genStmt(subNode, funName);
            }
        }
        public void genIfStmt(GrammerTreeNode node, string funName)
        {
            int savedLoc1, savedLoc2, currentLoc;
            int loc;
            emitComment("-> If");
            genExp(node.Children[0], funName);//gen the exp
            savedLoc1 = emitSkip(1);
            emitComment("if: jump to else belongs here");
            genStmtSequence(node.Children[1],funName);
            savedLoc2 = emitSkip(1);
            emitComment("if: jump to end belongs here");
            
            //backup for if-stmt
            currentLoc = emitSkip(0);
            emitBackup(savedLoc1);
            emitRM_Abs("JEQ", ac, currentLoc, "if: jmp to else");
            emitRestore();

            if (node.Children.Count == 3) { //has elst part
                genStmtSequence(node.Children[2],funName);
            }

            //backup for else part,to the end
            currentLoc = emitSkip(0);
            emitBackup(savedLoc2);
            emitRM_Abs("LDA", pc, currentLoc, "jmp to end");
            emitRestore();

            emitComment("<- If");
        }
        public void genWhileStmt(GrammerTreeNode node, string funName)
        {
            int savedLoc1, savedLoc2, currentLoc;
            int loc;

            emitComment("-> While");
            savedLoc1 = emitSkip(0);
            genExp(node.Children[0],funName);//gen the exp
            emitComment("while: jump to end belongs here");
            savedLoc2 = emitSkip(1);
            genStmtSequence(node.Children[1],funName);//gen the stmt sequence
            emitRM_Abs("LDA", pc, savedLoc1, "jmp to start");

            //backup the while stmt
            currentLoc = emitSkip(0);
            emitBackup(savedLoc2);
            emitRM_Abs("JEQ", ac, currentLoc, "while: jmp to end");//jump to the end
            emitRestore();

            emitComment("<- While");
        }
        public void genDeclareStmt(GrammerTreeNode node,string funName)
        {
            //update the symbol table
            symbolTable.insertSymbol(node,location);
   
            if (node.Children.Count > 2) { 
                //assign
                genExp(node.Children[2], funName);
                emitRM("ST", ac, location, gp, "declare assign");
            }
            //update the value of the location
            location += symbolTable.lookUpSize(node.Children[1].TerminalType.Image);
        }
        public void genReadStmt(GrammerTreeNode node,string funName)
        {
            emitRO("IN", ac, 0, 0, "read value");
            int loc;
            //#true
            if ((node.Children[0].Type == TreeNodeType.NONTERMINAL) && (node.Children[0].NonterminalType == NonterminalType.STRUCT_MEM_DESC)){
                //for the struct desc
                GrammerTreeNode memNode = node.Children[0];
                loc = symbolTable.interpretStructMemberLocation(memNode);
            }
            //#true
            else if((node.Children[0].Type == TreeNodeType.NONTERMINAL)&&(node.Children[0].Children.Count
                 != 0))
            { //id element follow
                loc = symbolTable.interpretElementLocation(node.Children[0]);
            }
            //#true
            else { //symbol
                //find in the params
                if ((loc = symbolTable.lookUpParaLoc(funName, node.Children[0].TerminalType.Image)) == -1)
                {
                    loc = symbolTable.lookUpLoc(node.Children[0].TerminalType.Image);
                }
                else ;//如果是参数，则已经为相对地址。存入符号表的都是绝对地址。指令中的都为相对地址
            }

            emitRM("ST",ac, loc,gp, "read: store value");
        }
        public void genWriteStmt(GrammerTreeNode node, string funName)
        {
            if(node.Children.Count != 0)
                genExp(node.Children[0], funName);
            emitRO("OUT",ac, 0, 0, "write ac");
        }
        public void genFunctionCallStmt(GrammerTreeNode node, string funName)
        {
            if ((node.Type == TreeNodeType.NONTERMINAL) && (node.NonterminalType == NonterminalType.FUNCTION_CALL_STMT)) {
                emitComment("->Function Call: "+node.Children[0].TerminalType.Image);

                //push the parameters to temporary var
                for (int i = 1; i < node.Children.Count; i++)
                {
                    genExp(node.Children[i], funName);
                    emitRM("ST", ac, tmpOffset--, mp, "store parameter " + i + " to tmp");
                }
                
                int temLoc = 0;
                //如何更新函数ebp的值，先存老的到tmp，然后更新之后存入位置0
                emitRM("ST",gp,tmpOffset--,mp,"store the old gp to tmp");
                //update the value of the ebp
                emitRM("LDC",ac,location,0,"load the location");
                emitRO("ADD",gp,ac,gp,"update the gp");
                emitRM("LD",ac1,++tmpOffset,mp,"Load the old gp to ac1");
                emitRM("ST",ac1,0,gp,"store the old gp to loc 0");
                temLoc++;
                int savedLoc1 = emitSkip(2);
                temLoc++;
                for (int i = 1; i < node.Children.Count; i++)
                {
                    emitRM("LD", ac, ++tmpOffset, mp, "Load parameter " + (node.Children.Count-i));
                    emitRM("ST", ac, temLoc, gp, "store parameter " + (node.Children.Count - i));
                    temLoc++;
                }

                //go to the func instruction
                int funStart = symbolTable.lookUpLoc(node.Children[0].TerminalType.Image);
                emitRM_Abs("LDA", pc, funStart, "jmp to function: " + node.Children[0].TerminalType.Image);
                //push the return address
                int currentLoc = emitSkip(0);
                emitBackup(savedLoc1);
                emitRM("LDC",ac,currentLoc,0,"load return address");
                emitBackup(savedLoc1+1);
                emitRM("ST",ac,1,gp,"store the return address");
                emitRestore();
                
                emitComment("<-Function Call: " + node.Children[0].TerminalType.Image);
            }
        }
        public void genReturnStmt(GrammerTreeNode node, string funName)
        {
            //emitRM("LD", ac1, 1, gp, "load the return address");
            //emitRM("ST", ac1, tmpOffset--, mp, "store the return address");
            if(node.Children.Count > 0)
                genExp(node.Children[0],funName);
            //emitRM("ST",ac1,tmpOffset--,mp,"store the return address");
            emitRM("LD", ac1, 1, gp, "load the return address");
            emitRM("LD",gp,0,gp,"back the gp");
            //emitRM("LD",ac1,++tmpOffset,mp,"load return address");
            //jmp to the return address
            emitRM("LDA",pc,0,ac1,"jmp return");

        }
        public void genAssignStmt(GrammerTreeNode node, string funName)
        {
            emitComment("-> assign");
            int loc;
            //calculate the value of the expression
            genExp(node.Children[1], funName);
            if(node.Children[1].NonterminalType != NonterminalType.FUNCTION_CALL_STMT){
                //change the value stored in the table
                symbolTable.change(node.Children[0].TerminalType.Image,symbolTable.interpreteDimenExpression(node.Children[1]));
            }
            
            if (node.Children[0].Children.Count != 0)
            { //array element
                loc = symbolTable.interpretElementLocation(node.Children[0]);
                emitRM("ST",ac, loc,gp, "assign: store value to array element");
            }
            else {
                if ((loc = symbolTable.lookUpParaLoc(funName, node.Children[0].TerminalType.Image)) == -1)
                {
                    loc = symbolTable.lookUpLoc(node.Children[0].TerminalType.Image);
                }
                emitRM("ST", ac, loc, gp, "assign: store value to symbol");
            }
            emitComment("<- assign");
        }
        public void genStructDecStmt(GrammerTreeNode node, string funName)
        {
            symbolTable.insertSymbol(node,location);
            //update the location
            location += (symbolTable.lookUpSize(node.Children[1].TerminalType.Image));//the string
        }
        public void genStructAssignStmt(GrammerTreeNode node, string funName)
        {
            emitComment("-> assign struct member");
            int loc = -1;
            //calculate the value of the expression
            genExp(node.Children[1], funName);
            if (node.Children[0].NonterminalType == NonterminalType.STRUCT_MEM_DESC) {
                //mem_desc
                GrammerTreeNode memNode = node.Children[0];
                loc = symbolTable.interpretStructMemberLocation(memNode);
            }
            emitRM("ST", ac, loc, gp, "assign: store value to struct element");
            emitComment("<- assign struct member");
        }
        

        ///generate the Expression
        ///GrammmerTreeNode type is as follows:
        ///             level 1:
        ///ANDEXPR,OREXPR,LOGICNOTEXPR
        ///             level 2:
        ///EQUALEXPR,(Token:TRUE,FALSE)
        ///             level 3:
        ///ARITHMEXPR, TERM, UNARY,STRUCT_MEM_DESC
        ///(IDENTIFIER,INT_CONSTANT,REAL_CONSTANT)
        ///(GT,GOE, LT,LOE, EQ, NEQ)
        ///(PLUS, MINUS, MULTIPLY, DIVIDE,MOD)
        public void genExp(GrammerTreeNode node, string funName)
        {
            if (node.Type == TreeNodeType.NONTERMINAL)
            {
                switch (node.NonterminalType) { 
                    case NonterminalType.OREXPR:
                        genOrExp(node, funName);
                        break;
                    case NonterminalType.ANDEXPR:
                        genAndExp(node, funName);
                        break;
                    case NonterminalType.LOGICNOTEXPR:
                        genLogicalNotExp(node, funName);
                        break;
                    case NonterminalType.EQUALEXPR:
                        genEqualExp(node, funName);
                        break;
                    case NonterminalType.ARITHMEXPR:
                        genArithmExp(node, funName);
                        break;
                    case NonterminalType.TERM:
                        genTermExp(node, funName);
                        break;
                    case NonterminalType.UNARY:
                        genUnaryExp(node, funName);
                        break;
                    case NonterminalType.STRUCT_MEM_DESC:
                        genStructMemDescExp(node, funName);
                        break;
                    case NonterminalType.ARRAY:
                        genIdentifier(node,funName);
                        break;
                    default:
                        genStmt(node,funName);//generate the other stmts
                        break;
                }
            }
            else {
                switch(node.TerminalType.Kind){
                    case TokenKind.TRUE:
                        genTrue(node, funName);
                        break;
                    case TokenKind.FALSE:
                        genFalse(node, funName);
                        break;
                    case TokenKind.INT_CONSTANT:
                        genIntConstant(node, funName);
                        break;
                    case TokenKind.REAL_CONSTANT:
                        genRealConstant(node, funName);
                        break;
                    case TokenKind.IDENTIFIER:
                        genIdentifier(node, funName);
                        break;
                    default:
                        break;
                }
            }
        }

        ///the tool method for the last generator
        public void genOrExp(GrammerTreeNode node, string funName)
        {
            emitComment("->OrExp");
            genExp(node.Children[0], funName);
            int savedLoc1 = emitSkip(1);
            genExp(node.Children[2], funName);
            int cntLoc = emitSkip(0);
            emitBackup(savedLoc1);
            emitRM_Abs("JNE", ac, cntLoc, "andExp is true");
            emitRestore();
            emitRM("LDC", ac, 1, ac, "set ac false");
            emitComment("<-OrExp");
        }
        public void genAndExp(GrammerTreeNode node, string funName)
        {
            emitComment("->AndExp");
            genExp(node.Children[0],funName);
            int savedLoc1 = emitSkip(1);
            genExp(node.Children[2],funName);
            int cntLoc = emitSkip(0);
            emitBackup(savedLoc1);
            emitRM_Abs("JEQ",ac,cntLoc,"andExp is false");
            emitRestore();
            emitRM("LDC",ac,0,ac,"set ac false");
            emitComment("<-AndExp");
        }
        public void genLogicalNotExp(GrammerTreeNode node, string funName)
        {
            emitComment("->NotExp");
            genExp(node.Children[1],funName);
            emitRM("JEQ",ac,2,pc,"not exp");
            emitRM("LDC",ac,0,ac,"set false");
            emitRM("LDA",pc,1,pc,"step over");
            emitRM("LDC",ac,1,ac,"set true");
            emitComment("<-NotExp");
        }
        public void genEqualExp(GrammerTreeNode node, string funName)
        {
            emitComment("->EqualExp");
            genExp(node.Children[0],funName);
            emitRM("ST",ac,tmpOffset--,mp,"store left optr");
            genExp(node.Children[2],funName);
            emitRM("LD",ac1,++tmpOffset,mp,"load left optr");
            emitRO("SUB", ac,ac1,ac, "opr -");
            if (node.Children[1].TerminalType.Kind == TokenKind.EQ) {
                emitRM("JEQ", ac, 2, pc, "br if true");
            }
            else if (node.Children[1].TerminalType.Kind == TokenKind.NEQ)
            {
                emitRM("JNE", ac, 2, pc, "br if true");
            }
            else if (node.Children[1].TerminalType.Kind == TokenKind.GOE)
            {
                emitRM("JGE", ac, 2, pc, "br if true");
            }
            else if (node.Children[1].TerminalType.Kind == TokenKind.GT)
            {
                emitRM("JGT", ac, 2, pc, "br if true");
            }
            else if (node.Children[1].TerminalType.Kind == TokenKind.LOE)
            {
                emitRM("JLE", ac, 2, pc, "br if true");
            }
            else {
                emitRM("JLT", ac, 2, pc, "br if true");
            }
            emitRM("LDC", ac, 0, ac, "false case");
            emitRM("LDA", pc, 1, pc, "unconditional jmp");
            emitRM("LDC", ac, 1, ac, "load constant true");
            emitComment("<-EqualExp");
        }
        public void genArithmExp(GrammerTreeNode node, string funName)
        {
            //gen the left
            emitComment("->Arithmatic");
            genExp(node.Children[0], funName);
            emitRM("ST", ac, tmpOffset--, mp, "store left opto");
            genExp(node.Children[2], funName);
            emitRM("LD", ac1, ++tmpOffset, mp, "op: load left opto");
            if (node.Children[1].TerminalType.Kind == TokenKind.PLUS)
                emitRO("ADD", ac, ac1, ac, "op +");
            else
                emitRO("SUB", ac, ac1, ac, "op -");
            emitComment("<-Arithmatic");
        }
        public void genTermExp(GrammerTreeNode node, string funName)
        {
            //gen the left
            emitComment("->Term");
            genExp(node.Children[0], funName);
            emitRM("ST", ac, tmpOffset--, mp, "store left opto");
            genExp(node.Children[2], funName);
            emitRM("LD", ac1, ++tmpOffset, mp, "op: load left opto");
            if (node.Children[1].TerminalType.Kind == TokenKind.MULTIPLY)
                emitRO("MUL", ac, ac1, ac, "op *");
            else if (node.Children[1].TerminalType.Kind == TokenKind.DIVIDE)
                emitRO("DIV",ac,ac1,ac, "op /");
            else //for mod
                emitRO("MOD", ac, ac1, ac, "op %");
            emitComment("<-Term");
        }
        public void genUnaryExp(GrammerTreeNode node, string funName)
        {
            if (node.Children[0].TerminalType.Kind == TokenKind.MINUS) {
                emitComment("->Negative value");
                genExp(node.Children[1], funName);
                emitRM("ST", ac, tmpOffset--,mp, "store pos value");
                emitRM("LDC", ac, 0, 0, "load const 0");
                emitRM("LD", ac1, ++tmpOffset,mp, "op: load pos value");
                emitRO("SUB", ac, ac, ac1, "op -");
                emitComment("<-Negative value");
            }
        }
        public void genStructMemDescExp(GrammerTreeNode node, string funName)
        {
            int loc = -1;
            if (node.NonterminalType == NonterminalType.STRUCT_MEM_DESC)
            {
                //mem_desc
                GrammerTreeNode memNode = node;
                loc = symbolTable.interpretStructMemberLocation(memNode);
                emitRM("LD",ac, loc,gp, "load struct member value");
            }
        }
        public void genTrue(GrammerTreeNode node, string funName)
        {
            emitComment("-> Const TRUE");
            emitRM("LDC", ac, 1, 0, "load const true");
            emitComment("<- Const TRUE");
        }
        public void genFalse(GrammerTreeNode node, string funName)
        {
            emitComment("-> Const FALSE");
            emitRM("LDC", ac, 0, 0, "load const false");
            emitComment("<- Const FALSE");
        }
        public void genIntConstant(GrammerTreeNode node, string funName)
        {
            emitComment("-> Const INT");
            emitRM("LDC", ac, node.IntValue, 0, "load const int");
            emitComment("<- Const INT");
        }
        public void genRealConstant(GrammerTreeNode node, string funName)
        {
            emitComment("-> Const REAL");
            emitRM2("LDC", ac, node.RealValue, 0, "load const real");
            emitComment("<- Const REAL");
        }
        public void genIdentifier(GrammerTreeNode node, string funName)
        {
            if (node.Children.Count != 0)
            {
                emitComment("-> Array Element");//array element
                //calculate the absolute location
                int eleLoc = symbolTable.interpretElementLocation(node);
                emitRM("LD", ac, eleLoc, gp, "load array element value");
                emitComment("<- Array Element");//array element
            }
            else
            {
                emitComment("-> Id");
                int temLoc;
                if ((temLoc = symbolTable.lookUpParaLoc(funName, node.TerminalType.Image)) == -1)
                {
                    temLoc = symbolTable.lookUpLoc(node.TerminalType.Image);
                }
                emitRM("LD", ac, temLoc, gp, "load id value");
            }

        }


        ///the tool method to generate internal code
        //generate the comment
        public void emitComment(string c)
        {
            if(JDYCompiler.TraceCode)
                this.codeString += ("* " + c + "\r\n");
        }

        /// <summary>
        ///generate the RO instruction
        /// </summary>
        /// <param name="op"></param>
        /// <param name="r"></param>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <param name="c"></param>
        public void emitRO(string op, int r, int s, int t, string c)
        {
            codeString += (emitLoc++ + ":  " + op + "  " + r + "," + s + "," + t + " ");
            if (JDYCompiler.TraceCode)
                codeString += ("\t" + c);
            codeString += "\r\n";
            if (highEmitLoc < emitLoc) highEmitLoc = emitLoc;
        }

        /// <summary>
        /// emit the register memory instruction
        /// </summary>
        /// <param name="op"></param>
        /// <param name="r"></param>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <param name="c"></param>
        public void emitRM(string op, int r, int s, int t, string c)
        {
            codeString += (emitLoc++ + ":  " + op + "  " + r + "," + s + "(" + t + ") ");
            if (JDYCompiler.TraceCode)
                codeString += ("\t" + c);
            codeString += "\r\n";
            if (highEmitLoc < emitLoc) highEmitLoc = emitLoc;
        }

        ///for the real LDC
        public void emitRM2(string op, int r, float s, int t, string c)
        {
            codeString += (emitLoc++ + ":  " + op + "  " + r + "," + s + "(" + t + ") ");
            if (JDYCompiler.TraceCode)
                codeString += ("\t" + c);
            codeString += "\r\n";
            if (highEmitLoc < emitLoc) highEmitLoc = emitLoc;
        }

        /// <summary>
        /// generate the register ab instruction
        /// </summary>
        /// <param name="op"></param>
        /// <param name="r"></param>
        /// <param name="a"></param>
        /// <param name="c"></param>
        public void emitRM_Abs(string op, int r, int a, string c)
        {
            codeString += (emitLoc + ":  " + op + "  " + r + "," + (a - (emitLoc + 1)) + "(" + pc + ") ");
            ++emitLoc;
            if (JDYCompiler.TraceCode)
                codeString += ("\t" + c);
            codeString += "\r\n";
            if (highEmitLoc < emitLoc) highEmitLoc = emitLoc;
        }
        /// <summary>
        /// emit the skip instruction
        /// </summary>
        /// <param name="howMany"></param>
        /// <returns></returns>
        public int emitSkip(int howMany)
        {
            int i = emitLoc;
            emitLoc += howMany;
            if (highEmitLoc < emitLoc) highEmitLoc = emitLoc;
            return i;
        }

        /// <summary>
        /// emit the backup instruction
        /// </summary>
        /// <param name="loc"></param>
        public void emitBackup(int loc)
        {
            if (loc > highEmitLoc) emitComment("BUG in emitBackup");
            emitLoc = loc;
        }

        /// <summary>
        /// emit the restore instruction
        /// </summary>
        public static void emitRestore()
        { 
            emitLoc = highEmitLoc; 
        }

    }
}
