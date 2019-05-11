
using System.IO;

namespace emotitron.Utilities.FileIO
{
	public static class ScriptIO
	{
		/// <summary>
		/// Read a line from a text file and return as a string.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="linenumber"></param>
		/// <returns></returns>
		public static string ReadLine(this string filename, int linenumber)
		{
			if (filename == null || filename == "")
				return null;

			if (!File.Exists(filename))
				return null;

			FileInfo fi = new FileInfo(filename);

			try
			{
				StreamReader reader = fi.OpenText();
				string line = null;
				for (int i = 0; i < linenumber; ++i)
					line = reader.ReadLine();

				reader.Close();
				return line;
			}
			catch (IOException IOEx)
			{
				Debugging.XDebug.Log(!Debugging.XDebug.logInfo ? null : ("CS File locked at the moment." + IOEx));
				return null;
			}
		}

		/// <summary>
		/// Extracts a field name of type T from the given cs line.
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		public static string ExtractFieldName<T>(this string line)
		{
			if (line == null)
				return null;

			string typename = typeof(T).Name;
			int start = line.IndexOf(typename) + typename.Length;

			if (start >= line.Length)
				return null;

			string frag = line.Substring(start).TrimStart();
			int len = frag.IndexOf(" ");

			if (len > 0)
				return frag.Substring(0, len);

			return null;

		}

		public static bool DoesFieldExistInLine<T>(this string fieldname, string filepath, int linenumber)
		{
			string line = filepath.ReadLine(linenumber);

			if (line == null)
				return false;

			string foundname = line.ExtractFieldName<T>();
			return (foundname == fieldname);
		}
	}
}

