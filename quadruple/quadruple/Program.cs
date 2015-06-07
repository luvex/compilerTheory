using System;
using System.IO;

namespace quadruple
{

	class MainClass
	{
		#region defination of tokens
		public enum type{IF,ELSE,WHILE,FOR,AND,OR,LESS,MORE,LLESS,LMORE,PLUS,MINUS,MULTIPLY,DIVISE,ASSIGN,ID};

		public class Wtoken
		{
			public type t;
			public char ch;
		}
		#endregion

		#region defination of two kinds of quadruple
		public enum OpKind{J,JNZ,JMORE,JLESS,JLMORE,JLLESS,PLUS,MINUS,MULTIPLY,DIVISE,ASSIGN};
		public class quadruple
		{
			public int label;
			public OpKind op;
			public char par1,par2;
		}
		public class Aquadruple:quadruple
		{
			public int address;
		}
		public class Rquadruple:quadruple
		{
			public char result;
		}
		#endregion

		#region Other definations
		public const int MAX_TOKEN=256;
		public const int MAX_QUAD=256;

		public static Wtoken[] tokenTable = new Wtoken[MAX_TOKEN];
		public static quadruple[] quadTable = new quadruple[MAX_QUAD];

		public static int token_index;
		public static int total_len;

		public static int quad_len;
		public static int quad_index;

		public static int label;

		public static string inputstring;
		public static FileStream input;

		public static int input_index;
		public static int input_len;
		public static Wtoken cur=new Wtoken();
		#endregion

		public static void Main (string[] args)
		{
			if (!Initialize ()) 
			{
				Console.WriteLine ("Initialize failed!");
				return;
			}
			if (!LexicalAnalysis ()) 
			{
				Console.WriteLine ("Lexical Analysis failed!");
				return;
			}
			char ch;
			Console.WriteLine ("The result of lexical analysis is:");
			printLexical ();
			Console.WriteLine ("\n");
			nexttoken ();
			Console.WriteLine ("The grammer is:");
			syntaxAnalysis ();
			Console.WriteLine ("The quadruple is:");
			printQuadRuple ();

		}

		public static Boolean Initialize()
		{
			token_index = 0;
			total_len = 0;
			quad_len = 0;
			quad_index = 0;
			label = 100;

			Console.WriteLine ("Please input the file you want to translate.");
			string FilePath = Console.ReadLine ();
			try
			{
				input = new FileStream (FilePath, FileMode.Open);
			}
			catch(Exception) 
			{
				Console.WriteLine ("File not exists!");
				return false;
			}
			StreamReader sr = new StreamReader (input);
			string temp = "";
			while ((temp=sr.ReadLine ()) != null) 
			{
				inputstring += temp;
			}
			sr.Close ();
			inputstring = inputstring.Replace (" ", "");
			input_index = 0;
			input_len = inputstring.Length;
			Console.WriteLine (inputstring);
			return true;

		}

		public static Boolean LexicalAnalysis()
		{
			char ch;
			while (true) 
			{
				ch = inputstring [input_index++];
				if(input_index>=input_len)
					break;
				tokenTable [total_len] = new Wtoken ();
				if (ch == 'i')
				{
					if (inputstring[input_index] == 'f')
					{
						tokenTable[total_len++].t = type.IF;
						input_index++;
					}
					else
					{
						tokenTable[total_len].ch = 'i';
						tokenTable[total_len++].t = type.ID;
					}
				}
				else if (ch == 'w')
				{
					if (inputstring[input_index] == 'h')
					{
						tokenTable[total_len++].t = type.WHILE;
						input_index = input_index + 4;
					}
					else
					{
						tokenTable[total_len].ch = 'w';
						tokenTable[total_len++].t = type.ID;
					}
				}
				else if (ch == 'e')
				{
					if (inputstring[input_index] == 'l')
					{
						tokenTable[total_len++].t = type.ELSE;
						input_index = input_index + 3;
					}
					else
					{
						tokenTable[total_len].ch = 'e';
						tokenTable[total_len++].t = type.ID;
					}
				}
				else if (ch == 'f')
				{
					if (inputstring[input_index] == 'o')
					{
						tokenTable[total_len++].t = type.FOR;
						input_index = input_index + 2;
					}
					else
					{
						tokenTable[total_len].ch = 'f';
						tokenTable[total_len++].t = type.ID;
					}
				}
				else if (ch == '∧')
					tokenTable[total_len++].t = type.AND;
				else if (ch == '∨')
					tokenTable[total_len++].t = type.OR;
				else if (ch == '+')
					tokenTable[total_len++].t = type.PLUS;
				else if (ch == '-')
					tokenTable[total_len++].t = type.MINUS;
				else if (ch == '*')
					tokenTable[total_len++].t = type.MULTIPLY;
				else if (ch == '/')
					tokenTable[total_len++].t = type.DIVISE;
				else if (ch == '=')
					tokenTable[total_len++].t = type.ASSIGN;
				else if (ch == '>')
				{
					if (inputstring[input_index] == '=')
					{
						tokenTable[total_len++].t = type.LMORE;
						input_index++;
					}
					else
						tokenTable[total_len++].t = type.MORE;
				}
				else if (ch == '<')
				{
					if (inputstring[input_index] == '=')
					{
						tokenTable[total_len++].t = type.LLESS;
						input_index++;
					}
					else
						tokenTable[total_len++].t = type.LESS;
				}
				else if (ch >= 'a' && ch <= 'z')
				{
					tokenTable[total_len].t = type.ID;
					tokenTable[total_len++].ch = ch;
				}
			}
			return true;
		}

