using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace JDYCompiler
{
    //the item for the table
    internal sealed class BucketList
    {
        public BucketList()
        {
            isEmpty = true;
        }
        public BucketList(string name, int type)
        {
            this.name = name;
            this.isEmpty = false;
            this.type = type;
            this.hashTable = new Hashtable();
        }
        public string name;
        public bool isEmpty;
        public int type;//class,function,or symbol
        public Hashtable hashTable;
        public BucketList next;
    }
    internal sealed class SymTab
    {
        //define the enum type for table
        #region
        private const int STRUCT_TYPE = 0;
        private const int SYMBOL_TYPE = 1;
        private const int FUNC_TYPE = 2;

        public static int SIZE = 255;
        public static int SHIFT = 4;
        #endregion
        //the hash table
        public BucketList[] hashTable;//THE HASH TABLE

        public SymTab() { 
            this.hashTable = new BucketList[SIZE];
            for (int i = 0; i < SIZE; i++)
                this.hashTable[i] = new BucketList();
        }

        //stay for check
        public SymTab(SymTab table) { 
            this.hashTable = new BucketList[SIZE];
            for (int i = 0; i < SIZE; i++) {
                this.hashTable[i] = table.hashTable[i];
            }
        }

        ///public API
        ///insertFunction
        ///insertStruct
        ///insertSymbol
        ///for the class SymTab
        public void insertFunction(GrammerTreeNode node,int loc) {
            st_insert(node, loc);//loc 是指令地址
        }
        public void insertStruct(GrammerTreeNode node, int loc)
        {
            st_insert(node, loc);//loc 是指令地址
        }
        public int insertSymbol(GrammerTreeNode node, int loc)
        {
            return st_insertSymbol(node, loc);//loc 是内存地址
        }
        public BucketList lookUp(string name) {
            return st_lookup(name);//符号表单元
        }
        public int lookUpSize(string name) {
            return st_lookup_size(name);//符号内存大小
        }
        //change the value of one int var
        public void change(string name,int value) {
            st_change(name,value);
        }
        public string lookUpStructType(string oldType, string id) {
            return st_lookUpStructType(oldType,id);
        }
        public int lookUpLoc(string name) {
            BucketList l = lookUp(name);
            int loc = -1;
            switch (l.type) { 
                case STRUCT_TYPE:
                    loc = (int)l.hashTable["startLoc"];
                    break;
                case SYMBOL_TYPE:
                    loc = (int)l.hashTable["loc"];
                    break;
                case FUNC_TYPE:
                    loc = (int)l.hashTable["startLoc"];
                    break;
                default:
                    break;
            }
            return loc;
        }
        //look up param loc
        public int lookUpParaLoc(string funName,string id) {
            return st_lookupParaLoc(funName,id);
        }
        //remove specific vars loc higher than loca
        public void remove(int loca) {
            st_remove(loca);
        }
        //print the symbol table to specific file
        public void print() {
            string result = st_print();
            StreamWriter writer = new StreamWriter("SYMBOL_TABLE.TXT");
            writer.Write(result);//write the result to the selected file
            writer.Flush();//remember to flush the outputstream
            writer.Close();
        }


        public int hash(string key)
        {
            int temp = 0;
            int i = 0;
            while (i < key.Length)
            {
                temp = ((temp << SHIFT) + key[i]) % SIZE;
                ++i;
            }
            return temp;
        }

        //change the value of int
        public void st_change(string name,int value) {
            BucketList temL = lookUp(name);
            if ((((string)temL.hashTable["typeName"]).Equals("int")) && (((int)temL.hashTable["dimen"]) == 0))
            {
                //change the value
                temL.hashTable["value"] = value;
            }
        }
        //insert a node,FUNCTION && STRUCT
        public void st_insert(GrammerTreeNode node,int loc)
        {
            string name = "";
            //struct and func identifier
            if (node.NonterminalType == NonterminalType.FUNCTION_DEFINE_STMT)
                name = node.Children[1].TerminalType.Image;
            else if(node.NonterminalType == NonterminalType.STRUCT)
                name = node.TerminalType.Image;

            int h = hash(name);
            BucketList l = hashTable[h];
            BucketList tem = null;
            while ((!l.isEmpty) && (!l.name.Equals(name)))
            {
                l = l.next;
            }
            if (l.isEmpty)
            {
                if (node.NonterminalType == NonterminalType.FUNCTION_DEFINE_STMT)
                {
                    tem = new BucketList(name,FUNC_TYPE);
                    tem.hashTable.Add("type",node.Children[0].TerminalType.Kind);//Integer,Real,BOOL,VOID
                    tem.hashTable.Add("typeName",node.Children[0].TerminalType.Image);
                    //construct the paramList
                    List<Hashtable> paramList = new List<Hashtable>();
                    for(int i=3;(node.Children[i].Type == TreeNodeType.NONTERMINAL)||(node.Children[i].TerminalType.Kind!=TokenKind.RPARENT);i++){
                        //info for every param
                        Hashtable table = new Hashtable();
                        table.Add("type",node.Children[i].Children[0].TerminalType.Kind);//Integer,Real,Bool,Identifier
                        table.Add("typeName", node.Children[i].Children[0].TerminalType.Image);//for the struct
                        table.Add("name",node.Children[i].Children[1].TerminalType.Image);
                        table.Add("dimen",node.Children[i].Children[1].Dimension);
                        //add the size
                        int result;
                        if((TokenKind)table["type"] == TokenKind.IDENTIFIER){//struct
                            result = st_lookup_size((string)table["typeName"]);
                        }else
                            result = 1;
                        table.Add("size",result);
                        paramList.Add(table);
                    }
                    //add the list to the hashTable
                    tem.hashTable.Add("params",paramList);
                    //add the start position to the hashTable
                    tem.hashTable.Add("startLoc",loc);//loc is the position of the instruction
                    tem.hashTable.Add("size",0);
                }
                else if (node.NonterminalType == NonterminalType.STRUCT)
                {
                    tem = new BucketList(name, STRUCT_TYPE);
                    //the field item
                    List<Hashtable> list = new List<Hashtable>();
                    foreach (GrammerTreeNode subNode in node.Children)
                    {
                        Hashtable table = new Hashtable();
                        table.Add("type", subNode.Children[0].TerminalType.Kind);
                        table.Add("typeName", subNode.Children[0].TerminalType.Image);
                        table.Add("name", subNode.Children[1].TerminalType.Image);
                        table.Add("dimen", subNode.Children[1].Dimension);
                        List<int> subList = new List<int>();
                        foreach (GrammerTreeNode subSubNode in subNode.Children[1].Children)
                        {
                            int temInt = interpreteDimenExpression(subSubNode);
                            subList.Add(temInt);//add every dimen value to the list
                        }
                        table.Add("dimenList", subList);
                        //calculate the size
                        int unit = 1;
                        if ((TokenKind)table["type"] == TokenKind.IDENTIFIER)
                        { //struct 类型
                            unit = st_lookup_size((string)table["typeName"]);
                        }
                        else
                            unit = 1;
                        int result = unit;
                        foreach (int dimen in (List<int>)table["dimenList"])
                            result *= dimen;
                        table.Add("size", result);
                        //add the hashTable to the list
                        list.Add(table);
                    }
                    tem.hashTable.Add("fields", list);
                    tem.hashTable.Add("startLoc", loc);
                    int size = 0;
                    foreach (Hashtable temTable in (List<Hashtable>)tem.hashTable["fields"])
                    {
                        size += (int)temTable["size"];//calculate the struct size
                    }
                    tem.hashTable.Add("size", size);
                }
            }
            //insert this node to the table,MUST NOT FORGOT
            tem.next = hashTable[h];
            hashTable[h] = tem;
        }

        //insert the symbol node,SYMBOL
        public int st_insertSymbol(GrammerTreeNode node,int loc) {
            string name = node.Children[1].TerminalType.Image;

            int h = hash(name);
            BucketList l = hashTable[h];
            BucketList tem = null;
            while ((!l.isEmpty) && true)//for local dumply vars
            {
                l = l.next;
            }
            if (l.isEmpty) {
                //only the declare statement will change the symbol table
                if (node.NonterminalType == NonterminalType.DECLARE_STMT || node.NonterminalType == NonterminalType.STRUCT_DEC_STMT)//dec or struct dec
                { 
                    tem = new BucketList(name, SYMBOL_TYPE);
                    if (node.NonterminalType == NonterminalType.STRUCT_DEC_STMT)
                    {
                        tem.hashTable.Add("type", TokenKind.STRUCT);
                    }
                    else
                        tem.hashTable.Add("type", node.Children[0].TerminalType.Kind);//the symbol's kind
                    tem.hashTable.Add("typeName",node.Children[0].TerminalType.Image);//the name of the type
                    tem.hashTable.Add("loc",loc);
                    tem.hashTable.Add("dimen",node.Children[1].Dimension);
                    List<int> list = new List<int>();
                    foreach (GrammerTreeNode subNode in node.Children[1].Children) {
                        int temInt = interpreteDimenExpression(subNode);
                        list.Add(temInt);//add every dimen value to the list
                    }
                    tem.hashTable.Add("dimenList",list);
                    //为int类型的非数组符号加上值字段
                    if ((node.Children[0].TerminalType.Image.Equals("int")) && (node.Children[1].Dimension == 0))
                    {
                        if (node.Children.Count > 2) {
                            int intRes;
                            if (node.Children[2].NonterminalType != NonterminalType.FUNCTION_CALL_STMT)
                            {
                                intRes = interpreteDimenExpression(node.Children[2]);

                            }
                            else intRes = 0;
                            //将解释出来的值存入符号表中
                            tem.hashTable.Add("value", intRes);
                        }
                        
                    }
                    int unit = 1;
                    if ((TokenKind)tem.hashTable["type"] == TokenKind.STRUCT)
                    { //struct 类型
                         unit = st_lookup_size((string)tem.hashTable["typeName"]);
                    }
                    else
                        unit = 1;
                    int result = unit;
                    foreach (int dimen in (List<int>)tem.hashTable["dimenList"])
                        result *= dimen;
                    tem.hashTable.Add("size",result);//calculate the size              
                }

                //insert this node to the table
                tem.next = hashTable[h];
                hashTable[h] = tem;
                //update the value of loc
                loc = loc + (int)tem.hashTable["size"];
            }
            return loc;
        }

        //lookup a specified name 
        public BucketList st_lookup(string name) {
            int h = hash(name);
            BucketList l = hashTable[h];
            while ((!l.isEmpty) && (!l.name.Equals(name)))
            {
                l = l.next;
            }
            return l;
        }

        //look up size by name
        public int st_lookup_size(string name) {
            int h = hash(name);
            BucketList l = hashTable[h];
            while ((!l.isEmpty) && (!l.name.Equals(name)))
            {
                l = l.next;
            }
            return (int)l.hashTable["size"];
        }

        //st_lookUpStructType
        public string st_lookUpStructType(string oldType,string id) {
            string result = "";
            BucketList l = st_lookup(oldType);
            List<Hashtable> list = (List<Hashtable>)l.hashTable["fields"];
            for (int i = 0; i < list.Count; i++) {
                if (((string)list[i]["name"]).Equals(id)) {
                    result = (string)list[i]["typeName"];
                }
            }
            return result;
        }

        //look up para loc
        public int st_lookupParaLoc(string funName,string id) {
            BucketList l = st_lookup(funName);
            if (l.type == FUNC_TYPE) {
                List<Hashtable> paraList = (List<Hashtable>)l.hashTable["params"];
                for (int i = 0; i < paraList.Count;i++ )
                {
                    if (((string)paraList[i]["name"]).Equals(id)) {
                        return (1 + (paraList.Count-i));
                    }
                }
            }
            return -1;
        }
        //remove bucket
        public void st_remove(int loca) {
            for (int i = 0; i < SIZE; i++) {
                BucketList pointer = hashTable[i];
                BucketList last = null;
                while (!pointer.isEmpty) {
                    if ((pointer.type == SYMBOL_TYPE)&&((int)pointer.hashTable["loc"] >= loca))//remove the bucket whose loc is higher than loca
                    {
                        if (last == null)
                        {
                            hashTable[i] = pointer.next;
                            pointer = pointer.next;
                        }
                        else
                        {
                            last.next = pointer.next;
                            pointer = pointer.next;
                        }
                    }
                    else {
                        pointer = pointer.next;
                        last = pointer;//driver
                    }
                }
            }
        }

        ///print the symbol table
        public string st_print() {
            int i;
            string result = "";
            result += "\r\nSymbol table:\r\n\r\n";
            result += "Name        Type        IsEmpty        Hash Table\r\n";
            result += "----        ----        -------        ----------\r\n";
            for (i = 0; i < SIZE; ++i)
            {
                if (!hashTable[i].isEmpty)
                {
                    BucketList l = hashTable[i];
                    while (!l.isEmpty)
                    {
                        result += l.name;//print the name
                        for (int j = 0; j < 12 - l.name.Length; j++)
                            result += " ";
                        result += l.type.ToString();//the type
                        for (int j = 0; j < 12 - l.type.ToString().Length; j++)
                            result += " ";
                        result += l.isEmpty.ToString();
                        for (int j = 0; j < 15 - l.isEmpty.ToString().Length; j++)
                            result += " ";
                        
                        switch(l.type){
                                ///for the struct type
                            case STRUCT_TYPE:
                                result += ("startLoc:" + (int)l.hashTable["startLoc"] + "\r\n");
                                for (int j = 0; j < 39; j++) {
                                    result += " ";
                                }
                                result += ("size:" + (int)l.hashTable["size"] + "\r\n");
                                for (int j = 0; j < 39; j++)
                                {
                                    result += " ";
                                }
                                result += ("fields:" + "\r\n");
                           
                                foreach(Hashtable struct_table in (List<Hashtable>)l.hashTable["fields"]){
                                    for(int x=0;x<2;x++){
                                        for (int j = 0; j < 49; j++)
                                        {
                                            result += " ";
                                        }
                                        result += "|\r\n";
                                    }
                                    for (int j = 0; j < 49; j++)
                                    {
                                        result += " ";
                                    }
                                    result += "V\r\n";//for the link
                                    //print the data
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("type:" + ((TokenKind)struct_table["type"]).ToString() + "\r\n");
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("typeName:" + (string)struct_table["typeName"] + "\r\n");
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("name:" + (string)struct_table["name"] + "\r\n");
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("size:" + (int)struct_table["size"] + "\r\n");
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("dimen:" + (int)struct_table["dimen"] + "\r\n");
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("dimenList:");//print the dimen list
                                    foreach(int temInt in (List<int>)struct_table["dimenList"])
                                        result += (temInt + " ");
                                    result += ("\r\n");
                                }
                              
                                    break;
                            case SYMBOL_TYPE:
                                result += ("type:" + ((TokenKind)l.hashTable["type"]).ToString() + "\r\n");
                                for (int j = 0; j < 39; j++) {
                                    result += " ";
                                }
                                result += ("typeName:" + (string)l.hashTable["typeName"] + "\r\n");
                                for (int j = 0; j < 39; j++) {
                                    result += " ";
                                }
                                result += ("dimen:" + (int)l.hashTable["dimen"] + "\r\n");
                                for (int j = 0; j < 39; j++) {
                                    result += " ";
                                }
                                result += ("size:" + (int)l.hashTable["size"] + "\r\n");
                                for (int j = 0; j < 39; j++) {
                                    result += " ";
                                }
                                result += ("loc:" + l.hashTable["loc"].ToString() + "\r\n");
                                for (int j = 0; j < 39; j++) {
                                    result += " ";
                                }
                                result += ("dimenList:");
                                foreach (int temInt in (List<int>)l.hashTable["dimenList"])
                                    result += (temInt + " ");
                                result += "\r\n";
                                //判断是否是int类型的非数组结构
                                if (((string)l.hashTable["typeName"]).Equals("int") && ((int)l.hashTable["dimen"] == 0)) {
                                    for (int j = 0; j < 39; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("value:" + (int)l.hashTable["value"]);
                                }
                                result += "\r\n";
                                break;
                            case FUNC_TYPE:
                                result += ("type:" + ((TokenKind)l.hashTable["type"]).ToString() + "\r\n");
                                for (int j = 0; j < 39; j++) {
                                    result += " ";
                                }
                                result += ("typeName:" + (string)l.hashTable["typeName"] + "\r\n");
                                for (int j = 0; j < 39; j++) {
                                    result += " ";
                                }
                                result += ("startLoc:" + (int)l.hashTable["startLoc"] + "\r\n");
                                for (int j = 0; j < 39; j++) {
                                    result += " ";
                                }
                                result += ("params:" + "\r\n");
                                foreach (Hashtable para_table in (List<Hashtable>)l.hashTable["params"]) {
                                    for (int x = 0; x < 2; x++)
                                    {
                                        for (int j = 0; j < 49; j++)
                                        {
                                            result += " ";
                                        }
                                        result += "|\r\n";
                                    }
                                    for (int j = 0; j < 49; j++)
                                    {
                                        result += " ";
                                    }
                                    result += "V\r\n";//for the link
                                    //print the data
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("type:" + ((TokenKind)para_table["type"]).ToString() + "\r\n");
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("typeName:" + (string)para_table["typeName"] + "\r\n");
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("name:" + (string)para_table["name"] + "\r\n");
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("size:" + (int)para_table["size"] + "\r\n");
                                    for (int j = 0; j < 43; j++)
                                    {
                                        result += " ";
                                    }
                                    result += ("dimen:" + (int)para_table["dimen"] + "\r\n");
                                    result += "\r\n";
                                }
                                break;
                            default:
                                break;
                        }
                        //go to the next
                        l = l.next;
                        for (int k = 0; k < 53;k++ )
                            result += ("#");
                        result += "\r\n";
                        result += "\r\n";
                    }
                }
            }
            return result;
        }
        //interprete the dimen expression
        public int interpreteDimenExpression(GrammerTreeNode node) {
            int left, right;
            if (node.Type == TreeNodeType.NONTERMINAL && node.NonterminalType == NonterminalType.ARITHMEXPR)
            {//arithmetic expression
                left = interpreteDimenExpression(node.Children[0]);
                right = interpreteDimenExpression(node.Children[2]);
                if (node.Children[1].TerminalType.Kind == TokenKind.PLUS)
                    return (left + right);
                else //sub
                    return (left - right);
            }
            else if (node.Type == TreeNodeType.NONTERMINAL && node.NonterminalType == NonterminalType.TERM)
            {
                left = interpreteDimenExpression(node.Children[0]);
                right = interpreteDimenExpression(node.Children[2]);
                if (node.Children[1].TerminalType.Kind == TokenKind.MULTIPLY)
                    return (left * right);
                else if (node.Children[1].TerminalType.Kind == TokenKind.DIVIDE)
                    return (left / right);
                else //mode
                    return (left % right);
            }
            else if (node.Type == TreeNodeType.NONTERMINAL && node.NonterminalType == NonterminalType.UNARY) {//unary expression
                if (node.Children[0].TerminalType.Kind == TokenKind.MINUS)
                    return (0 - interpreteDimenExpression(node.Children[1]));
                return (0 - interpreteDimenExpression(node.Children[1]));
            }
            else if (node.Type == TreeNodeType.TERMINAL)
            {
                if (node.TerminalType.Kind == TokenKind.INT_CONSTANT)
                    return node.IntValue;//real value is denied
                //如果是identifier就取出它的值
                if (node.TerminalType.Kind == TokenKind.IDENTIFIER) {
                    //找到bucketlist
                    BucketList temL = lookUp(node.TerminalType.Image);
                    if ((((string)temL.hashTable["typeName"]).Equals("int")) && (((int)temL.hashTable["dimen"]) == 0)) {
                        return (int)temL.hashTable["value"];//返回查出的符号表的值
                    }
                }
                return node.IntValue;
            }
            else {
                return interpreteDimenExpression(node.Children[0]);
            }
        }

        ///interpret the array element location
        public int interpretElementLocation(GrammerTreeNode node)
        {
            BucketList l = lookUp(node.TerminalType.Image);
            if((node.Type == TreeNodeType.NONTERMINAL)&&(node.TerminalType.Kind == TokenKind.IDENTIFIER))
            {
                //lookup the identifier
                int relaLoc = 0;
                for (int i = 0; i < node.Children.Count; i++)
                {
                    int expResult = interpreteDimenExpression(node.Children[i]);
                    int termUnit = 1;
                    for (int j = i + 1; j < (int)l.hashTable["dimen"]; j++)
                    {
                        termUnit *= ((List<int>)l.hashTable["dimenList"])[j];
                    }
                    expResult *= termUnit;
                    relaLoc += expResult;//calculate the relative location
                }
                if((TokenKind)l.hashTable["type"] == TokenKind.STRUCT)
                    relaLoc *= (int)l.hashTable["size"];//for the struct
                return (((int)l.hashTable["loc"]) + relaLoc);
            }
            return (int)l.hashTable["loc"];
        }

        ///interpret the struct member location
        public int interpretStructMemLocation(string structName,GrammerTreeNode node) {
            BucketList l = lookUp(structName);
            int location = -1;
            List<Hashtable> fields = (List<Hashtable>)l.hashTable["fields"];
            for (int i = 0; i < fields.Count; i++) {
                if (((string)fields[i]["name"]).Equals(node.TerminalType.Image)) {
                    location = 0;
                    for (int j = 0; j < i; j++) {
                        location += (int)fields[j]["size"];
                    }//add the prior size

                    ///for the array
                    int relaLoc = 0;
                    for (int ii = 0; ii < node.Children.Count; ii++)
                    {
                        int expResult = interpreteDimenExpression(node.Children[ii]);
                        int termUnit = 1;
                        for (int j = ii + 1; j < (int)fields[i]["dimen"]; j++)
                        {
                            termUnit *= ((List<int>)l.hashTable["dimenList"])[j];
                        }
                        expResult *= termUnit;
                        relaLoc += expResult;//calculate the relative location
                    }
                    if ((TokenKind)fields[i]["type"] == TokenKind.STRUCT || (TokenKind)fields[i]["type"] == TokenKind.IDENTIFIER)
                        relaLoc *= lookUpSize((string)fields[i]["typeName"]);//for the struct
                    location += relaLoc;
                    break;
                }
            }
            return location;
        }

        //interpret struct member location
        public int interpretStructMemberLocation(GrammerTreeNode node) {
            int loc = -1;
            if ((node.Type == TreeNodeType.NONTERMINAL) && (node.NonterminalType == NonterminalType.STRUCT_MEM_DESC))
            {
                //for the struct desc
                GrammerTreeNode memNode = node;
                loc = lookUpLoc(memNode.Children[0].TerminalType.Image);
                string oldType = (string)lookUp(memNode.Children[0].TerminalType.Image).hashTable["typeName"];
                for (int i = 1; i < memNode.Children.Count; i++)
                {
                    //查出结构名称
                    string typeName = lookUpStructType(oldType, memNode.Children[i].TerminalType.Image);
                    int temInt = interpretStructMemLocation(oldType, memNode.Children[i]);
                    loc += temInt;
                    oldType = typeName;
                }
            }
            return loc;
        }
    }
}
