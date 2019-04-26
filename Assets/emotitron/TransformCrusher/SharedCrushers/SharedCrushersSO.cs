using System.Collections;
using System.Collections.Generic;
using emotitron.Utilities.GUIUtilities;
using emotitron.Utilities.FileIO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Compression
{

	[System.Serializable]
	public abstract class SharedCrusherDefine
	{
		public enum ShareBy { ComponentAndFieldName, FieldName, Prefab, SpecifiedString, SpecifiedInt }

		public int hash;
		public ShareBy shareBy;
		public string fieldName;
		public string fileName;
		public int linenum;
		public int instanceId;
		public bool hasBeenSet;
		public abstract Crusher CrusherRef { set; get; }
		public abstract Crusher CrusherHold { set; get; }
	}

	[CreateAssetMenu()]
	public class SharedCrushersSO : SettingsScriptableObject<SharedCrushersSO>
	{
		/// <summary>
		/// These list pairs act as psuedo-Dictionaries. Unity can't serialize Dicts... so this hackiness is required.
		/// </summary>
		public List<int> sharedTransformCrusherHashes = new List<int>();
		public List<SharedCrusherDefineT> sharedTransformCrusherDef = new List<SharedCrusherDefineT>();

		public List<int> sharedRigidbodyCrusherHashes = new List<int>();
		public List<SharedCrusherDefineR> sharedRigidbodyCrusherDef = new List<SharedCrusherDefineR>();

		public List<int> sharedElementCrusherHashes = new List<int>();
		public List<SharedCrusherDefineE> sharedElementCrusherDef = new List<SharedCrusherDefineE>();

		public List<int> sharedFloatCrusherHashes = new List<int>();
		public List<SharedCrusherDefineF> sharedFloatCrusherDef = new List<SharedCrusherDefineF>();


		[System.Serializable]
		public class SharedCrusherDefineT : SharedCrusherDefine
		{
			public TransformCrusher crusherRef;
			public TransformCrusher crusherHold;
			public override Crusher CrusherRef { get { return crusherRef; } set { crusherRef = value as TransformCrusher; } }
			public override Crusher CrusherHold { get { return crusherHold; } set { crusherHold = value as TransformCrusher; } }
		}
		[System.Serializable]
		public class SharedCrusherDefineR : SharedCrusherDefine
		{
			public RigidbodyCrusher crusherRef;
			public RigidbodyCrusher crusherHold;
			public override Crusher CrusherRef { get { return crusherRef; } set { crusherRef = value as RigidbodyCrusher; } }
			public override Crusher CrusherHold { get { return crusherHold; } set { crusherHold = value as RigidbodyCrusher; } }
		}
		[System.Serializable]
		public class SharedCrusherDefineE : SharedCrusherDefine
		{
			public ElementCrusher crusherRef;
			public ElementCrusher crusherHold;
			public override Crusher CrusherRef { get { return crusherRef; } set { crusherRef = value as ElementCrusher; } }
			public override Crusher CrusherHold { get { return crusherHold; } set { crusherHold = value as ElementCrusher; } }
		}
		[System.Serializable]
		public class SharedCrusherDefineF : SharedCrusherDefine
		{
			public FloatCrusher crusherRef;
			public FloatCrusher crusherHold;
			public override Crusher CrusherRef { get { return crusherRef; } set { crusherRef = value as FloatCrusher; } }
			public override Crusher CrusherHold { get { return crusherHold; } set { crusherHold = value as FloatCrusher; } }
		}


#if UNITY_EDITOR

		public override string AssetPath { get { return @"Assets/emotitron/TransformCrusher/SharedCrushers/Resources/"; } }

		public override string HelpURL
		{
			get
			{
				return "";
			}
		}

		//[InitializeOnLoadMethod]
		//static void EditorBootstrap()
		//{
		//	var single = Single;
		//}

		protected override void Awake()
		{
			base.Awake();
			single.PurgeUnused();
		}

#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Bootstrap()
		{
			var single = Single;

		}


		public SharedCrusherDefine AddSharedCrusher<T>(SharedCrusherBase<T> instance) where T: Crusher, new()
		{
			/// This shared crusher has not yet come up with its hashcode, don't add to collection
			if (instance.hashcode == 0 || instance.hashcode == -1)
				return null;
			
			/// Get our appropriate Lists (TransformCrusher, ElementCrusher, or FloatCrusher)
			SharedCrusherDefine def;

			List<int> hashlist =
				(typeof(T) == typeof(TransformCrusher)) ? sharedTransformCrusherHashes :
				(typeof(T) == typeof(RigidbodyCrusher)) ? sharedRigidbodyCrusherHashes :
				(typeof(T) == typeof(ElementCrusher)) ? sharedElementCrusherHashes :
				sharedFloatCrusherHashes;

			int index = hashlist.IndexOf(instance.hashcode);
			bool exists = index > -1;

			if (typeof(T) == typeof(TransformCrusher))
				def = (exists) ? sharedTransformCrusherDef[index] : new SharedCrusherDefineT();
			else if (typeof(T) == typeof(RigidbodyCrusher))
				def = (exists) ? sharedRigidbodyCrusherDef[index] : new SharedCrusherDefineR();
			else if (typeof(T) == typeof(ElementCrusher))
				def = (exists) ? sharedElementCrusherDef[index] : new SharedCrusherDefineE();
			else
				def = (exists) ? sharedFloatCrusherDef[index] : new SharedCrusherDefineF();

			def.hash = instance.hashcode;
			def.shareBy = instance.shareBy;
			def.fileName = instance.filename;
			def.linenum = instance.linenum;
			def.fieldName = instance.fieldname;
			def.instanceId = instance.instanceId;

			/// If there is no defined crusherRef (this was not completely initialized) create it now using the crusher values of the instance
			if (def.CrusherRef == null)
			{
				if (ReferenceEquals(instance.Crusher, null))
					instance.Crusher = new T();

				if (ReferenceEquals(def.CrusherHold, null))
					def.CrusherHold = new T();

				def.CrusherRef = instance.Crusher;

				(def.CrusherHold as ICrusherCopy<T>).CopyFrom(def.CrusherRef as T);

			}
			else
			{
				if (def.CrusherRef == null)
				{
					Debug.LogError("No crusher ref yet?");
					return def;
				}

				instance.Crusher = def.CrusherRef as T;
				(instance.Crusher as ICrusherCopy<T>).CopyFrom(def.CrusherHold as T);
			}

			if (!exists)
			{
				hashlist.Add(instance.hashcode);

				if (typeof(T) == typeof(TransformCrusher))
					sharedTransformCrusherDef.Add(def as SharedCrusherDefineT);
				else if (typeof(T) == typeof(RigidbodyCrusher))
					sharedRigidbodyCrusherDef.Add(def as SharedCrusherDefineR);
				else if (typeof(T) == typeof(ElementCrusher))
					sharedElementCrusherDef.Add(def as SharedCrusherDefineE);
				else
					sharedFloatCrusherDef.Add(def as SharedCrusherDefineF);
			}
			return def;
		}

		public SharedCrusherDefine GetCrusher<T>(int hashcode) where T: class, new()
		{
			List<int> hashlist =
				(typeof(T) == typeof(TransformCrusher)) ? sharedTransformCrusherHashes :
				(typeof(T) == typeof(RigidbodyCrusher)) ? sharedRigidbodyCrusherHashes :
				(typeof(T) == typeof(ElementCrusher)) ? sharedElementCrusherHashes :
				sharedFloatCrusherHashes;

			int index = hashlist.IndexOf(hashcode);

			if (index == -1)
				return null;

			if (typeof(T) == typeof(TransformCrusher))
				return sharedTransformCrusherDef[index];

			if (typeof(T) == typeof(RigidbodyCrusher))
				return sharedRigidbodyCrusherDef[index];

			if (typeof(T) == typeof(ElementCrusher))
				return sharedElementCrusherDef[index];

			return sharedFloatCrusherDef[index];
		}

#if UNITY_EDITOR

		public void PurgeUnused()
		{
			/// TransformCrusher collection
			for (int i = sharedTransformCrusherHashes.Count - 1; i > -1; --i)
			{
				var v = sharedTransformCrusherDef[i];
				if (!v.fieldName.DoesFieldExistInLine<TransformCrusher>(v.fileName, v.linenum))
				{
					sharedTransformCrusherHashes.RemoveAt(i);
					sharedTransformCrusherDef.RemoveAt(i);
					continue;
				}
				if (v.shareBy == SharedCrusherDefine.ShareBy.Prefab)
				{
					if (EditorUtility.InstanceIDToObject(v.instanceId) == null)
					{
						sharedTransformCrusherHashes.RemoveAt(i);
						sharedTransformCrusherDef.RemoveAt(i);
						continue;
					}
				}
			}

			/// RigidbodyCrusher collection
			for (int i = sharedRigidbodyCrusherHashes.Count - 1; i > -1; --i)
			{
				var v = sharedRigidbodyCrusherDef[i];
				if (!v.fieldName.DoesFieldExistInLine<RigidbodyCrusher>(v.fileName, v.linenum))
				{
					sharedRigidbodyCrusherHashes.RemoveAt(i);
					sharedRigidbodyCrusherDef.RemoveAt(i);
					continue;
				}
				if (v.shareBy == SharedCrusherDefine.ShareBy.Prefab)
				{
					if (EditorUtility.InstanceIDToObject(v.instanceId) == null)
					{
						sharedRigidbodyCrusherHashes.RemoveAt(i);
						sharedRigidbodyCrusherDef.RemoveAt(i);
						continue;
					}
				}
			}


			/// ElementCrusher collection
			for (int i = sharedElementCrusherHashes.Count - 1; i > -1; --i)
			{
				var v = sharedElementCrusherDef[i];
				if (!v.fieldName.DoesFieldExistInLine<ElementCrusher>(v.fileName, v.linenum))
				{
					sharedElementCrusherHashes.RemoveAt(i);
					sharedElementCrusherDef.RemoveAt(i);
					continue;
				}
				if (v.shareBy == SharedCrusherDefine.ShareBy.Prefab)
				{
					if (EditorUtility.InstanceIDToObject(v.instanceId) == null)
					{
						sharedElementCrusherHashes.RemoveAt(i);
						sharedElementCrusherDef.RemoveAt(i);
						continue;
					}
				}
			}

			/// FloatCrusher collection
			for (int i = sharedFloatCrusherHashes.Count - 1; i > -1; i--)
			{
				var v = sharedFloatCrusherDef[i];
				if (!v.fieldName.DoesFieldExistInLine<FloatCrusher>(v.fileName, v.linenum))
				{
					sharedFloatCrusherHashes.RemoveAt(i);
					sharedFloatCrusherDef.RemoveAt(i);
					continue;
				}
				if (v.shareBy == SharedCrusherDefine.ShareBy.Prefab)
				{
					if (EditorUtility.InstanceIDToObject(v.instanceId) == null)
					{
						sharedFloatCrusherHashes.RemoveAt(i);
						sharedFloatCrusherDef.RemoveAt(i);
						continue;
					}
				}
			}
			EditorUtility.SetDirty(single);
		}

		public static void Clear()
		{
			Single.sharedTransformCrusherHashes.Clear();
			single.sharedTransformCrusherDef.Clear();

			Single.sharedRigidbodyCrusherHashes.Clear();
			single.sharedRigidbodyCrusherDef.Clear();

			single.sharedElementCrusherHashes.Clear();
			single.sharedElementCrusherDef.Clear();

			single.sharedFloatCrusherHashes.Clear();
			single.sharedFloatCrusherDef.Clear();

			EditorUtility.SetDirty(single);
		}
#endif
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SharedCrushersSO))]
	[CanEditMultipleObjects]
	public class LookAtPointEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			Rect r = EditorGUILayout.GetControlRect();
			if (GUI.Button(r, "Clear All"))
				SharedCrushersSO.Clear();

			r = EditorGUILayout.GetControlRect();
			if (GUI.Button(r, "Purge Unused"))
				SharedCrushersSO.Single.PurgeUnused();

			base.OnInspectorGUI();

		}
	}

#endif

}

