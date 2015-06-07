
Main()            //主程序算法
{
	open("save.txt");      //打开输入文件
	open("output.txt");    //打开输出文件
	Print(G[S]);           //显示文法G[S]
	int check,over=0;
	int m,k;
	char chr;
	for(m=0;m<MAX;m++)
		for(k=0;k<buf;k++)
			t[m].name[k]='\0';    //初始化t[]
	initlab();                       //初始化LR(0)分析表
	get(sym);                        //取一个字符 
	while(sym!=结束)
	{
		chr=Getsymbol();             //词法分析
		if(chr为空格) continue;
		else 
		{
			S[num]=chr;              //保存词法分析结果
			num++;
		}
	}
	S[num++]='#';
	print("The while sentence is:");
	LR();                           //语法分析
	if(over==-1||Check!=1) 
	{
		print("Your input does not tally with the grammar!");
		close(文件);
		return -1;
	}
	if(Check==1) 
	{
		print("Parsing completed!");
		close(文件);
		return -1;
	}
	close(文件);
	return 0;
}

LR()              //语法分析算法
{
	i=0;
	int k=0;
	status.push(0);
	symbol.push('#');
    a=输入串队列队首元素;
    b=结束符号;
    c=符号栈栈顶元素;
	d=状态栈栈顶元素;
	s=开始符号;
	while(a!=b&&c!=s)
	{
		if(k==0) 
		{
			k=meet(d,a);
			continue;
		}
		if(k==1) 
		{
			k=meet(d,c);
			continue;
		}
		if(k==-1)
		{
			出错
			break;
		}
		if(k==2)
		{
			成功
			break;
		}
	}
	k=0;
	while(a==b)        //分析结束
	{
		if(c==s)
		{
			if(meet(d,a)==2)
			{
				ONE();                    //语法正确，输出四元式
				成功
				break;
			}
		}
		if(c!=s&&(meet(d,c)==1||meet(d,c))==0)
			continue;
		else 
			if(meet(d,c)==-1)              
			{
				出错
				break;
			}
	}
}

meet(int c,char s)              //进栈规约算法
{
	if(s==错误) return -1;
	int m,k=0;
	if(规约标志为1)
	{
		规约
		return 1;                         //规约成功
	}
	else
		进栈
} 

char  Getsymbol()             //词法分析算法
{
	i=0;
	while(sym!=结束)
	{
		if(sym==合法标识符的开头)
		{
			int h1=0,h2=0,h3=0;
			while(sym==字母/数字/下划线)
			{
				保存sym到token[]中;
				get(sym);                 //取下一字符
			}
			判断token[]中的字符串是否为关键字while/rop/op
			if(是while) 
				return 'w';              //返回while给主程序
			else 
				if(是rop) return 'r';    //返回rop给主程序
				else
					if(是op) return 'o'; //返回op给主程序
					else
						return 'i';
		}                                //已经取了下一个字符
		else
			if(sym==数字)
			{
				while(sym==数字)
				{
				    保存sym到token[]中;
				    get(sym);            //取下一字符
				}
				return 'i';			     //数字
			}
			else 
				if(sym=='(')
				{
 				    保存sym到token[]中;
					get(sym);            //取下一字符
					return '(';
				}					
				else
					if(sym==')')
					{
				        保存sym到token[]中;
						get(sym);            //取下一字符
					    return ')';
					}						
					else 
						if(sym=='=')
						{
							保存sym到token[]中;
							get(sym);
							return '=';
						}
						else
							if(sym==' ')
							{
								get(sym);
								return ' ';
							}
							else
							{
								get(sym);
								return 'X';  //错误
							}
	}
	return 0;
}