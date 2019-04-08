using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.GUIUtilities
{
	/// <summary>
	/// Non-generic base class for SettingsScriptableObject to make initialize a common base call.
	/// </summary>
	public abstract class SettingsScriptableObjectBase : ScriptableObject
	{
		public abstract void Initialize();
#if UNITY_EDITOR
		[HideInInspector]
		public virtual string SettingsName
		{
			get
			{
				return ObjectNames.NicifyVariableName(GetType().Name);
			}
		}
		public virtual string AssetPath { get { return @"Assets/emotitron/Resources/"; } }

		
#endif
	}

	/// <summary>
	/// Base class for all of the settings scriptable objects.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class SettingsScriptableObject<T> : SettingsScriptableObjectBase where T : SettingsScriptableObjectBase
	{

#if UNITY_EDITOR

		[HideInInspector]
		public abstract string HelpURL { get; }
		
#endif
		public static string AssetName = typeof(T).Name;

		//public static bool LoadResource()
		//{
		//	return (Resources.Load<T>(AssetName) as T) != null;
		//}

		public static System.Action OnSingletonReady;

		public static T single;
		/// <summary>
		/// Using Single rather than single fires off an check to make sure the singleton SO has been found and is mapped it 'single'.
		/// It also fires off an Initialize() to ensure everything is in order. Do not use Single in a hot path for this reason, but rather
		/// use the backing single field instead.
		/// </summary>
		public static T Single
		{
			get
			{
				if (!single)
				{
					/// TODO: Make this find all and enforce singleton
					single = Resources.Load<T>(AssetName) as T;

#if UNITY_EDITOR
					/// Can't find SO - need to make a replacement
					if (!single)
					{

						single = ScriptableObject.CreateInstance<T>();

						if (!System.IO.Directory.Exists(single.AssetPath))
						{
							System.IO.Directory.CreateDirectory(single.AssetPath);
							Debug.LogWarning(typeof(T).Name  + " resource directory missing. Recreation success: " + System.IO.Directory.Exists(single.AssetPath));
						}

						if (System.IO.Directory.Exists(single.AssetPath))
						{
							string filepath = single.AssetPath + AssetName + ".asset";

							if (!System.IO.File.Exists(filepath))
							{
								AssetDatabase.CreateAsset(single, filepath);
								Debug.LogWarning(AssetName + " missing. Recreation success: " + System.IO.File.Exists(filepath));
							}
							
						}
					}
#endif

					if (single)
						single.Initialize();
				}
				return single;
			}
		}

		protected virtual void Awake()
		{
			/// Only fire the OnSingleReady if we move from a null to found state - to prevent firing multiple times
			var holdsingle = single;

			Initialize();

			if (holdsingle == null && single != null)
				if (OnSingletonReady != null)
					OnSingletonReady();
		}

		protected virtual void OnEnable()
		{
			/// Only fire the OnSingleReady if we move from a null to found state - to prevent firing multiple times
			var holdsingle = single;

			Initialize();

			if (holdsingle == null && single != null)
				if (OnSingletonReady != null)
					OnSingletonReady();
		}

		public override void Initialize()
		{
			single = this as T;
		}


#if UNITY_EDITOR
		/// <summary>
		/// EditorGUILayout Serialize all visible properties in this Scriptable Object. Returns true if expanded;
		/// </summary>
		public virtual bool DrawGui(Object target, bool asFoldout, bool includeScriptField, bool initializeAsOpen = true, bool asWindow = false)
		{
			EditorGUI.indentLevel = 0;
			bool isExpanded = ScriptableObjectGUITools.DrawHeaderFoldout(target, this, asFoldout, SettingsName, HelpURL, initializeAsOpen, asWindow);

			if (asWindow)
				EditorGUI.indentLevel = 1;

			if (!asFoldout || isExpanded)
			{
				DrawGuiPre(asWindow);
				ScriptableObjectGUITools.RenderContentsOfScriptableObject(Single, includeScriptField);
				DrawGuiPost(asWindow);

			}
			else
				EditorGUILayout.Space();

			return isExpanded;
		}

		/// <summary>
		/// EditorGUI code to inject before rendering default contents of SO
		/// </summary>
		public virtual void DrawGuiPre(bool asWindow)
		{
		}
		/// <summary>
		/// EditorGUI code to inject after rendering default contents of SO
		/// </summary>
		public virtual void DrawGuiPost(bool asWindow)
		{
		}
#endif
	}


#if UNITY_EDITOR

	public class ScriptableObjectGUITools
	{

		public static Dictionary<FolderLookupKey, bool> foldoutStates = new Dictionary<FolderLookupKey, bool>();
		public static FolderLookupKey reusableLookup = new FolderLookupKey();
		public struct FolderLookupKey
		{
			public Object target;
			public System.Type type;
			public void Set(Object target, System.Type type)
			{
				this.target = target;
				this.type = type;
			}
		}

		public static bool DrawHeaderFoldout(Object target, Object targetSO, bool asFoldout, string settingsName, string HelpURL, bool initializeAsOpen, bool isWindow)
		{
			int holdIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			reusableLookup.Set(target, targetSO.GetType());

			/// Add this target to the foldout dictionary if it isn't there
			if (!foldoutStates.ContainsKey(reusableLookup))
			{
				foldoutStates.Add(reusableLookup, initializeAsOpen);
			}

			Rect rt = EditorGUILayout.GetControlRect();
			//EditorGUI.LabelField(rt, settingsName, (GUIStyle)"BoldLabel");

			//// Width is one pixel wider than the header, fixing this.
			//rt.width -= 1;

			float holdX = rt.x;
			//Adjust the find button to left align correctly based on foldout/non-foldout
			if (isWindow)
				rt.xMin += (asFoldout) ? 16 : 0;
			else
				rt.xMin += (asFoldout) ? 2 : -7;

			if (GUI.Button(rt, settingsName, (GUIStyle)"PreButton"))
			{
				EditorGUIUtility.PingObject(targetSO);

				if (HelpURL != null && HelpURL != "")
					Application.OpenURL(HelpURL);
			}

			//Adjust the xmin back
			rt.xMin = holdX;

			/// store the foldout state for this target object
			if (asFoldout)
				foldoutStates[reusableLookup] = EditorGUI.Foldout(rt, foldoutStates[reusableLookup], "");

			EditorGUI.indentLevel = holdIndentLevel;

			return foldoutStates[reusableLookup];
		}

		public static void RenderContentsOfScriptableObject(SettingsScriptableObjectBase singleton, bool includeScriptField)
		{
			EditorGUILayout.Space();

			SerializedObject so = new SerializedObject(singleton);
			SerializedProperty sp = so.GetIterator();
			sp.Next(true);

			// Skip drawing the script reference?
			if (!includeScriptField)
				sp.NextVisible(false);

			EditorGUI.BeginChangeCheck();
			while (sp.NextVisible(false))
			{
				Rect r = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(sp));
				EditorGUI.PropertyField(r, sp);
				//EditorGUILayout.PropertyField(sp);
			}

			so.ApplyModifiedProperties();

			EditorGUILayout.Space();

			if (EditorGUI.EndChangeCheck())
			{
				singleton.Initialize();
				AssetDatabase.SaveAssets();
			}
		}
	}

	#endif

}

