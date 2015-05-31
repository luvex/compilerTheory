using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace FirstFollow
{
	class FirstFollow
	{
		public static ArrayList grammer = new ArrayList ();
		public static Dictionary<string,ArrayList> first = new Dictionary<string, ArrayList>();
		public static Dictionary<string,ArrayList> follow = new Dictionary<string, ArrayList>();
		public static DataTable predictTable = new DataTable ();
		public static Stack<string> tokens = new Stack<string> ();
		public static Stack<string> intokens = new Stack<string> ();

		public static void Main (string[] args)
		{
			Console.WriteLine ("If you want to input the grammer in the console, please enter 'i'.");
			Console.WriteLine ("If you want to see the test case, please enter 't'.");
			processInput ();                          //store the grammer into the ArrayList grammer.
			//foreach (string[] temp in grammer)
			//	Console.WriteLine ("{0},{1}",temp[0],temp[1]);

			First ();
			Follow ();
			constructAnalyst ();
			test ();
		}
		public static void processInput()
		{
			string judge = Console.ReadLine ();
			if (judge.Equals ("t")) 
			{
				Console.WriteLine ("The grammer of the test case is:");
				Console.WriteLine ("E->TE'");
				Console.WriteLine ("E'->+E|ε");
				Console.WriteLine ("T->FT'");
				Console.WriteLine ("T'->T|ε");
				Console.WriteLine ("F->PF'");
				Console.WriteLine ("F'->*F'|ε");
				Console.WriteLine ("P->(E)|a|b|^");
				Console.WriteLine ("end");
				string[] s1 = new string[2];s1 [0] = "E";s1 [1] = "TE'";grammer.Add (s1);
				string[] s2 = new string[2];s2 [0] = "E'";s2 [1] = "+E";grammer.Add (s2);
				string[] s3 = new string[2];s3 [0] = "E'";s3 [1] = "ε";grammer.Add (s3);
				string[] s4 = new string[2];s4 [0] = "T";s4 [1] = "FT'";grammer.Add (s4);
				string[] s5 = new string[2];s5 [0] = "T'";s5 [1] = "T";grammer.Add (s5);
				string[] s6 = new string[2];s6 [0] = "T'";s6 [1] = "ε";grammer.Add (s6);
				string[] s7 = new string[2];s7 [0] = "F";s7 [1] = "PF'";grammer.Add (s7);
				string[] s8 = new string[2];s8 [0] = "F'";s8 [1] = "*F'";grammer.Add (s8);
				string[] s9 = new string[2];s9 [0] = "F'";s9 [1] = "ε";grammer.Add (s9);
				string[] s10 = new string[2];s10 [0] = "P";s10 [1] = "(E)";grammer.Add (s10);
				string[] s11 = new string[2];s11 [0] = "P";s11 [1] = "a";grammer.Add (s11);
				string[] s12 = new string[2];s12 [0] = "P";s12 [1] = "b";grammer.Add (s12);
				string[] s13 = new string[2];s13 [0] = "P";s13 [1] = "^";grammer.Add (s13);
				follow.Add (s1 [0], new ArrayList ());
				follow.Add (s2 [0], new ArrayList ());
				follow.Add (s4 [0], new ArrayList ());
				follow.Add (s5 [0], new ArrayList ());
				follow.Add (s7 [0], new ArrayList ());
				follow.Add (s8 [0], new ArrayList ());
				follow.Add (s10 [0], new ArrayList ());
			} 
			else if (judge.Equals ("i")) 
			{
				string temp;
				while (!(temp = Console.ReadLine ()).Equals ("end")) 
				{
					temp = System.Text.RegularExpressions.Regex.Replace (temp, " ", "");//clean up the blank space token
					string[] lr = temp.Split (new string[]{"->"},StringSplitOptions.None);
					if (lr.Length == 1)
						lr = temp.Split (new string[]{"=>"},StringSplitOptions.None);
					if (lr.Length == 1)
								lr = temp.Split (new string[]{"→"},StringSplitOptions.None);
					if (lr.Length == 1) 
					{
						Console.WriteLine ("The grammer is not in regular format!");
						System.Environment.Exit (0);
					}
					follow.Add (lr [0], new ArrayList ());
					string[] r = lr [1].Split (new string[]{"|"},StringSplitOptions.None);
					foreach (string rtemp in r) 
					{
						string[] regular = new string[2];
						regular [0] = lr [0];
						regular [1] = rtemp;
						grammer.Add (regular);
					}
				}

			} 
			else 
			{
				Console.WriteLine ("The choice is not right, please choose again!");
				processInput ();
			}
		}
		public static void First()
		{
			foreach (string[] temp in grammer) {
				ArrayList result = getFirst (temp [0]);
				addFirst (first, temp [0], result);
			}
			Console.WriteLine ("\nThe first set is below:");
			foreach (KeyValuePair<string, ArrayList> temp in first) 
			{
				string result = "";
				foreach (string s in temp.Value)
					result = result + s + ",";
				Console.WriteLine ("First{{{0}}}={{{1}}}",temp.Key,result.Substring(0,result.Length-1));
			}
		}
		public static ArrayList getFirst(string s)
		{
			ArrayList result = new ArrayList ();
			if (isEndToekn (s[0])) 
			{
				result.Add (s[0].ToString());//X is a endToken
			} 
			else 
			{
				foreach (string[] temp in grammer) 
				{
					if (s.Equals (temp [0])/*||s[0].ToString().Equals(temp[0])*/) 
					{
						foreach (char ctemp in temp[1]) 
						{
							ArrayList result1 = getFirst (ctemp.ToString ());
							foreach (string stemp in result1) 
							{
								if (!result.Contains (stemp))
									result.Add (stemp);
							}
							if (!result1.Contains ("ε"))
								break;
						}
					}
				}
			}
			if (s.Equals ("ε")) 
			{
				result.Add (s);
			}
			return result;
		}
		public static Boolean isEndToekn(char c)
		{
			if(char.IsUpper(c))
				return false;
			else
				return true;
		}
		public static void addFirst(Dictionary<string,ArrayList> first,string key,ArrayList value)
		{
			if (first.ContainsKey (key)) {
				foreach (string s in value) {
					if (!first [key].Contains (s))
						first [key].Add (s);
				}
			} else
				first.Add (key, value);
		}

		public static void Follow()
		{
			//bool startToken = true;
			foreach (string [] s in grammer) 
			{
				//if (startToken) 
				//{
					follow [s [0]].Add ("$");
				break;
				//	startToken = false;
				//}
			}
			//while (true) 
			//{
			getFollow ();
			getFollow ();
			//rule2 ();
			//}
			Console.WriteLine ("\nThe follow set is below:");
			foreach (KeyValuePair<string, ArrayList> temp in follow) 
			{
				string result = "";
				foreach (string s in temp.Value)
					result = result + s + ",";
				Console.WriteLine ("Follow{{{0}}}={{{1}}}",temp.Key,result.Substring(0,result.Length-1));
			}

		}
		public static void getFollow()
		{
			foreach (string[] s in grammer) 
			{
				Dictionary<int,string> dtemp = new Dictionary<int,string> ();
				foreach (string[] temp in grammer) {
					if (s [1].IndexOf (temp [0])!=-1) {
						if (dtemp.ContainsKey (s [1].IndexOf (temp [0]))) {
							if (temp [0].Length > dtemp [s [1].IndexOf (temp [0])].Length)
								dtemp [s [1].IndexOf (temp [0])] = temp [0];
						}
						else
							dtemp.Add (s [1].IndexOf (temp [0]), temp [0]);
					}
				}
				var list = dtemp.Keys.ToList ();
				list.Sort ();
				/*if (list.ToArray ().Length >= 2) 
				{
					int a = (int)list.ToArray ().GetValue (list.ToArray ().Length - 1);
					int b = (int)list.ToArray ().GetValue (list.ToArray ().Length - 2);
					if (a - b == dtemp [b].Length) 
					{
						ArrayList al = getFirst (dtemp [a]);
						string goal = dtemp [b];
						foreach (string gtemp in al) 
						{
							if (gtemp != "ε") {
								if (!follow [goal].Contains (gtemp))
									follow [goal].Add (gtemp);
							}
						}
					}
				}*/

				if(list.ToArray ().Length>0)
				{
					int[] notEndTokenIndex = new int[list.ToArray ().Length];
					for (int i = 0; i < notEndTokenIndex.Length; i++)
						notEndTokenIndex [i] = (int)list.ToArray ().GetValue (list.ToArray ().Length - 1 - i);
					//rule 2
					foreach (int itemp in notEndTokenIndex) {
						string waitingForGetFirst = s [1].Substring (itemp + dtemp [itemp].Length);
						if (waitingForGetFirst != "") {
							ArrayList al = getFirst (waitingForGetFirst);
							string goal = dtemp [itemp];
							foreach (string gtemp in al) {
								if (gtemp != "ε") {
									if (!follow [goal].Contains (gtemp))
										follow [goal].Add (gtemp);
								}
							}
						}
					}

					//rule 3
					if (s [1].Length - notEndTokenIndex [0] == dtemp [notEndTokenIndex [0]].Length) 
					{
						foreach (string ftemp in follow[s[0]]) 
						{
							if (!follow [dtemp [notEndTokenIndex [0]]].Contains (ftemp))
								follow [dtemp [notEndTokenIndex [0]]].Add (ftemp);
						}
						for (int i = 1; i < notEndTokenIndex.Length; i++) {
							if (notEndTokenIndex [i-1] - notEndTokenIndex [i] == dtemp [notEndTokenIndex [i]].Length) {
								if (getFirst (dtemp [notEndTokenIndex [i-1]]).Contains ("ε")) 
								{
									foreach (string ftemp in follow[s[0]]) 
									{
										if (!follow [dtemp [notEndTokenIndex [i]]].Contains (ftemp))
											follow [dtemp [notEndTokenIndex [i]]].Add (ftemp);
									}
								}
							}
						}
					}
				}
			}
		}
		public static void constructAnalyst()
		{
			DataColumn[] dctemp = new DataColumn[1];
			dctemp [0] = new DataColumn ();
			dctemp[0].ColumnName = "NET";
			predictTable.Columns.Add (dctemp[0]);
			predictTable.PrimaryKey = dctemp;
			foreach (KeyValuePair<string,ArrayList> temp in first) 
			{
				foreach (string s in temp.Value) 
				{
					if (s != "ε") 
					{
						if (!predictTable.Columns.Contains (s))
							predictTable.Columns.Add (s);
					}
				}
			}
			foreach (KeyValuePair<string,ArrayList> temp in follow) 
			{
				foreach (string s in temp.Value) 
				{
					if (!predictTable.Columns.Contains (s))
						predictTable.Columns.Add (s);
				}
			}
			foreach (string[] temp in grammer) 
			{
				if (!predictTable.Rows.Contains (temp [0])) {
					DataRow dr = predictTable.NewRow ();
					dr ["NET"] = temp [0];
					string runtemp = temp [1];
					if (temp [1].Length > 1) 
					{
						if (!isEndToekn (temp [1] [0])) 
						{
							if (temp [1] [1] == '\'') 
							{
								runtemp = temp [1] [0].ToString () + "'";
							} else
								runtemp = temp [1] [0].ToString ();
						}
					}
					ArrayList result = getFirst (runtemp);
					foreach (string s in result) 
					{
						//Console.WriteLine (s);
						if (s != "ε")
							dr [s] = temp [0] + "->" + temp [1];
						else 
						{
							ArrayList followset = follow [temp[0]];
							foreach (string sss in followset) 
							{
								dr [sss] = temp [0] + "->" + temp [1];
							}
						}
					}
					predictTable.Rows.Add (dr);
				} 
				else 
				{
					DataRow dr = predictTable.Rows.Find (temp [0]);
					ArrayList result = getFirst (temp [1]);
					foreach (string s in result) 
					{
						if (s != "ε")
							dr [s] = temp [0] + "->" + temp [1];
						else 
						{
							ArrayList followset = follow [temp[0]];
							foreach (string sss in followset) 
							{
								dr [sss] = temp [0] + "->" + temp [1];
							}
						}
					}
				}
			}

			Console.WriteLine ("\nThe predict analyst table is:\n");
			string firstRow = "";
			foreach (DataColumn dc in predictTable.Columns)
				firstRow = firstRow + dc.ToString () + "\t";
			Console.WriteLine ("{0}", firstRow);
			foreach (DataRow dr in predictTable.Rows) 
			{
				string rowtemp = "";
				foreach (DataColumn dc in predictTable.Columns)
					rowtemp = rowtemp + dr [dc.ToString ()].ToString () + "\t";
				Console.WriteLine (rowtemp);
			}
		}
		public static void test()
		{
			//Stack<string> tokens = new Stack<string> ();
			tokens.Push ("$");
			foreach (string[] temp in grammer) 
			{
				tokens.Push (temp [0]);
				break;
			}
			//Stack<string> intokens = new Stack<string> ();
			intokens.Push ("$");
			Console.Write ("\nPlease input a phrase.\n");
			string input = Console.ReadLine ();
			for(int i=input.Length-1;i>=0;i--)
			{
				intokens.Push (input[i].ToString());
			}
			//while(intokens.Count>0)
			//	Console.WriteLine (intokens.Pop());
			//while (tokens.Count > 0)
			//	Console.WriteLine (tokens.Pop ());
			int step=0;
			string rule = "";
			while (intokens.Count > 0 && tokens.Count > 0) 
			{
				step++;
				outputstack (step,rule);
				string tx = tokens.Pop ();
				string ta = intokens.Pop ();
				tokens.Push (tx);intokens.Push (ta);
				rule = process (tx, ta);
			}
		}
		public static void outputstack(int step,string rule)
		{
			string tok = "";
			string itok = "";
			foreach (string temp in tokens)
				tok += temp;
			foreach (string temp in intokens)
				itok += temp;
			Console.WriteLine ("{0}\t{1}\t\t{2}\t{3}\n", step, tok, itok,rule);
		}
		public static string process(string x,string a)
		{
			if (isEndToekn (x[0])) 
			{
				if (!x.Equals (a))
					Console.WriteLine ("Unsuccessful match!");
				else 
				{
					if (x != "$") 
					{
						string d = tokens.Pop ();
						intokens.Pop ();
						return "-" + d + "- has been deleted!";
					} 
					else 
					{
						Console.WriteLine ("Successful Match!");
						tokens.Pop ();
						intokens.Pop ();
					}
				}
				return "";
			} 
			else 
			{
				DataRow dr = predictTable.Rows.Find (x);
				string s = dr [a].ToString();
				if (s == "")
					Console.WriteLine ("There is some errors in predict analyst!");
				else 
				{
					tokens.Pop ();
					string[] ws = s.Split (new string[]{ "->" }, StringSplitOptions.None);
					for (int i = ws [1].Length - 1; i >= 0; i--) 
					{
						if (ws [1] [i] == '\'') {
							tokens.Push (ws [1] [i - 1].ToString () + ws [1] [i].ToString ());
							i--;
						} 
						else 
						{
							if(!ws[1][i].ToString().Equals("ε"))
								tokens.Push (ws [1] [i].ToString ());
						}
					}
				}
				return s;
			}
		}
	}

}
