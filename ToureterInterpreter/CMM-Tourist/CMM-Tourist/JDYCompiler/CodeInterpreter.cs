using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JDYCompiler
{
    //指令的类别
    internal enum OPCLASS {
        opclRR,     /* reg operands r,s,t */
        opclRM,     /* reg r, mem d+s */
        opclRA      /* reg r, int d+s */
    }
    //指令集
    internal enum OPCODE {
        /* RR instructions */
        opHALT,    /* RR     halt, operands are ignored */ //0
        opIN,      /* RR     read into reg(r); s and t are ignored *///1
        opOUT,     /* RR     write from reg(r), s and t are ignored *///2
        opADD,    /* RR     reg(r) = reg(s)+reg(t) *///3
        opSUB,    /* RR     reg(r) = reg(s)-reg(t) *///4
        opMUL,    /* RR     reg(r) = reg(s)*reg(t) *///5
        opDIV,    /* RR     reg(r) = reg(s)/reg(t) *///6
        opMOD,    /* RR     reg(r) = reg(s)%reg(t) *///7
        opRRLim,   /* limit of RR opcodes *///8

        /* RM instructions */
        opLD,      /* RM     reg(r) = mem(d+reg(s)) *///9
        opST,      /* RM     mem(d+reg(s)) = reg(r) *///10
        opRMLim,   /* Limit of RM opcodes *///11

        /* RA instructions */
        opLDA,     /* RA     reg(r) = d+reg(s) *///12
        opLDC,     /* RA     reg(r) = d ; reg(s) is ignored *///13
        opJLT,     /* RA     if reg(r)<0 then reg(7) = d+reg(s) *///14
        opJLE,     /* RA     if reg(r)<=0 then reg(7) = d+reg(s) *///15
        opJGT,     /* RA     if reg(r)>0 then reg(7) = d+reg(s) *///16
        opJGE,     /* RA     if reg(r)>=0 then reg(7) = d+reg(s) *///17
        opJEQ,     /* RA     if reg(r)==0 then reg(7) = d+reg(s) *///18
        opJNE,     /* RA     if reg(r)!=0 then reg(7) = d+reg(s) *///19
        opRALim    /* Limit of RA opcodes *///20
    }

    //指令执行后状态
    internal enum STEPRESULT {
        srOKAY,
        srHALT,
        srIMEM_ERR,
        srDMEM_ERR,
        srZERODIVIDE
    }
    //表示指令的instruction集合
    internal sealed class INSTRUCTION {
        public INSTRUCTION() { }
        public int iop;
        public int iarg1;
        public int iarg2;
        public int iarg3;
        public string realImg = "";
    }

    //表示存储单元的内部struct
    internal sealed class UNIT{
        public UNIT() {
            type = 0;
            int_value = 0;
            real_value = 0f;
        }
        public UNIT(int value) {
            type = 0;
            int_value = value;
        }
        public UNIT(float value) {
            type = 1;
            real_value = value;
        }
        public int type;//0是整数，1是浮点数
        public int int_value;
        public float real_value;
    }

    internal sealed class CodeInterpreter
    {
        #region
        public static int TRUE = 1;
        public static int FALSE = 0;
        //instruction memory size
        public static int IADDR_SIZE = 1024;
        public static int DADDR_SIZE = 1024;
        public static int NO_REGS = 8;
        public static int PC_REG = 7;

        public static int LINESIZE = 1200;
        public static int WORDSIZE = 20;

        /******** vars ********/
        public static int iloc = 0 ;//指令位置
        public static int dloc = 0 ;//数据位置
        public static int traceflag = FALSE;
        public static int icountflag = FALSE;

        public static INSTRUCTION[] iMem = new INSTRUCTION[IADDR_SIZE];//指令存储器
        public static UNIT[] dMem = new UNIT[DADDR_SIZE];//数据存储器
        public static UNIT[] reg = new UNIT[NO_REGS];//指令寄存器
        //表示指令的string数组
        public static string[] opCodeTab = 
        {"HALT","IN","OUT","ADD","SUB","MUL","DIV","MOD","????",
            /* RR opcodes */
           "LD","ST","????", /* RM opcodes */
           "LDA","LDC","JLT","JLE","JGT","JGE","JEQ","JNE","????"
           /* RA opcodes */
          };
        //表示执行结果的字符串
        public static string[] stepResultTab = 
        {"OK","Halted","Instruction Memory Fault",
           "Data Memory Fault","Division by 0"
          };

        public static char[] in_Line = new char[LINESIZE] ;
        public static int lineLen ;
        public static int inCol  ;//列号
        public static int num  ;
        public static string realString = "";
        public static string word = "" ;
        public static char ch  ;//当前取得的字符
        public static int done;
        #endregion
        //执行结果字符串
        private string resultString = "";

        public CodeInterpreter() { 
            //初始化
            iloc = 0 ;//指令位置
           dloc = 0 ;//数据位置
           traceflag = FALSE;
           icountflag = FALSE;

           iMem = new INSTRUCTION[IADDR_SIZE];//指令存储器
         dMem = new UNIT[DADDR_SIZE];//数据存储器
          reg = new UNIT[NO_REGS];//指令寄存器

            in_Line = new char[LINESIZE] ;
           lineLen=0 ;
           inCol  =0;//列号
           num  =0;
           realString = "";
           word = "" ;
           ch='\0'  ;//当前取得的字符
           done=0;
               resultString = "";
        }


        //确定指令的类型
        int opClass(int c)
        {
            if (c <= (int)OPCODE.opRRLim) return ((int)OPCLASS.opclRR);
            else if (c <= (int)OPCODE.opRMLim) return ((int)OPCLASS.opclRM);
            else return ((int)OPCLASS.opclRA);
        } /* opClass */

        //取得字符
        void getCh ()
        { if (++inCol < lineLen)
        ch = in_Line[inCol] ;
        else ch = ' ' ;
        } /* getCh */
        /********************************************/
        //取得下一个非空字符并判断是否到达行的末尾
        int nonBlank ()
        { while ((inCol < lineLen)
            && (in_Line[inCol] == ' ') )
            inCol++ ;
          if (inCol < lineLen)
          { 
            ch = in_Line[inCol] ;
            return TRUE ; 
          }
          else
          { 
            ch = ' ' ;
            return FALSE ; 
          }
        } /* nonBlank */
        /********************************************/
        int getNum ()
        { 
            int sign;
            int term;
            int temp = FALSE;
            num = 0 ;
            realString = "";
            do
            { 
                sign = 1;
                while ( (nonBlank() == 1)&& ((ch == '+') || (ch == '-')) )
                { 
                    temp = FALSE ;
                    if (ch == '-')  sign = - sign ;
                    getCh();
                }
                term = 0 ;
                nonBlank();
                while (ch > 47 && ch < 58)
                { 
                    temp = TRUE ;
                    term = term * 10 + ( ch - '0' ) ;
                    realString += ch;//加上ch
                    getCh();
                }
                num = num + (term * sign) ;
                if (ch == '.') {
                    realString += ch;
                    getCh();
                    while (ch > 47 && ch < 58) {
                        realString += ch;//加上ch
                        getCh();
                    }
                }
            } while ( (nonBlank() == 1) && ((ch == '+') || (ch == '-')) ) ;
                return temp;
        } /* getNum */

        int getWord ()
        { 
            int temp = FALSE;
            int length = 0;
            word = "";
            if (nonBlank () == 1)
            {
                //ch为字符或者数字
                while ((ch > 47 && ch < 58) || ((ch > 64 && ch < 91) || (ch > 96 && ch < 123)))
                { 
                    //if (length < WORDSIZE-1) word [length++] =  ch ;
                    word += ch;
                    getCh() ;
                }   
                //word[length] = '\0';
                temp = (word != "")?1:0;
            }
            return temp;
        } /* getWord */

        //判断参数char c是否是下一个字符，如果是的话就跳过且返回true，如果不是那么返回false
        int skipCh(char c)
        {
            int temp = FALSE;
            if ((nonBlank() == 1) && (ch == c))
            {
                getCh();
                temp = TRUE;
            }
            return temp;
        } /* skipCh */

        //判断是否处于行的末尾
        int atEOL()
        { 
            return ( 1-nonBlank ());
        } /* atEOL */

        //打印出错信息
        int error(string msg, int lineNo, int instNo)
        {
            resultString += ("Line " + lineNo);
            if (instNo >= 0) resultString += (" (Instruction " + instNo + ")");
            resultString += ("   " + msg + "\r\n");
            return FALSE;
        } /* error */

        //将中间代码读取到
        /********************************************/
        int readInstructions (string codeStr)
        {
            //将中间代码分割为数组
            codeStr = codeStr.Replace("\r\n", "\n");
            string[] insArr = codeStr.Split('\n');

            OPCODE op;
            int arg1=0, arg2=0, arg3=0;
            string temImg = "";
            int loc, regNo, lineNo;
            //初始化寄存器
            for (regNo = 0 ; regNo < NO_REGS ; regNo++)
                reg[regNo] =  new UNIT();
            dMem[0] = new UNIT(DADDR_SIZE - 1);
            //初始化数据存储
            for (loc = 1 ; loc < DADDR_SIZE ; loc++)
                dMem[loc] = new UNIT() ;
            for (loc = 0 ; loc < IADDR_SIZE ; loc++)
            {
                iMem[loc] = new INSTRUCTION();
                iMem[loc].iop = (int)OPCODE.opHALT;
                iMem[loc].iarg1 = 0 ;
                iMem[loc].iarg2 = 0 ;
                iMem[loc].iarg3 = 0 ;
            }
            lineNo = 0 ;
            
            int insIndex = 0;
            while(insIndex < insArr.Length)
            {
                //重置in_Line数组
                for (int i = 0; i < in_Line.Length; i++)
                    in_Line[i] = '\0';
                //将字符数组中的字符拷贝进in_Line数组中
                for(int i=0;i<insArr[insIndex].ToCharArray().Length;i++)
                    in_Line[i] = (insArr[insIndex].ToCharArray())[i];
                inCol = 0 ; 
                lineNo++;
                lineLen = insArr[insIndex].Length - 1;
                //将字符数组的最后一个字符设置为'\0'
                in_Line[++lineLen] = '\0';
                if ( (nonBlank() == 1) && (in_Line[inCol] != '*') )
                { 
                    if (getNum() == 0)
                        return error("Bad location", lineNo,-1);
                    loc = num;
                    if (loc > IADDR_SIZE)
                        return error("Location too large",lineNo,loc);
                    if (skipCh(':') == 0)
                        return error("Missing colon", lineNo,loc);
                    if (getWord () == 0)
                        return error("Missing opcode", lineNo,loc);
                    op = OPCODE.opHALT ;
                    while ((op < OPCODE.opRALim)
                        && (!opCodeTab[(int)op].Equals(word)))//不相等的时候
                        op++ ;
                    if (!opCodeTab[(int)op].Equals(word))
                        return error("Illegal opcode", lineNo,loc);
                    switch ( opClass((int)op) )
                    { 
                        case (int)OPCLASS.opclRR :
                            if ( (getNum () == 0) || (num < 0) || (num >= NO_REGS) )
                                return error("Bad first register", lineNo,loc);
                            arg1 = num;
                            if ( skipCh(',') == 0)
                                return error("Missing comma", lineNo, loc);
                            if ( (getNum () == 0) || (num < 0) || (num >= NO_REGS) )
                                return error("Bad second register", lineNo, loc);
                            arg2 = num;
                            if (skipCh(',') == 0) 
                                return error("Missing comma", lineNo,loc);
                            if ( (getNum () == 0) || (num < 0) || (num >= NO_REGS) )
                                return error("Bad third register", lineNo,loc);
                            arg3 = num;
                            break;

                        case (int)OPCLASS.opclRM:
                        case (int)OPCLASS.opclRA:

                            if ( (getNum () == 0) || (num < 0) || (num >= NO_REGS) )
                                return error("Bad first register", lineNo,loc);
                            arg1 = num;
                            if (skipCh(',') == 0)
                                return error("Missing comma", lineNo,loc);
                            if (getNum () == 0)
                                return error("Bad displacement", lineNo,loc);
                            arg2 = num;
                            //将数字的img保存
                            temImg = realString;
                            if ( (skipCh('(') == 0) && (skipCh(',') == 0) )
                                return error("Missing LParen", lineNo,loc);
                            if ( (getNum () == 0) || (num < 0) || (num >= NO_REGS))
                                return error("Bad second register", lineNo,loc);
                            arg3 = num;
                            break;
                        }
                        iMem[loc].iop = (int)op;
                        iMem[loc].iarg1 = arg1;
                        iMem[loc].iarg2 = arg2;
                        iMem[loc].iarg3 = arg3;
                    //保存浮点数的img
                        iMem[loc].realImg = temImg;
                    }
                insIndex++;//下标增加
                }
                return TRUE;
            } /* readInstructions */

        //执行函数
        /********************************************/
        STEPRESULT stepTM ()
        { 
            INSTRUCTION currentinstruction  ;
            int pc;
            int r=0,s=0,t=0,m=0;
            int ok;
            //设置程序计数器的值
            pc = reg[PC_REG].int_value ;
            if ( (pc < 0) || (pc > IADDR_SIZE)  )
                return STEPRESULT.srIMEM_ERR;
            reg[PC_REG].int_value = pc + 1 ;
            currentinstruction = iMem[ pc ] ;
            switch (opClass(currentinstruction.iop) )
            { 
                case (int)OPCLASS.opclRR :
                /***********************************/
                    r = currentinstruction.iarg1 ;
                    s = currentinstruction.iarg2 ;
                    t = currentinstruction.iarg3 ;
                    break;

                case (int)OPCLASS.opclRM:
                /***********************************/
                    r = currentinstruction.iarg1 ;
                    s = currentinstruction.iarg3 ;
                    m = currentinstruction.iarg2 + reg[s].int_value ;
                    if ( (m < 0) || (m > DADDR_SIZE))
                        return STEPRESULT.srDMEM_ERR;
                    break;

                case (int)OPCLASS.opclRA:
                /***********************************/
                    r = currentinstruction.iarg1 ;
                    s = currentinstruction.iarg3 ;
                    m = currentinstruction.iarg2 + reg[s].int_value ;
                    break;
            } /* case */

            switch ( currentinstruction.iop)
            { /* RR instructions */
                case (int)OPCODE.opHALT :
                /***********************************/
                    //resultString += "HALT: " + r + "," + s + "," + t + "\r\n";
                    return STEPRESULT.srHALT;
                    /* break; */

                case (int)OPCODE.opIN:
                /***********************************/
                    string prompt="Enter value for IN instruction: ";
                    do
                    {
                        string input = Microsoft.VisualBasic.Interaction.InputBox(prompt, "Toureter", "Default", 0, 0);
                        resultString += ("Enter value for IN instruction: \r\n");

                        in_Line=input.ToArray();
                        lineLen = in_Line.Length ;
                        inCol = 0;
                        ok = getNum();
                        if ( ok==0) prompt="Illegal value\nEnter value for IN instruction: ";
                        else {
                            reg[r] = new UNIT(num);
                        }
                    }
                    while (ok == 0);
           
                    break;

                case (int)OPCODE.opOUT:  
                    resultString += ("OUT instruction prints: " + (reg[r].type==0?reg[r].int_value:reg[r].real_value) + "\r\n"  );
                    break;
                case (int)OPCODE.opADD:
                    if (reg[s].type == 1 || reg[t].type == 1)
                        reg[r].type = 1;
                    else
                        reg[r].type = 0;
                    if (reg[r].type == 0) {
                        reg[r].int_value = ((int)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) + ((int)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                    }else
                        reg[r].real_value = ((float)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) + ((float)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                    break;
                case (int)OPCODE.opSUB: 
                    if (reg[s].type == 1 || reg[t].type == 1)
                        reg[r].type = 1;
                    else
                        reg[r].type = 0;
                    if (reg[r].type == 0) {
                        reg[r].int_value = ((int)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) - ((int)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                    }else
                        reg[r].real_value = ((float)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) - ((float)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                    break;
                case (int)OPCODE.opMUL: 
                    if (reg[s].type == 1 || reg[t].type == 1)
                        reg[r].type = 1;
                    else
                        reg[r].type = 0;
                    if (reg[r].type == 0) {
                        reg[r].int_value = ((int)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) * ((int)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                    }else
                        reg[r].real_value = ((float)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) * ((float)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                    break;

                case (int)OPCODE.opDIV:
                /***********************************/
                    if ((reg[t].type == 0 && reg[t].int_value != 0) || (reg[t].type == 1 && reg[t].real_value != 0f)) {
                        if (reg[s].type == 1 || reg[t].type == 1)
                            reg[r].type = 1;
                        else
                            reg[r].type = 0;
                        if (reg[r].type == 0)
                        {
                            reg[r].int_value = ((int)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) / ((int)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                        }
                        else
                            reg[r].real_value = ((float)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) / ((float)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                    }
                    else return STEPRESULT.srZERODIVIDE;
                    break;
                case (int)OPCODE.opMOD:
                    if ((reg[t].type == 0 && reg[t].int_value != 0) || (reg[t].type == 1 && reg[t].real_value != 0f))
                    {
                        if (reg[s].type == 1 || reg[t].type == 1)
                            reg[r].type = 1;
                        else
                            reg[r].type = 0;
                        if (reg[r].type == 0)
                        {
                            reg[r].int_value = ((int)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) % ((int)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                        }
                        else
                            reg[r].int_value = ((int)(reg[s].type == 0 ? reg[s].int_value : reg[s].real_value)) % ((int)(reg[t].type == 0 ? reg[t].int_value : reg[t].real_value));
                    }
                    else return STEPRESULT.srZERODIVIDE;
                    break;

                /*************** RM instructions ********************/
                case (int)OPCODE.opLD: 
                    reg[r].type = dMem[m].type;
                    reg[r].int_value = dMem[m].int_value;
                    reg[r].real_value = dMem[m].real_value;
                    break;
                case (int)OPCODE.opST: 
                    dMem[m].type = reg[r].type;
                    dMem[m].int_value = reg[r].int_value;
                    dMem[m].real_value = reg[r].real_value;
                    break;

                /*************** RA instructions ********************/
                case (int)OPCODE.opLDA:
                    reg[r].type = 0;
                    reg[r].int_value = m; 
                    break;
                case (int)OPCODE.opLDC:
                    if (currentinstruction.realImg.Contains('.'))
                    {
                        reg[r].type = 1;
                        reg[r].real_value = float.Parse(currentinstruction.realImg);
                    }
                    else {
                        reg[r].type = 0;
                        reg[r].int_value = currentinstruction.iarg2;
                    }
                    break;
                case (int)OPCODE.opJLT:
                    if (((reg[r].type == 0) && (reg[r].int_value < 0)) || ((reg[r].type == 1) && (reg[r].real_value < 0f))) 
                        reg[PC_REG].int_value = m; 
                    break;
                case (int)OPCODE.opJLE:
                    if (((reg[r].type == 0) && (reg[r].int_value <= 0)) || ((reg[r].type == 1) && (reg[r].real_value <= 0f)))
                        reg[PC_REG].int_value = m; 
                    break;
                case (int)OPCODE.opJGT:
                    if (((reg[r].type == 0) && (reg[r].int_value > 0)) || ((reg[r].type == 1) && (reg[r].real_value > 0f)))
                        reg[PC_REG].int_value = m; 
                    break;
                case (int)OPCODE.opJGE:
                    if (((reg[r].type == 0) && (reg[r].int_value >= 0)) || ((reg[r].type == 1) && (reg[r].real_value >= 0f)))
                        reg[PC_REG].int_value = m; 
                    break;
                case (int)OPCODE.opJEQ:
                    if (((reg[r].type == 0) && (reg[r].int_value == 0)) || ((reg[r].type == 1) && (reg[r].real_value == 0f)))
                        reg[PC_REG].int_value = m; 
                    break;
                case (int)OPCODE.opJNE:
                    if (((reg[r].type == 0) && (reg[r].int_value != 0)) || ((reg[r].type == 1) && (reg[r].real_value != 0f)))
                        reg[PC_REG].int_value = m;  
                    break;

                /* end of legal instructions */
                } /* case */
                return STEPRESULT.srOKAY ;
            } /* stepTM */

        //执行指令
        int doCommand() {
            int stepcnt = 1;
            int stepResult;
            stepResult = (int)STEPRESULT.srOKAY;
            char cmd = 'g';
            if (stepcnt > 0)
            {
                if (cmd == 'g')
                {
                    stepcnt = 0;
                    while (stepResult == (int)STEPRESULT.srOKAY)
                    {
                        iloc = reg[PC_REG].int_value;
                        //if (traceflag!=0) writeInstruction(iloc);
                        stepResult = (int)stepTM();
                        stepcnt++;
                    }
                }
                else
                {
                    while ((stepcnt > 0) && (stepResult == (int)STEPRESULT.srOKAY))
                    {
                        iloc = reg[PC_REG].int_value;
                        //if (traceflag) writeInstruction(iloc);
                        stepResult = (int)stepTM();
                        stepcnt--;
                    }
                }
                //printf("%s\n", stepResultTab[stepResult]);
            }
            return TRUE;
        }

        //入口函数
        public string interprete(string codeString) {
            //读取指令
            if (readInstructions(codeString) == 0)
                return "";
            //执行指令
            doCommand();
            return resultString;
        }

        

    }
}
