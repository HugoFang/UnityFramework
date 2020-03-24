using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework
{
	namespace Utils
	{
		public static class SceneUtils
		{
			public static T FindInScene<T>(Scene scene, bool includeInactive = false) where T : Component
			{
				T component = null;
				
				foreach (GameObject rootObject in scene.GetRootGameObjects())
				{
					component = rootObject.GetComponentInChildren<T>(includeInactive);

					if (component != null)
					{
						break;
					}
				}

				return component;
			}

			public static GameObject FindInScene(Scene scene, string name)
			{
				GameObject gameObject = null;

				foreach (GameObject rootObject in scene.GetRootGameObjects())
				{
					Transform transform = rootObject.transform.Find(name);

					if (transform != null)
					{
						gameObject = transform.gameObject;
						break;
					}
				}

				return gameObject;
			}

			public static T FindInScene<T>(Scene scene, string name) where T : Component
			{
				T component = null;

				foreach (GameObject rootObject in scene.GetRootGameObjects())
				{
					Transform transform = rootObject.transform.Find(name);

					if (transform != null)
					{
						component = transform.gameObject.GetComponent<T>();

						if (component != null)
						{
							break;
						}
					}
				}

				return component;
			}

			public static T[] FindAllInScene<T>(Scene scene, bool includeInactive = false) where T : Component
			{
				List<T> components = new List<T>();

				if (scene.IsValid())
				{
					foreach (GameObject rootObject in scene.GetRootGameObjects())
					{
						components.AddRange(rootObject.GetComponentsInChildren<T>(includeInactive));
					}
				}			

				return components.ToArray();
			}


			public static T[] FindAllComponentInferfacesInScene<T>(Scene scene, bool includeInactive = false) where T : class
			{
				List<T> components = new List<T>();

				if (scene.IsValid())
				{
					foreach (GameObject rootObject in scene.GetRootGameObjects())
					{
						if (includeInactive || rootObject.activeSelf)
							AddComponentInferfacesInGameObject(rootObject, ref components, includeInactive);
					}
				}

				return components.ToArray();			
			}

			private static void AddComponentInferfacesInGameObject<T>(GameObject gameObject, ref List<T> components, bool includeInactive = true) where T : class
			{
				Component[] cmponents = gameObject.GetComponents<Component>();

				for (int i = 0; i < cmponents.Length; i++)
				{
					if (cmponents[i] is T typedComp)
						components.Add(typedComp);
				}

				foreach (Transform child in gameObject.transform)
				{
					if (includeInactive || child.gameObject.activeSelf)
						AddComponentInferfacesInGameObject(child.gameObject, ref components);
				}
			}

			public static string GetSceneNameFromPath(string scenePath)
			{
				int folder = scenePath.LastIndexOf("/") + 1;
				int file = scenePath.LastIndexOf(".");
				return scenePath.Substring(folder, file - folder);
			}
		}
	}
}