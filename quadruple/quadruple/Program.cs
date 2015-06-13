using System;
using System.Collections;
using System.Collections.Generic;

namespace quadruple
{
	class MainClass
	{
        public static List<char> VT=new List<char>();
        public static void initVT()
        {
            for (char i = 'a'; i <= 'z'; i++)
                VT.Add(i);
        }

		public class pos
		{
			public int value;
			public pos next;
		}

		public class node
		{
			public char type;
			public pos tc;
			public pos fc;
		}

        public class quad
        {
            public int label;
            public string type;
            public char a;
            public char b;
            public int address;
        }
        public static List<quad> quadTable=new List<quad>();
        public static void emit(int label,string type,char a,char b,int address)
        {
            quad newquad = new quad();
            newquad.label = label;
            newquad.type = type;
            newquad.a = a;
            newquad.b = b;
            newquad.address = address;
            quadTable.Add(newquad);
        }

		public static int nxq;

        public static Stack<node> myStack=new Stack<node>();

		public static void BackPatch(pos a,int v)
		{
            foreach (quad qtemp in quadTable)
            {
                if (qtemp.label == a.value)
                    qtemp.address = v;
            }
            a.value = v;
			pos p = a;
			while (p.next != null) 
			{
				p = p.next;
                foreach (quad qtemp in quadTable)
                {
                    if (qtemp.label == p.value)
                        qtemp.address = v;
                }
				p.value = v;
			}
		}

        public static pos merge(pos a,pos b)
        {
            if (a.value == 0)
                return a;
            else
            {
                pos ptemp = b;
                while (ptemp.value != 0)
                    ptemp.value = ptemp.value;
                ptemp.value = a.value;
                return b;
            }
        }

		public static void display()
		{
            Stack<node> temp=new Stack<node>();
			node tmp;
            Console.WriteLine("\t\tThe present situation:");
            Console.WriteLine("\t\t{0}\n\t\tin stack", nxq);
            string str = string.Empty;
            while (myStack.Count > 0)
            {
                tmp = myStack.Peek();
                myStack.Pop();
                Console.WriteLine("\t\t\ttoken{0}", tmp.type);
                str += tmp.type;
                pos p = tmp.tc;
                Console.Write("\t\t\t\ttrue out");
                while (p != null)
                {
                    Console.Write("{0}--->", p.value);
                    p = p.next;
                }
                Console.WriteLine("");
                p = tmp.fc;
                Console.Write("\t\t\t\tfalse out");
                while (p != null)
                {
                    Console.Write("{0}--->", p.value);
                    p = p.next;
                }
                Console.WriteLine("");
                temp.Push(tmp);
            }
            Console.WriteLine("\t\t the present token string is {0}\n",str);
            while (temp.Count > 0)
            {
                tmp = temp.Peek();
                temp.Pop();
                myStack.Push(tmp);
            }
		}

