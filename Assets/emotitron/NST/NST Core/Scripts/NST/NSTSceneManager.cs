using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using emotitron.Debugging;
using emotitron.Compression;

namespace emotitron.NST
{
	public class NSTSceneManager
	{
		private static int _currentSceneIndex;
		public static readonly bool includeSceneIndexInUpdates;
		public static readonly int numOfScenesInBuild;
		public static readonly int bitsForSceneIndex;


		public static int CurrentSceneIndex
		{
			get { return _currentSceneIndex; }
		}

		static NSTSceneManager()
		{
			includeSceneIndexInUpdates = HeaderSettings.Single.includeSceneIndex;
			numOfScenesInBuild = SceneManager.sceneCountInBuildSettings;
			bitsForSceneIndex = emotitron.Compression.FloatCrusher.GetBitsForMaxValue((uint)Mathf.Max(0, numOfScenesInBuild));
			SceneManager.sceneLoaded += OnSceneLoaded;
			XDebug.Log(numOfScenesInBuild + " total scenes in build, networking scene index will add " + bitsForSceneIndex + " to each update ");
		}

		public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			XDebug.Log("<b>Loaded Scene </b>" + scene + " with mode " + mode);
			PollCurrentSceneIndex();
		}


		[RuntimeInitializeOnLoadMethod]
		public static int PollCurrentSceneIndex()
		{

			_currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

			/// Convert -1 value into max indexed value (can't network negative numbers)
			if (_currentSceneIndex == -1)
				_currentSceneIndex = numOfScenesInBuild;

			XDebug.Log("<b>New scene index</b> : " + _currentSceneIndex);
			return _currentSceneIndex;
		}

		/// <summary>
		/// Write the current scene ID into the bitstream.
		/// </summary>
		/// <param name="bitstream"></param>
		public static void Serialize(ref UdpBitStream bitstream)
		{
			if (includeSceneIndexInUpdates)
				bitstream.WriteInt(_currentSceneIndex, bitsForSceneIndex);
		}

		/// <summary>
		/// Read the current scene ID from the bitstream. Return -1 for Scene not being in the build list, or if includeSceneIndexInUpdate is disabled.
		/// </summary>
		/// <param name="bitstream"></param>
		/// <param name="outstream"></param>
		/// <param name="asServer"></param>
		/// <returns></returns>
		public static int Deserialize(ref UdpBitStream bitstream, ref UdpBitStream outstream, bool asServer)
		{
			if (includeSceneIndexInUpdates)
			{
				int buildindex = bitstream.ReadInt(bitsForSceneIndex);
				if (asServer)
					outstream.Write(buildindex, bitsForSceneIndex);
				return buildindex;
			}
			else
				return -1;
		}
	}
}

