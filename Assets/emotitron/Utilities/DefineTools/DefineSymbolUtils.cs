//Copyright 2019, Davin Carten, All rights reserved

#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace emotitron.Utilities
{

	/// <summary>
	/// Methods for adding and removing symbols from Unity's Define Symbol section of PlayerSettings.
	/// </summary>
	public static class DefineSymbolUtils
	{

		/// <summary>
		/// Add a define symbol to the PlayerSettings
		/// </summary>
		public static void AddDefineSymbol(this string symbol)
		{
			var grp = EditorUserBuildSettings.selectedBuildTargetGroup;
			string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(grp);
			definesString += (definesString == "") ? symbol : (";" + symbol);

			PlayerSettings.SetScriptingDefineSymbolsForGroup(grp, definesString);

			Debug.Log("Added '" + symbol + "' to Define Symbols in PlayerSettings.\nNew Symbols string: <i>'" + definesString + "'</i>");

		}

		/// <summary>
		/// Remove a define symbol from the PlayerSettings
		/// </summary>
		/// <param name="symbol"></param>
		public static void RemoveDefineSymbol(this string symbol)
		{
			var grp = EditorUserBuildSettings.selectedBuildTargetGroup;
			string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(grp);

			List<int> positions = new List<int>();
			int ptr = 0;

			int safety = 0;
			int lastidx = definesString.LastIndexOf(symbol);

			/// Find all occurances of the symbol from last to first
			while (lastidx > -1)
			{
				positions.Add(lastidx);

				lastidx = definesString.Substring(0, lastidx).LastIndexOf(symbol);

				safety++;
				if (safety > 10)
				{
					Debug.LogError("Stuck " + ptr);
					break;
				}
			}

			/// Make sure the found occurances aren't substrings and actually terminate on both ends
			/// Remove if so.
			for (int i = 0; i < positions.Count; ++i)
			{

				int pos = positions[i];
				int len = symbol.Length;
				string search = symbol;

				// If symbol isn't the end of the string, it should have a ; after it.
				if (pos + symbol.Length < definesString.Length)
					search += ";";

				// If symbol isn't the start of the string, it should have a ; before it
				if (pos > 0)
				{
					search = ";" + search;
					pos--;
					len++;
				}

				// Remove the symbol if it terminates correctly on both ends
				if (definesString.Substring(pos).Contains(search))
					definesString = definesString.Remove(pos, len);

			}
			PlayerSettings.SetScriptingDefineSymbolsForGroup(grp, definesString);
			Debug.Log("Removed '" + symbol + "' from Define Symbols in PlayerSettings.\nNew Symbols string: <i>'" + definesString + "'</i>");

		}
	}
}

#endif
