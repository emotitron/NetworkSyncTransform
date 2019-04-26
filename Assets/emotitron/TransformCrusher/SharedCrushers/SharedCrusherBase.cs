//Copyright 2018, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Diagnostics;
using emotitron.Utilities.FileIO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Compression
{

	public enum ShareByCommon
	{
		/// <summary>
		/// All instances of this component will use the same crusher for this field.
		/// </summary>
		ComponentAndFieldName,
		/// <summary>
		/// All instances of ANY component will use the same crusher for this field name.
		/// </summary>
		FieldName,

		/// <summary>
		/// All instances of a Prefab will use the same crusher for this field on this Component. NOTE: This is highly dependent on OnGUI code in the inspector, and will only work
		/// after the component has constructed and has been inspected (properties have been shown in the editor).
		/// </summary>
		Prefab
	}

	/// <summary>
	/// Base wrapper class for Crushers that allows sharing of a common crusher between instances, types and field names. To work correctly, be sure that this
	/// field is serialized, byt setting it to Public or using the [SerializeField] attribute.
	/// </summary>
	[System.Serializable]
	public abstract class SharedCrusherBase<T> : ISerializationCallbackReceiver where T : Crusher, new()
	{

		[SerializeField]
		public SharedCrusherDefine.ShareBy shareBy;

		[SerializeField]
		public int hashcode;

		public string fieldname;
		public string filename;
		public System.Type type;
		public int linenum = -1;
		public int instanceId;

		[SerializeField]
		protected T _crusher/* = new T()*/;

		/// <summary>
		/// This property can be used when initializing the crusher of a SharedCrusher. It will only overwrite the crusher values
		/// if the SharedCrusher has not yet been set up, allowing the first instance to set the initial values.
		/// </summary>
		public T Crusher
		{
			get
			{
				return _crusher;
			}
			set
			{

				/// Hashcode hasn't be established yet for this crusher, so can't tie it to the Shared SO
				if (hashcode == -1 || hashcode == 0)
				{

					if (_crusher == null)
						_crusher = value;

					return;
				}

				/// SO isn't ready yet, adding will have to be handled in the serialization loop
				if (SharedCrushersSO.single == null)
				{
					if (_crusher == null)
						_crusher = value;
					else
						(_crusher as ICrusherCopy<T>).CopyFrom(value);

					return;
				}

				var shared = SharedCrushersSO.single.GetCrusher<T>(hashcode);

				/// If no crusher ref in the SO exists yet for this hashcode, we will add it now
				if (shared == null)
				{
					if (_crusher == null)
						_crusher = value;
					else
						(_crusher as ICrusherCopy<T>).CopyFrom(value);

					shared = SharedCrushersSO.single.AddSharedCrusher(this);

					/// Since this isn't the default constructor, any value set here is user specified.
					shared.hasBeenSet = true;
					return;
				}

				/// An SO crusher exists for this hashcode - only overwrite it if it has not yet been set. We only allow it to be set once.
				if (shared.hasBeenSet)
				{
					_crusher = shared.CrusherRef as T;
					return;
				}

				/// make sure we are using the shared crusher before we write the new value, then AddSharedCrusher to make sure the SO has all the latest values
				_crusher = shared.CrusherRef as T;
				(_crusher as ICrusherCopy<T>).CopyFrom(value);
				SharedCrushersSO.single.AddSharedCrusher(this);
				shared.hasBeenSet = true;
			}
		}


		/// <summary>
		/// Default constructor determines the type of the object that called the constructer, and uses a common crusher for all instances of that Type
		/// </summary>
		public SharedCrusherBase()
		{
			shareBy = SharedCrusherDefine.ShareBy.Prefab;
			GetCallerTypeAndFieldName();
		}

		/// <summary>
		/// Constructor that uses a supplied hashcode for identifying a shared crusher. If a crusher with this hascode has been constructed,
		/// then the existing crusher will be used. If not a new one will be instantiated and used.
		/// </summary>
		/// <param name="hashcode"></param>
		public SharedCrusherBase(int hashcode)
		{
			shareBy = SharedCrusherDefine.ShareBy.SpecifiedInt;
			GetCallerTypeAndFieldName();
			this.hashcode = hashcode;
		}

		/// <summary>
		/// Constructor that uses a supplied name as a hashcode for identifying a shared crusher. If a crusher with this hascode has been constructed,
		/// then the existing crusher will be used. If not a new one will be instantiated and used.
		/// </summary>
		/// <param name="hashcode"></param>
		public SharedCrusherBase(string name)
		{
			shareBy = SharedCrusherDefine.ShareBy.SpecifiedString;
			GetCallerTypeAndFieldName();
			this.hashcode = name.GetHashCode();

		}

		public SharedCrusherBase(ShareByCommon sharedBy)
		{
			this.shareBy = (SharedCrusherDefine.ShareBy)sharedBy;
			GetCallerTypeAndFieldName();

			switch (this.shareBy)
			{
				case SharedCrusherDefine.ShareBy.ComponentAndFieldName:
					hashcode = (type.FullName + fieldname).GetHashCode();
					return;

				case SharedCrusherDefine.ShareBy.FieldName:
					hashcode = fieldname.GetHashCode();
					return;

				/// Prefabs can't immediately register with the SharedCrusherSO since they need to get their instanceId from the component,
				/// and StackTrace doesn't get us that. We get the instanceId from the Editor OnGui update.
				case SharedCrusherDefine.ShareBy.Prefab:
					return;

				default:
					UnityEngine.Debug.LogError("Invalid Enum value.");
					return;
			}
		}

		public static implicit operator T(SharedCrusherBase<T> crusher)
		{
			return crusher._crusher as T;
		}

		/// <summary>
		/// Attempts to collect fieldname, filename ,offset and Type that called the constructor, 
		/// and converts it into a hashcode for the dictionary.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		protected bool GetCallerTypeAndFieldName()
		{

			var st = new StackTrace(true);
			if (st != null)
			{
				int level = 3; // st.FrameCount - 1;
				StackFrame sf = st.GetFrame(level);
				if (sf == null)
				{
					/// No frame at level 3, so this stack trace is not originating from the field initialization and won't have the info we need.
					sf = st.GetFrame(st.FrameCount - 1);
					return false;
				}
				else
				{
					MethodBase mb = sf.GetMethod();

					var memberType = mb.MemberType;

					if (mb != null)
					{
						type = mb.DeclaringType;

						//if (memberType != MemberTypes.Constructor)
						if (type == typeof(SharedElementCrusher) || type == typeof(SharedFloatCrusher) || type == typeof(SharedTransformCrusher) || type == typeof(SharedRigidbodyCrusher))
						{
							UnityEngine.Debug.Log(type.FullName + " " + (type.FullName.GetHashCode() + sf.GetILOffset()));
							UnityEngine.Debug.LogError(
						"SharedCrusher classes need to include an initialization with <b>Shared" + typeof(T).Name + " sharedcrusher = new Shared" + typeof(T).Name + "()</b>. " +
						"The automatic determination of shared crushers uses a StackTrace on the constructor to identify field instances, " +
						"but this only works when a constructor is explicitly called.");
							return false;
						}

						else
						{
							filename = sf.GetFileName();
							linenum = sf.GetFileLineNumber();
							fieldname = filename.ReadLine(linenum).ExtractFieldName<T>();
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// User changes to crushers are serialized with the instance, even though they are part of a shared instance. We hold the values of the last inspected crusher
		/// with the CrusherHold reference, and these values are always used (OnAfterDeserialize) rather than the deserialized values.
		/// </summary>
		public void OnBeforeSerialize()
		{

#if UNITY_EDITOR

			if (SharedCrushersSO.single == null)
				return;

			/// WARNING: Don't try to use Sing here rather than single. Resource.Load during this timing leads to crashes.

			var sharedDefine = SharedCrushersSO.single.GetCrusher<T>(hashcode);

			/// SharedCrusher entry doesn't exist yet for this hashcode, try to add it
			if (sharedDefine == null || sharedDefine.CrusherRef == null && _crusher != null)
			{

				sharedDefine = SharedCrushersSO.single.AddSharedCrusher(this);

				return;
			}

			/// If the shared crusher ref and this objects crusher don't match - remedy that.
			if (!ReferenceEquals(sharedDefine.CrusherRef, _crusher))
			{
				if (sharedDefine.CrusherRef != null)
					_crusher = sharedDefine.CrusherRef as T;

				return;
			}

			if (sharedDefine.CrusherHold.GetHashCode() != _crusher.GetHashCode())
			{
				var holdcrusher = sharedDefine.CrusherHold as ICrusherCopy<T>;

				holdcrusher.CopyFrom(_crusher as T);
				EditorUtility.SetDirty(SharedCrushersSO.single);

				sharedDefine.hasBeenSet = true;
			}


#endif

		}

		/// <summary>
		/// We overwrite any deserialization to the crusher with the held settings. This brute force, but ensures out of sync instances get the most recent
		/// crusher settings applied by the user.
		/// </summary>
		public void OnAfterDeserialize()
		{
			SharedCrushersSO.OnSingletonReady -= OnSharedCrusherSOReady;
			SharedCrushersSO.OnSingletonReady += OnSharedCrusherSOReady;

			if (SharedCrushersSO.single == null)
				return;

			GetCrusherFromSharedSO();
		}

		private void OnSharedCrusherSOReady()
		{
			GetCrusherFromSharedSO();
		}

		private void GetCrusherFromSharedSO()
		{
			ElementCrusher pc = null;
			ElementCrusher rc = null;
			ElementCrusher sc = null;

			var sharedDefine = SharedCrushersSO.single.GetCrusher<T>(hashcode);
			if (sharedDefine == null)
			{
				return;
			}

			if (!ReferenceEquals(sharedDefine.CrusherRef, _crusher) && sharedDefine.CrusherRef != null)
			{

				if (sharedDefine.CrusherRef != null)
					_crusher = sharedDefine.CrusherRef as T;

				/// If the stored crusher uses WorldBounds, make sure we change the refs to that.
				var tc = _crusher as TransformCrusher;

				if (tc != null)
				{
					pc = tc.PosCrusher;
					rc = tc.RotCrusher;
					sc = tc.SclCrusher;

					if (pc != null)
					{
						pc.ApplyWorldCrusherSettings();
						pc.CacheValues();
					}
					if (rc != null)
					{
						rc.CacheValues();
					}
					if (sc != null)
					{
						sc.CacheValues();
					}
				}
				else
				{
					var ec = _crusher as ElementCrusher;
					if (ec != null)
					{
						if (ec.TRSType == TRSType.Position && ec.UseWorldBounds)
						{
							ec.ApplyWorldCrusherSettings();
						}
						ec.CacheValues();
					}
				}

				return;
			}

			if (sharedDefine.CrusherHold.GetHashCode() != _crusher.GetHashCode())
			{
				//#if !UNITY_EDITOR
				//					var tc = _crusher as TransformCrusher;
				//					UnityEngine.Debug.LogError("SC SetAlready PRE" +tc.PosCrusher.XCrusher.BitsDeterminedBy 
				//						+ " tally: " + tc.PosCrusher.TallyBits() + " cache: " + tc.PosCrusher.Cached_TotalBits);

				//					tc = sharedDefine.CrusherHold as TransformCrusher;
				//					UnityEngine.Debug.LogError("SC SetAlready PST" + tc.PosCrusher.XCrusher.BitsDeterminedBy
				//						+ " tally: " + tc.PosCrusher.TallyBits() + " cache: " + tc.PosCrusher.Cached_TotalBits);
				//#endif
				var holdcrusher = sharedDefine.CrusherHold as ICrusherCopy<T>;
				(_crusher as ICrusherCopy<T>).CopyFrom(holdcrusher as T);

			}
		}

	}

#if UNITY_EDITOR

	public class SharedCrusherBaseDrawer<T, U> : PropertyDrawer where T : class where U : class, new()
	{
		private static GUIContent sharedlabel = new GUIContent();
		private static SerializedProperty hash;
		private static SerializedProperty instanceId;
		private static SerializedProperty crusher;
		private static SerializedProperty shareBy;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{

			crusher = property.FindPropertyRelative("_crusher");
			hash = property.FindPropertyRelative("hashcode");
			shareBy = property.FindPropertyRelative("shareBy");
			instanceId = property.FindPropertyRelative("instanceId");

			string propname = property.name;

			int hashvalue = hash.intValue;
			string hashstring = hashvalue.ToString();

			sharedlabel.text =
				((hashvalue == -1) ? "    " : "") +
				((label == null) ? null : label.text + " [" + (System.Math.Abs(hashvalue) > 999 ? (".." + hashstring.Substring(hashstring.Length - 4)) : hashvalue.ToString()) + "]");

			sharedlabel.tooltip =
				(label == null) ? null :
				(label.tooltip + ((label.tooltip != null && label.tooltip != "") ? "\n" : "") + typeof(T).Name + " sharing based on common '" + (ShareByCommon)shareBy.intValue + "', using hashcode of " + hash.intValue);

			//float h = EditorGUI.GetPropertyHeight(crusher);
			Rect r = position;
			//r.height = h;

			EditorGUI.PropertyField(r, crusher, sharedlabel);

			var targetObject = property.serializedObject.targetObject;



#if UNITY_2018_3_OR_NEWER
			Object instance = PrefabUtility.GetCorrespondingObjectFromSource(property.serializedObject.targetObject);
#else
#pragma warning disable CS0618 // Type or member is obsolete
			Object instance = PrefabUtility.GetPrefabParent(property.serializedObject.targetObject);
#pragma warning restore CS0618 // Type or member is obsolete
#endif

			if (instance == null)
				instance = property.serializedObject.targetObject;

			if (hashvalue == 0)
			{
				if (shareBy.intValue == (int)ShareByCommon.Prefab)
				{
					int prefabid = instance.GetInstanceID();

					int propid = propname.GetHashCode();

					if (hashvalue != (prefabid + propid))
					{
						instanceId.intValue = prefabid;
						hash.intValue = (prefabid + propid);
						property.serializedObject.ApplyModifiedProperties();
					}
				}
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var sp = property.FindPropertyRelative("_crusher");
			return EditorGUI.GetPropertyHeight(sp);
		}
	}

#endif

}
