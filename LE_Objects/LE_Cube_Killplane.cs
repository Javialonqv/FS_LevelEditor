using FS_LevelEditor;
using FS_LevelEditor.Playmode;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class LE_Cube_Killplane : LE_Object
	{
		public bool Unauthorized;
		void Awake()
		{
			properties = new Dictionary<string, object>
			{
				{ "IgnoreIfInHands", false }
			};
		}
		public override void OnInstantiated(LEScene scene)
		{
			if (scene == LEScene.Playmode)
			{
				gameObject.GetChildAt("Content/Mesh").SetActive(false);
			}

			base.OnInstantiated(scene);
		}

		public override void InitComponent()
		{
			GameObject content = gameObject.GetChild("Content");

			content.SetActive(false);
			if(!GetProperty<bool>("IgnoreIfInHands"))
			{
				content.GetChild("Trigger").tag = "KillZoneCube";
			} else
			{
				content.GetChild("Trigger").tag = "KillZoneCube_OnlyIfNotInHands";
			}
			content.GetChild("Trigger").gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
			content.SetActive(true);

			initialized = true;
		}
		public override bool SetProperty(string name, object value)
		{
			if (name == "IgnoreIfInHands")
			{
				if (value is bool)
				{
					properties["IgnoreIfInHands"] = (bool)value;
					return true;
				}
			}
			return base.SetProperty(name, value);
		}
		public static new Color GetDefaultObjectColor(LEObjectContext context)
		{
			return new Color(0f, 1f, 0.07843138f);
		}
	}
}