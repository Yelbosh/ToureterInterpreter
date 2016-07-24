using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JDYCompiler
{

    internal enum TreeNodeType {TERMINAL,NONTERMINAL}
    internal enum NonterminalType {
        START,/*表示开始的非终结符*/
        ARRAY,STRUCT,CLASS,/*代表结构体,类或者函数的非终结符,这类对象做参数时,按照引用传递*/
        STMT_SEQUENCE,STMT, DECLARE_STMT, IF_STMT, WHILE_STMT, ASSGIN_STMT, RETURN_STMT, WRITE_STMT,READ_STMT,FUNCTION_CALL_STMT, FUNCTION_DEFINE_STMT,/*语句类型的非终结符*/
        //My NonterminalType
        STRUCT_MEM_DESC,STRUCT_DEC_STMT,STRUCT_ASSIGN_STMT,//struct declare and assign
        EXPR,ANDEXPR,OREXPR,LOGICNOTEXPR, EQUALEXPR, ARITHMEXPR, TERM, UNARY, ELEMENT/*表达式类型的非终结符*/
  
    }

    internal sealed class GrammerTreeNode {
        public List<GrammerTreeNode> Children { get;private set; }
        public TreeNodeType Type { get; set; } //is terminal
        public NonterminalType NonterminalType { get; set; }
        public Token TerminalType { get; set; }
        public float RealValue { get;private set; }
        public int IntValue { get; private set; }
        public int Dimension { get;private set; }

        public GrammerTreeNode() { this.Children = new List<GrammerTreeNode>(); }
        public GrammerTreeNode(Token token)
            : this()
        {
            this.Type = TreeNodeType.TERMINAL;
            this.TerminalType = token;
            if (token.Kind == TokenKind.INT_CONSTANT) {
                this.IntValue = Convert.ToInt32(token.Image);
            }
            else if (token.Kind == TokenKind.REAL_CONSTANT) {
                this.RealValue = Convert.ToSingle(token.Image);
            }
        }
        public GrammerTreeNode Add(Token terminal) {
            GrammerTreeNode node = new GrammerTreeNode() {Type=TreeNodeType.TERMINAL, TerminalType = terminal };
            if (terminal.Kind == TokenKind.INT_CONSTANT)
                node.IntValue = Convert.ToInt32(terminal.Image);
            else if (terminal.Kind == TokenKind.REAL_CONSTANT)
                node.RealValue = Convert.ToSingle(terminal.Image);
             this.Add(node);
             return node;
        }
        public void Add(GrammerTreeNode nonterminal) {
            this.Children.Add(nonterminal);
        }
        public void Add(List<GrammerTreeNode> nonterminals) {
            foreach (var nonterminal in nonterminals)
                this.Add(nonterminal);
        }

        public void DimensionAdd() {
            if (this.Type != TreeNodeType.NONTERMINAL)
            {
                this.Type = TreeNodeType.NONTERMINAL;
                this.NonterminalType = NonterminalType.ARRAY;
            }
            this.Dimension++;
        }
        
        
    }
    internal class GrammerAnalyzer
    {
        #region const field
        static readonly TokenKind[] typeToken = new TokenKind[] { TokenKind.INTEGER, TokenKind.FLOAT, TokenKind.BOOLEAN, TokenKind.VOID };
        static readonly TokenKind[] nonVoidTypeToken = new TokenKind[] { TokenKind.INTEGER, TokenKind.FLOAT, TokenKind.BOOLEAN };
        static readonly TokenKind[] boolElemtToken = new TokenKind[] { TokenKind.TRUE, TokenKind.FALSE };
        static readonly TokenKind[] nonExprToken = new TokenKind[] { TokenKind.COMMA, TokenKind.SEMI, TokenKind.RPARENT, TokenKind.CBRAKET };
        static readonly TokenKind[] equalityToken = new TokenKind[] { TokenKind.EQ, TokenKind.NEQ, TokenKind.GT, TokenKind.LT,TokenKind.GOE,TokenKind.LOE };
        static readonly TokenKind[] plusOrMinusToken = new TokenKind[] { TokenKind.PLUS, TokenKind.MINUS };
        static readonly TokenKind[] multiOrDividToken = new TokenKind[] { TokenKind.MULTIPLY, TokenKind.DIVIDE ,TokenKind.MOD};
        static readonly TokenKind[] constElemToken = new TokenKind[] { TokenKind.INT_CONSTANT, TokenKind.REAL_CONSTANT };
        static readonly TokenKind[] bitBinocularToken = new TokenKind[] {  TokenKind.BITAND, TokenKind.BITOR };
        static readonly TokenKind[] bitUnaryToken = new TokenKind[] { TokenKind.BITNOT, TokenKind.BITXOR };
        //for the expression 
        static readonly TokenKind[] expressionToken = new TokenKind[] { TokenKind.INT_CONSTANT, TokenKind.REAL_CONSTANT, TokenKind.IDENTIFIER, TokenKind.LPARENT };
        #endregion

        #region static member
        private static Tokens sTokens;
        #endregion

        public static GrammerTreeNode Parse(Tokens tokens) {
            sTokens = tokens;
            GrammerTreeNode root=start();
            return root;
        }

        private static GrammerTreeNode start(){
            GrammerTreeNode root = new GrammerTreeNode(){Type=TreeNodeType.NONTERMINAL,NonterminalType=NonterminalType.START};
            
            while(!sTokens.TestNextToken(TokenKind.EOF)){
               if(sTokens.TestNextToken(TokenKind.STRUCT)){
                    root.Add(structDeclareStmt());
               }
               else
               {
                   root.Add(classDeclareStmt());
               }
            }
            return root;
        }
        /// <summary>
        /// tree node struct
        /// -----------------------------------------
        ///                             declare_stmt1
        /// class_delcare_stmt          delcare_stmt2
        /// (Terminal store class id)   ...
        ///                             delcare_stmtn
        ///                             function1
        ///                             function2
        ///                             ...
        ///                             functionn
        /// -----------------------------------------
        /// Order of delcare_stmt or function is not important                            
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode classDeclareStmt()
        {
            GrammerTreeNode class_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.CLASS };
            sTokens.Consume(TokenKind.CLASS);
            class_stmt.TerminalType=(sTokens.Consume(TokenKind.IDENTIFIER));
            sTokens.Consume(TokenKind.OBRACE);
            while (!sTokens.TestNextToken(TokenKind.CBRACE))
            {
                if (sTokens.TestNextToken(TokenKind.LPARENT, 2))
                    class_stmt.Children.Add(functionStmt());
                else
                    class_stmt.Children.AddRange(declareStmt());
            }
            sTokens.Consume(TokenKind.CBRACE);
            return class_stmt;
        }
        /// <summary>
        /// tree node struct
        /// --------------------------
        ///                                 declare_stmt1
        /// struct_declare_stmt             declare_stmt2
        /// (Terminal contains struct id)   ...
        ///                                 decare_stmtn
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode structDeclareStmt()
        {
            GrammerTreeNode struct_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.STRUCT };
            sTokens.Consume(TokenKind.STRUCT);
            struct_stmt.TerminalType = (sTokens.Consume(TokenKind.IDENTIFIER));
            sTokens.Consume(TokenKind.OBRACE);
            while (!sTokens.TestNextToken(TokenKind.CBRACE)) { //add the struct declare
                if (sTokens.TestNextToken(typeToken))
                    struct_stmt.Add(declareStmt());
                else if (sTokens.TestNextToken(TokenKind.STRUCT))
                    struct_stmt.Add(struct_dec_stmt());
            }
            sTokens.Consume(TokenKind.CBRACE);
            return struct_stmt;
        }
        /// <summary>
        /// 
        /// -------------------------------------------------
        ///                 declare_stmt1
        /// declare_stmts   declare_stmt2
        ///                 ...
        ///                 declare_stmtn
        /// --------------------------------------------------                
        ///                 type
        ///                             dimention1_size(expr)
        /// declare_stmt    identifier  dimention2_size(expr)
        ///                             ...
        ///                             dimentsionn_size(expr)
        /// 
        ///                 value(for delcare and assgin, e.g. int a=2;)
        ///                 
        /// </summary>
        /// <returns></returns>
        private static List<GrammerTreeNode> declareStmt()
        {
             List<GrammerTreeNode> declares = new List<GrammerTreeNode>();
             Token type=  sTokens.Consume(typeToken);

             while (true)
             {
                 GrammerTreeNode declare = new GrammerTreeNode() {Type=TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.DECLARE_STMT };
                 declare.Add(type);
                 GrammerTreeNode element = declare.Add(sTokens.Consume(TokenKind.IDENTIFIER));

                 if (sTokens.TestNextToken(TokenKind.ASSIGN))
                 {
                     sTokens.Consume(TokenKind.ASSIGN);
                     declare.Add(expression());
                 }
                 else if (sTokens.TestNextToken(TokenKind.OBRAKET))//n dimentional array
                 {
                     do
                     {
                         sTokens.Consume(TokenKind.OBRAKET);
                         element.Add(arithmaticExpression());
                         element.DimensionAdd();
                         sTokens.Consume(TokenKind.CBRAKET);
                     } while (sTokens.TestNextToken(TokenKind.OBRAKET));
                 }

                 declares.Add(declare);
                 if (sTokens.TestNextToken(TokenKind.COMMA))
                     sTokens.Consume(TokenKind.COMMA);
                 else
                     break;
             }
            
            sTokens.Consume(TokenKind.SEMI);
            return declares;

        }
        /// <summary>
        /// tree node struct:
        /// ---------------------------------------
        ///                     return_type
        /// function_tree_node  function_name
        ///                     lparent(for separate)
        ///                     declare_stmt1
        ///                     declare_stmt2
        ///                     ...
        ///                     declare_stmtn
        ///                     rparent(for separate)
        ///                     stmt_sequence
        /// ----------------------------------------
        /// declare_stmt here may simply use GrammerTreeNode.Dimension to 
        /// describe the dimension of the array, since in param list, size
        /// is not very important. e.g. int foo(int bar[][],bool erbi[]);
        /// i simply regard 'bar' or 'erbi' as reference.
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode functionStmt() {
            GrammerTreeNode function = null;
            Token next = null;
            function = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.FUNCTION_DEFINE_STMT };
                
            next = sTokens.Consume(typeToken);
            function.Add(next);

            next = sTokens.Consume(TokenKind.IDENTIFIER);
            function.Add(next);

            next = sTokens.Consume(TokenKind.LPARENT);
            function.Add(next);

            while(!sTokens.TestNextToken(TokenKind.RPARENT)){ //param list of function
                GrammerTreeNode declare;
                if (sTokens.TestNextToken(TokenKind.STRUCT))
                {
                    GrammerTreeNode struct_dec_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.STRUCT_DEC_STMT };
                    sTokens.Consume(TokenKind.STRUCT);
                    struct_dec_stmt.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                    GrammerTreeNode element = struct_dec_stmt.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                    if (sTokens.TestNextToken(TokenKind.COMMA))
                    {
                        
                    }
                    //struct array
                    else if (sTokens.TestNextToken(TokenKind.OBRAKET))//n dimentional array
                    {
                        do
                        {
                            sTokens.Consume(TokenKind.OBRAKET);
                            if(sTokens.TestNextToken(expressionToken))
                                element.Add(arithmaticExpression());
                            element.DimensionAdd();
                            sTokens.Consume(TokenKind.CBRAKET);
                        } while (sTokens.TestNextToken(TokenKind.OBRAKET));
                    }
                    declare = struct_dec_stmt;
                }
                else
                {
                    declare = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.DECLARE_STMT };
                    Token subNext = sTokens.Consume(nonVoidTypeToken);
                    declare.Add(subNext);

                    subNext = sTokens.Consume(TokenKind.IDENTIFIER);
                    GrammerTreeNode element = declare.Add(subNext);

                    while (sTokens.TestNextToken(TokenKind.OBRAKET))
                    {
                        subNext = sTokens.Consume(TokenKind.OBRAKET);
                        element.DimensionAdd();
                        subNext = sTokens.Consume(TokenKind.CBRAKET);
                    }
                }  
                function.Add(declare);
                if (sTokens.TestNextToken(TokenKind.COMMA)) sTokens.Consume(TokenKind.COMMA);
            }
            function.Add(sTokens.Consume(TokenKind.RPARENT));

            sTokens.Consume(TokenKind.OBRACE);
            GrammerTreeNode stmt_sequence = stmtSequence();
            function.Add(stmt_sequence);
            sTokens.Consume(TokenKind.CBRACE);
            return function;

           

        }
        /// <summary>
        ///                 stmt1
        /// stmt_sequence   stmt2
        ///                 ...
        ///                 stmtn
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode stmtSequence() {
            GrammerTreeNode stmt_sequence = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.STMT_SEQUENCE };
            while (!sTokens.TestNextToken(TokenKind.CBRACE))
            {
                stmt_sequence.Add(stmt());
            }
            return stmt_sequence;
            
        }
        /// <summary>
        /// tree node struct
        /// -----------------------------------------
        /// stmt    (declare_stmt)|(if_stmt)|(while_stmt)|(read_stmt)|(write_stmt)|(return_stmt)|(call_stmt)|(assgin_stmt)
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode stmt() {
            GrammerTreeNode stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.STMT };
            if (sTokens.TestNextToken(nonVoidTypeToken))
                stmt.Add(declareStmt());
            else if (sTokens.TestNextToken(TokenKind.STRUCT))//struct declare
                stmt.Add(struct_dec_stmt());
            else if (sTokens.TestNextToken(TokenKind.IF))
                stmt.Add(ifStmt());
            else if (sTokens.TestNextToken(TokenKind.WHILE))
                stmt.Add(whileStmt());
            else if (sTokens.TestNextToken(TokenKind.READ))
                stmt.Add(readStmt());
            else if (sTokens.TestNextToken(TokenKind.WRITE))
                stmt.Add(writeStmt());
            else if (sTokens.TestNextToken(TokenKind.RETURN))
                stmt.Add(returnStmt());
            else if (sTokens.TestNextToken(TokenKind.SEMI))//空白语句直接忽略.
                sTokens.Consume(TokenKind.SEMI);
            else if (sTokens.TestNextToken(TokenKind.IDENTIFIER)) //函数调用或者是赋值语句
            {
                if (sTokens.TestNextToken(TokenKind.LPARENT, 1)) //lookahead
                    stmt.Add(callStmt());
                else if (sTokens.TestNextToken(TokenKind.DOT, 1))
                    stmt.Add(struct_assign_stmt());
                else
                    stmt.Add(assginStmt());
            }
            return stmt;
        }
        /// <summary>
        /// tree node struct
        /// ----------------------------------------
        ///                             dimention1
        ///                             dimension2
        ///                 identifier    ...
        /// assgin_stmt                  dimensionn
        ///                 expr
        /// 
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode assginStmt() {
            GrammerTreeNode assgin_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.ASSGIN_STMT };

            GrammerTreeNode element= assgin_stmt.Add( sTokens.Consume(TokenKind.IDENTIFIER));
            while (sTokens.TestNextToken(TokenKind.OBRAKET)) {
                sTokens.Consume(TokenKind.OBRAKET);
                element.Add(arithmaticExpression());
                element.DimensionAdd();
                sTokens.Consume(TokenKind.CBRAKET);
            }
            sTokens.Consume(TokenKind.ASSIGN); //consume "="

            assgin_stmt.Add(expression()); //right part of "="(expr)
            
            sTokens.Consume(TokenKind.SEMI);
            return assgin_stmt;

        }
        /// <summary>
        /// tree node struct
        /// --------------------------
        /// return_stmt expr
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode returnStmt() {
            GrammerTreeNode return_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.RETURN_STMT };
            sTokens.Consume(TokenKind.RETURN);
            if(sTokens.TestNextToken(expressionToken))
                return_stmt.Add(expression());
            sTokens.Consume(TokenKind.SEMI);
            return return_stmt;
        }
        /// <summary>
        /// tree node struct
        /// ----------------------------------
        /// write_stmt expr
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode writeStmt(){
            GrammerTreeNode write_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.WRITE_STMT };
            sTokens.Consume(TokenKind.WRITE);
            write_stmt.Add(expression());
            sTokens.Consume(TokenKind.SEMI);
            return write_stmt;
        }
        /// <summary>
        /// tree node struct
        /// -------------------------------
        ///                         dimension1
        /// read_stmt identifier    dimension2
        ///                         ...
        ///                         dimensionn
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode readStmt() {
            GrammerTreeNode read_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.READ_STMT };
            sTokens.Consume(TokenKind.READ);
            //for the struct mem desc
            if (sTokens.TestNextToken(TokenKind.DOT,1)) {
                GrammerTreeNode elem = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.STRUCT_MEM_DESC };
                elem.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                do
                {
                    sTokens.Consume(TokenKind.DOT);
                    GrammerTreeNode iden = elem.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                    //for the array
                    if (sTokens.TestNextToken(TokenKind.OBRAKET))//n dimentional array
                    {
                        do
                        {
                            sTokens.Consume(TokenKind.OBRAKET);
                            iden.Add(arithmaticExpression());
                            iden.DimensionAdd();
                            sTokens.Consume(TokenKind.CBRAKET);
                        } while (sTokens.TestNextToken(TokenKind.OBRAKET));
                    }
                } while (sTokens.TestNextToken(TokenKind.DOT));

                read_stmt.Add(elem);
            }
                //for the array
            else if (sTokens.TestNextToken(TokenKind.OBRAKET, 1))
            {
                GrammerTreeNode element = read_stmt.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                while (sTokens.TestNextToken(TokenKind.OBRAKET))
                {
                    sTokens.Consume(TokenKind.OBRAKET);
                    element.Add(arithmaticExpression());
                    element.DimensionAdd();
                    sTokens.Consume(TokenKind.CBRAKET);
                }
            }

                //for the identifier
            else {
                read_stmt.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                
            }
            sTokens.Consume(TokenKind.SEMI);

            return read_stmt;
        }
        /// <summary>
        /// function call stmt struct:
        ///             identifier
        /// call_stmt   expr(param1)
        ///             expr(param2)
        ///             ...
        ///             expr(paramn)
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode callStmt() {
            GrammerTreeNode call_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.FUNCTION_CALL_STMT };
            call_stmt.Add(sTokens.Consume(TokenKind.IDENTIFIER));

            sTokens.Consume(TokenKind.LPARENT);
            while (!sTokens.TestNextToken(TokenKind.RPARENT)) {
                call_stmt.Add(expression());
                if(sTokens.TestNextToken(TokenKind.COMMA)) sTokens.Consume(TokenKind.COMMA);
            }
            sTokens.Consume(TokenKind.RPARENT);
            sTokens.Consume(TokenKind.SEMI);
            return call_stmt;

        }
        /// <summary>
        /// tree node struct
        /// ----------------------------------
        ///             expr
        /// while_stmt  (stmt_sequence)|(stmt)
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode whileStmt()
        {
            GrammerTreeNode while_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.WHILE_STMT };
            sTokens.Consume(TokenKind.WHILE);
            sTokens.Consume(TokenKind.LPARENT);
            while_stmt.Add(expression());
            sTokens.Consume(TokenKind.RPARENT);
            if (sTokens.TestNextToken(TokenKind.OBRACE))
            {
                sTokens.Consume(TokenKind.OBRACE);
                while_stmt.Add(stmtSequence());
                sTokens.Consume(TokenKind.CBRACE);
            }
            else
            {
                while_stmt.Add(stmt());
            }
            return while_stmt;
        }
        /// <summary>
        /// 
        /// 
        ///         expr
        /// if_stmt (stmt_sequence)|(stmt)
        ///         else(optional)
        ///         (stmt_sequence)|(stmt)
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode ifStmt() {
            GrammerTreeNode if_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.IF_STMT };
            sTokens.Consume(TokenKind.IF);
            sTokens.Consume(TokenKind.LPARENT);
            if_stmt.Add(expression());
            sTokens.Consume(TokenKind.RPARENT);
            if (sTokens.TestNextToken(TokenKind.OBRACE))
            {
                sTokens.Consume(TokenKind.OBRACE);
                if_stmt.Add(stmtSequence());
                sTokens.Consume(TokenKind.CBRACE);
            }
            else {
                if_stmt.Add(stmt());
            }
            if (sTokens.TestNextToken(TokenKind.ELSE)) {
                sTokens.Consume(TokenKind.ELSE);
                if (sTokens.TestNextToken(TokenKind.OBRACE))
                {
                    sTokens.Consume(TokenKind.OBRACE);
                    if_stmt.Add(stmtSequence());
                    sTokens.Consume(TokenKind.CBRACE);
                }
                else
                {
                    if_stmt.Add(stmt());
                }
            }
            return if_stmt;
        }
        ///<sumary>
        ///struct declare stmt
        /// struct DEMO demo1;
        private static GrammerTreeNode struct_dec_stmt() {
            GrammerTreeNode struct_dec_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.STRUCT_DEC_STMT };
            sTokens.Consume(TokenKind.STRUCT);
            struct_dec_stmt.Add(sTokens.Consume(TokenKind.IDENTIFIER));
            GrammerTreeNode element = struct_dec_stmt.Add(sTokens.Consume(TokenKind.IDENTIFIER));
            if (sTokens.TestNextToken(TokenKind.SEMI))
            {
                sTokens.Consume(TokenKind.SEMI);
            }
                //struct array
            else if (sTokens.TestNextToken(TokenKind.OBRAKET))//n dimentional array
            {
                do
                {
                    sTokens.Consume(TokenKind.OBRAKET);
                    element.Add(arithmaticExpression());
                    element.DimensionAdd();
                    sTokens.Consume(TokenKind.CBRAKET);
                } while (sTokens.TestNextToken(TokenKind.OBRAKET));
                sTokens.Consume(TokenKind.SEMI);
            }

            return struct_dec_stmt;

        }

        ///<sumary>
        ///struct_assign_stmt
        ///A.b.c = 9
        private static GrammerTreeNode struct_assign_stmt() {
            GrammerTreeNode struct_assign_stmt = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.STRUCT_ASSIGN_STMT };
            GrammerTreeNode element = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.STRUCT_MEM_DESC };//struct member
            element.Add(sTokens.Consume(TokenKind.IDENTIFIER));
            do
            {
                sTokens.Consume(TokenKind.DOT);
                GrammerTreeNode iden = element.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                //for the array
                if (sTokens.TestNextToken(TokenKind.OBRAKET))//n dimentional array
                {
                    do
                    {
                        sTokens.Consume(TokenKind.OBRAKET);
                        iden.Add(arithmaticExpression());
                        iden.DimensionAdd();
                        sTokens.Consume(TokenKind.CBRAKET);
                    } while (sTokens.TestNextToken(TokenKind.OBRAKET));
                }
            } while (sTokens.TestNextToken(TokenKind.DOT));

            struct_assign_stmt.Add(element);//add the element to the treenode

            sTokens.Consume(TokenKind.ASSIGN); //consume "="
            struct_assign_stmt.Add(expression()); //right part of "="(expr)
            sTokens.Consume(TokenKind.SEMI);
            return struct_assign_stmt;
        }
        /// <summary>
        /// NONTERMINAL.EXPR is never a root of a expr, it may be used for jugde level of an expr
        /// e.g. instance.NONTERMINAL > NONTERMINAL.EXPR means 'instance' is a sub level of an expr
        /// 
        /// i'm considering giving a trim of unneccessary node of an expr tree.
        /// e.g. to recongnize a number 3 may construct a tree like:
        ///     logic_or_expr logic_and_expr logic_not_expr equal_expr arithmetic_expr term unary element
        /// however if tirmed unneeded node, it will look like: 
        ///     element
        /// only one node.
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode expression() {
            return logicalOrExpression();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode logicalOrExpression() {
            GrammerTreeNode logic_or_expr = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.OREXPR };
               logic_or_expr.Add(logicalAndExpression());
               if (sTokens.TestNextToken(TokenKind.OR))
               {
                   logic_or_expr.Add(sTokens.Consume(TokenKind.OR));
                   logic_or_expr.Add(logicalOrExpression());
               }
               else {
                   logic_or_expr = logic_or_expr.Children.First();//no operator, trim the node
               }
            return logic_or_expr;
        }
        /// <summary>
        /// tree node sturct
        /// --------------------------------
        ///                 logic_not_expr
        /// logic_and_expr  and(&&)
        ///                 logic_not_expr
        ///                 
        /// however no operator(&&) exist, it will be trimed like:
        /// logic_not_expr
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode logicalAndExpression() {
            GrammerTreeNode logic_and_expr = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.ANDEXPR };
            logic_and_expr.Add(logicalNotExpression());
            if (sTokens.TestNextToken(TokenKind.AND))
            {
                logic_and_expr.Add(sTokens.Consume(TokenKind.AND)); //can be simplified
                logic_and_expr.Add(logicalAndExpression());
            }
            else {
                logic_and_expr = logic_and_expr.Children.First();//no operator, trim the node
            }
            return logic_and_expr;
        }
        /// <summary>
        ///                 not(optional)
        /// logic_not_expr  logic_element
        /// 
        /// however if trimed, it look like:
        /// 
        /// logic_element
        /// 
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode logicalNotExpression() {
            GrammerTreeNode logic_not_expr = new GrammerTreeNode() { Type=TreeNodeType.NONTERMINAL,NonterminalType=NonterminalType.LOGICNOTEXPR};
            if (sTokens.TestNextToken(TokenKind.NOT))
            {
                logic_not_expr.Add(sTokens.Consume(TokenKind.NOT));
                logic_not_expr.Add(logicalElement());
            }
            else {
                logic_not_expr = logicalElement();
            }
             
            return logic_not_expr;
        }
        /// <summary>
        /// logic_element true|false
        /// 
        /// or trimed like:
        /// equality_expr
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode logicalElement() {
            GrammerTreeNode logic_elemt = null;
            if (sTokens.TestNextToken(boolElemtToken))
            {
                logic_elemt = new GrammerTreeNode() { Type = TreeNodeType.TERMINAL, TerminalType = sTokens.Consume(boolElemtToken) };
            }
            else
            {
                logic_elemt =equalityExpression();
            }
            return logic_elemt;
        }
        private static GrammerTreeNode equalityExpression() {
            GrammerTreeNode equal_expr= new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.EQUALEXPR };
            equal_expr.Add(arithmaticExpression());
            if (sTokens.TestNextToken(equalityToken))
            {
                equal_expr.Add(sTokens.Consume(equalityToken));
                equal_expr.Add(arithmaticExpression());
            }
            else {
                equal_expr = equal_expr.Children.First();
            }
            return equal_expr;

        }
        private static GrammerTreeNode arithmaticExpression() {
            GrammerTreeNode arithm_expr = new GrammerTreeNode() { Type=TreeNodeType.NONTERMINAL,NonterminalType=NonterminalType.ARITHMEXPR};
            arithm_expr.Add(termExpression());
            if (sTokens.TestNextToken(plusOrMinusToken))
            {
                arithm_expr.Add(sTokens.Consume(plusOrMinusToken));
                arithm_expr.Add(arithmaticExpression());
            }
            else
            {
                arithm_expr = arithm_expr.Children.First();
            }
            return arithm_expr;
        }
        private static GrammerTreeNode termExpression() {
            GrammerTreeNode term_expr = new GrammerTreeNode() { Type=TreeNodeType.NONTERMINAL,NonterminalType=NonterminalType.TERM};
            term_expr.Add(unaryExpression());
            if (sTokens.TestNextToken(multiOrDividToken))
            {
                term_expr.Add(sTokens.Consume(multiOrDividToken));
                term_expr.Add(termExpression());
            }
            else {
                term_expr = term_expr.Children.First();
            }
            return term_expr;
        }
        /// <summary>
        ///             
        ///             mius(optional)
        /// unary_expr  element
        /// 
        /// </summary>
        /// <returns></returns>
        private static GrammerTreeNode unaryExpression() {
            GrammerTreeNode unary_expr = new GrammerTreeNode() { Type=TreeNodeType.NONTERMINAL,NonterminalType=NonterminalType.UNARY};
            if (sTokens.TestNextToken(TokenKind.MINUS))
            {
                unary_expr.Add(sTokens.Consume(TokenKind.MINUS));
                unary_expr.Add(element());
            }
            else {
                unary_expr = element();
            }
            
            return unary_expr;
        }
        private static GrammerTreeNode element() {
            GrammerTreeNode elem = null;
            if (sTokens.TestNextToken(constElemToken)) { 
                Token next=sTokens.Consume(constElemToken);
                elem = new GrammerTreeNode(next);
            }
            else if (sTokens.TestNextToken(TokenKind.IDENTIFIER))
            {
                //look ahead
                if (sTokens.TestNextToken(TokenKind.LPARENT, 1))
                {//function call
                    elem = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.FUNCTION_CALL_STMT };
                    elem.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                    sTokens.Consume(TokenKind.LPARENT);
                    while (!sTokens.TestNextToken(TokenKind.RPARENT))
                    {//param list
                        elem.Add(expression());
                        if (sTokens.TestNextToken(TokenKind.COMMA))
                            sTokens.Consume(TokenKind.COMMA);
                    }
                    sTokens.Consume(TokenKind.RPARENT);
                }
                else if (sTokens.TestNextToken(TokenKind.DOT, 1))
                {//struct
                    elem = new GrammerTreeNode() { Type = TreeNodeType.NONTERMINAL, NonterminalType = NonterminalType.STRUCT_MEM_DESC };
                    elem.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                    do
                    {
                        sTokens.Consume(TokenKind.DOT);
                        GrammerTreeNode iden = elem.Add(sTokens.Consume(TokenKind.IDENTIFIER));
                        //for the array
                        if (sTokens.TestNextToken(TokenKind.OBRAKET))//n dimentional array
                        {
                            do
                            {
                                sTokens.Consume(TokenKind.OBRAKET);
                                iden.Add(arithmaticExpression());
                                iden.DimensionAdd();
                                sTokens.Consume(TokenKind.CBRAKET);
                            } while (sTokens.TestNextToken(TokenKind.OBRAKET));
                        }
                    } while (sTokens.TestNextToken(TokenKind.DOT));
                }
                else
                { //array element
                    elem = new GrammerTreeNode() { Type = TreeNodeType.TERMINAL, TerminalType = sTokens.Consume(TokenKind.IDENTIFIER) };
                    //如果没有则表明是一个identifier
                    while (sTokens.TestNextToken(TokenKind.OBRAKET)) {
                        sTokens.Consume(TokenKind.OBRAKET);
                        elem.Add(arithmaticExpression());
                        elem.DimensionAdd();
                        sTokens.Consume(TokenKind.CBRAKET);
                    }
                }
            }
            else if (sTokens.TestNextToken(TokenKind.LPARENT))
            { //括号表达式...
                sTokens.Consume(TokenKind.LPARENT);
                elem=expression();
                sTokens.Consume(TokenKind.RPARENT);
            }
            else { //trigger a exception
                sTokens.Consume(constElemToken.Union(new TokenKind[] {TokenKind.IDENTIFIER,TokenKind.LPARENT }).ToArray());
            }
            return elem;
        }
    }
}
