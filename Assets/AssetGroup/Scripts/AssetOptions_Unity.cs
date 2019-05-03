#if UNITY_EDITOR

namespace SmartAssets.Editor
{
	using UnityEngine;
	using UnityEditor;
	using UnityEditor.Animations;
	using UnityEngine.Timeline;
	internal static class AssetOptions_Unity
	{
		[AssetGroupOption("Material")] private static Object GetMaterial() { return new Material(Shader.Find("Standard")); }
		[AssetGroupOption("Render Texture")] private static Object GetRenderTexture() { return new RenderTexture(256, 256, 24); }
		[AssetGroupOption("GUI Skin")] private static Object GetGUISkin() { return new GUISkin(); }
		[AssetGroupOption("Animations/Animation Controller")] private static Object GetAnimationController() { return new AnimatorController(); }
		[AssetGroupOption("Animations/Avatar Mask")] private static Object GetAvatarMask() { return new AvatarMask(); }
		[AssetGroupOption("Timeline/Timeline Asset")] private static Object GetTimeline() { return ScriptableObject.CreateInstance<TimelineAsset>(); }
		[AssetGroupOption("Editor/Lightmap Parameters")] private static Object GetLMParameters() { return new LightmapParameters(); }
		[AssetGroupOption("Physics/Physic Material")] private static Object GetPhysicMaterial() { return new PhysicMaterial(); }
		// [AssetGroupOption("Physics/Physics Material 2D")] private static Object GetPhysicsMaterial2D() { return new PhysicsMaterial2D(); }
	}
}
#endif