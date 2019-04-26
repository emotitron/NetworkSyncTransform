//Copyright 2019, Davin Carten, All rights reserved

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Compression
{
	/// <summary>
	/// Wrapper class for RigidbodyCrusher that allows sharing of a common crusher between instances, types and field names. To work correctly, be sure that this
	/// field is serialized, byt setting it to Public or using the [SerializeField] attribute.
	/// </summary>
	[System.Serializable]
	public class SharedRigidbodyCrusher : SharedCrusherBase<RigidbodyCrusher>
	{
		
			/// <summary>
			/// Constructor that uses a supplied hashcode for identifying a shared crusher. If a crusher with this hascode has been constructed,
			/// then the existing crusher will be used. If not a new one will be instantiated and used.
			/// </summary>
			/// <param name="hashcode"></param>
			public SharedRigidbodyCrusher(int hashcode) : base(hashcode) { }

			/// <summary>
			/// Constructor that uses a supplied name as a hashcode for identifying a shared crusher. If a crusher with this hascode has been constructed,
			/// then the existing crusher will be used. If not a new one will be instantiated and used.
			/// </summary>
			/// <param name="hashcode"></param>
			public SharedRigidbodyCrusher(string name) : base(name) { }

			/// <summary>
			/// Constructor that uses StackTrace and SerializedProperty to determined which instances are common and should share a crusher instance.
			/// \nPrefab : All instances of a prefab will share a crusher instance.
			/// \nFieldName : All crushers with this same field name will share a crusher instance, even across components and scenes.
			/// \nComponent and FieldName : All instances of this component will share a common crusher for this field.
			/// </summary>
			/// <param name="shareBy"></param>
			public SharedRigidbodyCrusher(ShareByCommon shareBy = ShareByCommon.Prefab) : base(shareBy) { }

	}

#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(SharedRigidbodyCrusher))]
	[CanEditMultipleObjects]

	public class SharedRigidbodyCrusherDrawer : SharedCrusherBaseDrawer<SharedRigidbodyCrusher, RigidbodyCrusher>
	{
		//public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		//{

		//	base.OnGUI(position, property, label);
		//}

		//public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		//{
		//	return base.GetPropertyHeight(property, label);
		//}
	}

#endif
}

