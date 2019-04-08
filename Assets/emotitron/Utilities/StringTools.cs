using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class StringTools
{
	public static string RemoveAllNonAlphaNumeric(string instring)
	{
		Regex rgx = new Regex("[^a-zA-Z0-9 -]");
		return rgx.Replace(instring, "");
	}
}