        public static void work()
        {
            if (myStack.Count < 1)
                return;
            node tmp;
            tmp = myStack.Peek();
            myStack.Pop();
            if (VT.Contains(tmp.type))
            {
                if (myStack.Count!=0)
                {
                    node tmp2 = myStack.Peek();
                    if (tmp2.type == '>' || tmp2.type == '<' || tmp2.type == '=')
                    {
                        myStack.Pop();
                        node tmp3 = myStack.Peek();
                        myStack.Pop();
                        Console.WriteLine("process '{0}'",tmp2.type);
                        node putIn = new node();
                        putIn.tc = new pos();
                        putIn.fc = new pos();
                        putIn.type = 'E';
                        putIn.tc.value = nxq;
                        putIn.fc.value = nxq + 1;
                        myStack.Push(putIn);
                        Console.WriteLine("\t emit(j{0},{1},{2},0);", tmp2.type, tmp.type, tmp3.type);
                        emit(nxq, "j" + tmp2.type, tmp.type, tmp3.type, 0);
                        nxq++;
                        emit(nxq, "j", '_', '_', 0);
                        Console.WriteLine("\t emit(j,_,_,0);");
                        nxq++;
                        Console.WriteLine("process over!");
                    }
                    else
                    {
                        Console.WriteLine("process main");
                        node putIn = new node();
                        putIn.tc = new pos();
                        putIn.fc = new pos();
                        putIn.type = 'E';
                        putIn.tc.value = nxq;
                        putIn.fc.value = nxq + 1;
                        myStack.Push(putIn);
                        emit(nxq, "jnz", tmp.type, '_', 0);
                        Console.WriteLine("\t {0} emit(jnz,{1},_,0);", nxq, tmp.type);
                        nxq++;
                        emit(nxq, "j", '_', '_', 0);
                        Console.WriteLine("\t {0} emit(j,_,_,0);", nxq);
                        nxq++;
                        Console.WriteLine("process over!");
                    }
                }
                else
                {
                    Console.WriteLine("process main");
                    node putIn = new node();
                    putIn.tc = new pos();
                    putIn.fc = new pos();
                    putIn.type = 'E';
                    putIn.tc.value = nxq;
                    putIn.fc.value = nxq + 1;
                    myStack.Push(putIn);
                    emit(nxq, "jnz", tmp.type, '_', 0);
                    Console.WriteLine("\t {0} emit(jnz,{1},_,0);", nxq, tmp.type);
                    nxq++;
                    emit(nxq, "j", '_', '_', 0);
                    Console.WriteLine("\t {0} emit(j,_,_,0);", nxq);
                    nxq++;
                    Console.WriteLine("process over");
                }
            }
            else if (tmp.type == '!')
            {
                Console.WriteLine("process '┐'");
                node tmp2 = myStack.Peek();
                if (tmp2.type == 'E')
                {
                    node putIn = new node();
                    putIn.type = 'E';
                    putIn.tc = tmp2.fc;
                    putIn.fc = tmp2.tc;
                    myStack.Push(putIn);
                    Console.WriteLine("process over!");
                }
                else
                {
                    while (tmp2.type != 'E')
                    {
                        work();
                        tmp2 = myStack.Peek();
                    }
                    myStack.Push(tmp);
                    work();
                }
            }
            else if (tmp.type == '(')
            {
                node tmp2 = myStack.Peek();
                myStack.Pop();
                node tmp3 = myStack.Peek();
                while (!(tmp2.type == 'E' && tmp3.type == ')'))
                {
                    myStack.Push(tmp2);
                    work();
                    tmp2 = myStack.Peek();
                    myStack.Pop();
                    tmp3 = myStack.Peek();
                }
                myStack.Pop();
                Console.WriteLine("process '()'");
                node putIn=new node();
                putIn.type = 'E';
                putIn.tc = tmp2.tc;
                putIn.fc = tmp2.fc;
                myStack.Push(putIn);
                Console.WriteLine("process over!");
            }
            else if (tmp.type == 'E')
            {
                node tmp2 = myStack.Peek();
                if (tmp2.type == '&')
                {

                    Console.WriteLine("process '&'");
                    myStack.Pop();
                    node putIn=new node();
                    putIn.type = 'A';
                    BackPatch(tmp.tc, nxq);
                    putIn.tc = tmp.tc;
                    putIn.fc = tmp.fc;
                    myStack.Push(putIn);
                    Console.WriteLine("process over!");
                }
                else if (tmp2.type == '|')
                {
                    Console.WriteLine("process '|'");
                    myStack.Pop();
                    node putIn=new node();
                    putIn.type = 'B';
                    BackPatch(tmp.fc, nxq);
                    putIn.tc = tmp.tc;
                    putIn.fc = tmp.fc;
                    myStack.Push(putIn);
                    Console.WriteLine("process over!");
                }
            }
            else if (tmp.type == 'A')
            {
                node tmp2 = myStack.Peek();
                while (tmp2.type != 'E')
                {
                    work();
                    tmp2 = myStack.Peek();
                }
                Console.WriteLine("process E_a");
                myStack.Pop();
                node putIn=new node();
                putIn.type = 'E';
                putIn.tc = tmp2.tc;
                putIn.fc = tmp2.fc;
                pos p = putIn.fc;
                while (p.next != null)
                {
                    p = p.next;
                }
                p.next = tmp.fc;
                myStack.Push(putIn);
                Console.WriteLine("process over!");
            }
            else if (tmp.type == 'B')
            {
                node tmp2 = myStack.Peek();
                while (tmp2.type != 'E')
                {
                    work();
                    tmp2 = myStack.Peek();
                }
                Console.WriteLine("process E_b");
                myStack.Pop();
                node putIn=new node();
                putIn.type = 'E';
                putIn.fc = tmp2.fc;
                putIn.tc = tmp2.tc;
                pos p = putIn.tc;
                while (p.next != null)
                {
                    p = p.next;
                }
                p.next = tmp.tc;
                myStack.Push(putIn);
                Console.WriteLine("process over!");
            }            
        }



		public static void Main (string[] args)
		{
            initVT();
            nxq = 100;
            Console.WriteLine("Please input the expression waiting for analysis:");
            string strToDeal = Console.ReadLine();
            for (int i = strToDeal.Length - 1; i >= 0; i--)
            {
                node tmp=new node();
                tmp.fc = new pos();
                tmp.tc = new pos();
                tmp.type = strToDeal[i];
                myStack.Push(tmp);
            }
            while (myStack.Count > 1)
            {
                work();
                display();
            }
            node result;
            result = myStack.Peek();
            myStack.Pop();
            BackPatch(result.tc, nxq);
            BackPatch(result.fc, 0);
            myStack.Push(result);
            display();
            Console.WriteLine("The final quadrupes are:");
            foreach (quad qtemp in quadTable)
            {
                Console.WriteLine("{0}({1},{2},{3},{4})", qtemp.label, qtemp.type, qtemp.a, qtemp.b, qtemp.address);
            }
            Console.WriteLine("T:{0}", nxq);
            Console.WriteLine("F:0");
            return ;
		}
	}
}
