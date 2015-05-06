using System;

namespace DirectConstruction
{
	class MainClass
	{
		public static string[][] testcase = new string[][]{new string[]{" ","+","*","i","(",")","#"},
														   new string[]{"+",">","<","<","<",">",">"},
														   new string[]{"*",">",">","<","<",">",">"},
														   new string[]{"i",">",">"," "," ",">",">"},
														   new string[]{"(","<","<","<","<","="," "},
														   new string[]{")",">",">"," "," ",">",">"},
														   new string[]{"#","<","<","<","<"," ","="}};
		public static void Main (string[] args)
		{
			Console.WriteLine ("This is just a test case for direct construction!");
			DirectConstruction (testcase);
		}
		public static void DirectConstruction(string[][] input)
		{
			EndToken[] tokens = new EndToken[testcase [0].Length-1];
			for (int i = 1; i < testcase [0].Length; i++) {
				tokens [i - 1] = new EndToken (testcase [0] [i]);
			}
			int anyChange = 0;
			int round = 1;
			do 
			{
				Console.WriteLine("** Iteration {0} **",round++);
				outputResult(tokens);
				anyChange=0;
				for (int i = 1; i < testcase.Length; i++) 
				{
					for (int j = 1; j < testcase [i].Length; j++) 
					{
						if (testcase [i] [j] == ">") 
						{
							if (tokens [i - 1].functionF <= tokens [j - 1].functionG)
							{
								tokens [i - 1].functionF = tokens [j - 1].functionG + 1;
								anyChange++;
							}
						} else if (testcase [i] [j] == "<") 
						{
							if (tokens [i - 1].functionF >= tokens [j - 1].functionG)
							{
								tokens [j - 1].functionG = tokens [i - 1].functionF + 1;
								anyChange++;
							}
						} else if (testcase [i] [j] == "=") 
						{
							if (tokens [i - 1].functionF != tokens [j - 1].functionG) 
							{
								tokens [i - 1].functionF = Math.Max (tokens [i - 1].functionF, tokens [j - 1].functionG);
								tokens [j - 1].functionG = tokens [i - 1].functionF;
								anyChange++;
							}
						}
					}
				}
			} while(anyChange != 0);
		}

		public static void outputResult(EndToken[] tokens)
		{
			Console.WriteLine ("   | f | g ");
			for (int i = 0; i < tokens.Length-1; i++) 
			{
				Console.WriteLine (" {0} | {1} | {2} ", tokens [i].name, tokens [i].functionF, tokens [i].functionG);
			}
			Console.WriteLine ("\n");
		}
	}
	public class EndToken
	{
		public string name;
		public int functionF;
		public int functionG;
		public EndToken(string name)
		{
			this.name = name;
			this.functionF = 1;
			this.functionG = 1;
		}
	}
}
