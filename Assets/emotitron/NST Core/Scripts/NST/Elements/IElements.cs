//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Utilities.SmartVars;
using emotitron.Compression;

namespace emotitron.NST
{
	public interface INSTTransformElement
	{
		GameObject SrcGameObject { get; }
		TransformElement TransElement { get; }
		bool Enabled { get; }
	}

	public interface ITransformElements
	{
		IncludedAxes IncludedAxes { get; }
		GenericX Localized { get; set; }
		//bool this[int axisId] { get; }
		Vector3 GetCorrectedForOutOfBounds(Vector3 unclamped);
		void Apply(GenericX pos, GameObject targetGO);
		void Apply(GenericX pos);
	}

	public interface IPositionElement : ITransformElements
	{
		//FloatRange[] AxisRanges { get; }
		//IncludedAxes IncludedAxes { get; }
	}

	public interface IScaleElement : ITransformElements
	{
		//FloatRange[] AxisRanges { get; }
	}

	public interface IRotationElement : ITransformElements
	{
		//RotationType RotationType { get; }
	}
}

#endif
