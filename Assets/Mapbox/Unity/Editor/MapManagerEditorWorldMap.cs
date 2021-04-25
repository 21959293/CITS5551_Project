using UnityEditor;
using Mapbox.Editor;

namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using Mapbox.Platform.TilesetTileJSON;
	using System.Collections.Generic;
	using Mapbox.VectorTile.ExtensionMethods;

	[CustomEditor(typeof(WorldMap))]
	[CanEditMultipleObjects]
	public class MapManagerEditorWorldEditor : MapManagerEditor
	{

	}

}