		public static void printLexical()
		{
			for(token_index=0;token_index<total_len;token_index++)
			{
				if (tokenTable[token_index].t == type.ID)
					Console.WriteLine("标识符 {0}", tokenTable[token_index].ch);
				else if (tokenTable[token_index].t == type.WHILE)
					Console.WriteLine("关键字 while");
				else if (tokenTable[token_index].t == type.IF)
					Console.WriteLine("关键字 if");
				else if (tokenTable[token_index].t == type.ELSE)
					Console.WriteLine("关键字 else");
				else if (tokenTable[token_index].t == type.FOR)
					Console.WriteLine("关键字 for");
				else if (tokenTable[token_index].t == type.PLUS)
					Console.WriteLine("运算符 +");
				else if (tokenTable[token_index].t == type.MINUS)
					Console.WriteLine("运算符 -");
				else if (tokenTable[token_index].t == type.MULTIPLY)
					Console.WriteLine("运算符 *");
				else if (tokenTable[token_index].t == type.DIVISE)
					Console.WriteLine("运算符 /");
				else if (tokenTable[token_index].t == type.AND)
					Console.WriteLine("运算符 ∧");
				else if (tokenTable[token_index].t == type.OR)
					Console.WriteLine("运算符 ∨");
				else if (tokenTable[token_index].t == type.ASSIGN)
					Console.WriteLine("运算符 =");
				else if (tokenTable[token_index].t == type.MORE)
					Console.WriteLine("运算符 >");
				else if (tokenTable[token_index].t == type.LMORE)
					Console.WriteLine("运算符 >=");
				else if (tokenTable[token_index].t == type.LESS)
					Console.WriteLine("运算符 <");
				else if (tokenTable[token_index].t == type.LLESS)
					Console.WriteLine("运算符 <=");
			}
			token_index = 0;
		}

		public static Boolean nexttoken()
		{
			if (token_index >= total_len)
				return false;
			cur.t = tokenTable[token_index].t;
			cur.ch = tokenTable[token_index].ch;
			token_index++;
			return true;
		}

		public static void syntaxAnalysis()
		{
			S(-1, 1000);
		}

		public static void S(int begin,int next)
		{
			if (cur.t == type.ID)
			{
				char a, b;
				Console.Write("S->{0}", cur.ch);
				a = cur.ch;
				if (!nexttoken())
					Console.WriteLine("\nerror!");
				if (cur.t != type.ASSIGN)
					Console.WriteLine("\nerror!");
				Console.WriteLine("=");
				if (!nexttoken())
					Console.WriteLine("\nerror!");
				if (cur.t != type.ID)
					Console.WriteLine("\nerror!");
				Console.WriteLine(cur.ch);
				b = cur.ch;
				AD_RESULT(begin, OpKind.ASSIGN, b, '_', a);
				AD_ADDRESS(-1, OpKind.J, '_', '_', next);
				nexttoken();
			}
			else if (cur.t == type.IF)
			{
				if (!nexttoken())
					Console.WriteLine("\nerror");
				Console.WriteLine("S->if(E){S}else{S}\n");
				int etrue = newlabel();
				int efalse = newlabel();
				E(begin, etrue, efalse);
				S(etrue, next);
				if (cur.t == type.ELSE)
				{
					if (!nexttoken())
						Console.WriteLine("\nerror");
					S(efalse, next);
				}
				else
					Console.WriteLine("\nerror");
			}
		}
		public static void E(int begin,int etrue,int efalse)
		{
			if (cur.t == type.ID)
			{
				char a, b;
				int mark = 0;
				a = cur.ch;
				Console.Write("E->{0}", cur.ch);
				if (!nexttoken())
					Console.WriteLine("\nerror");
				if (cur.t == type.MORE)
				{
					Console.Write(">");
					mark = 1;
				}
				else if (cur.t == type.LESS)
					Console.Write("<");
				else
					Console.WriteLine("\nerror");
				if (!nexttoken())
					Console.WriteLine("\nerror");
				if (cur.t != type.ID)
					Console.WriteLine("\nerror");
				Console.WriteLine(cur.ch);
				b = cur.ch;
				if (mark == 0)
					AD_ADDRESS(begin, OpKind.JLESS, a, b, etrue);
				if (mark == 1)
					AD_ADDRESS(begin, OpKind.JMORE, a, b, etrue);
				AD_ADDRESS(-1, OpKind.J, '_', '_', efalse);
				if (!nexttoken())
					Console.WriteLine("\nerror");
			}
			else
				Console.WriteLine("\nerror");
		}

		public static void printQuadRuple()
		{
		}

		public static int newlabel()
		{
			return label++;
		}
		public static void AD_RESULT(int nlabel,OpKind nop,char npar1,char npar2,char nresult)
		{
			Rquadruple temp = new Rquadruple();
			temp.label = nlabel;
			temp.op = nop;
			temp.par1 = npar1;
			temp.par2 = npar2;
			temp.result = nresult;
			quadTable[quad_len++] = temp;
		}
		public static void AD_ADDRESS(int nlabel,OpKind nop,char npar1,char npar2,int naddress)
		{
			Aquadruple temp = new Aquadruple();
			temp.label = nlabel;
			temp.op = nop;
			temp.par1 = npar1;
			temp.par2 = npar2;
			temp.address = naddress;
			quadTable[quad_len++] = temp;
		}
	}


}
