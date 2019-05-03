#if UNITY_EDITOR
namespace SmartAssets
{
	using System;
	using UObject = UnityEngine.Object;
	using CreateFunc = System.Func<UnityEngine.Object>;
	using System.Reflection;
	using System.Linq;

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class AssetGroupOption : Attribute
	{
		public string MenuName { get { return _menuName; } }
		public AssetGroupOption(string menuName) { _menuName = menuName; }
		private string _menuName = "";
		private AssetGroupOption() { }

		public static CreateAssetInfo[] FindMethods()
		{
			var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
			var tl = new System.Collections.Generic.List<Type>();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				var types = assemblies[i].GetTypes();
				for (int j = 0; j < types.Length; j++) { tl.Add(types[j]); }
			}
			var meths = tl.SelectMany(t => t.GetMethods(flags))
			.Where(m => m.GetCustomAttributes(typeof(AssetGroupOption), false).Length > 0)
			.Where(m => m.ReturnType == typeof(UObject) && m.GetParameters().Length == 0);
			var infos = meths.Select(m => new CreateAssetInfo(((AssetGroupOption) m.GetCustomAttributes(typeof(AssetGroupOption), true)[0]).MenuName,
					  () => (UObject) m.Invoke(null, new object[0]))).ToArray();
			return infos;
		}

		public struct CreateAssetInfo
		{
			public string menuName { get { return _menuName; } }
			public UObject Instantiate() { return _fn != null ? _fn() : default(UObject); }
			public CreateAssetInfo(string menuName, CreateFunc fn)
			{
				_fn = fn;
				_menuName = menuName;
			}
			private CreateFunc _fn;
			private string _menuName;
		}
	}
}
#endif