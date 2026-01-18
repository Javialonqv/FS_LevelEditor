using FS_LevelEditor.Editor;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using static FS_LevelEditor.LE_Death_Trigger;

namespace FS_LevelEditor
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class LE_Destructible_Wall : LE_Object
	{

		private List<BrickMaterialController> bricks = new List<BrickMaterialController>();

		void Awake()
		{
			if(EditorController.Instance)
			{
				Transform debrisParent = gameObject.GetChildAt("Content/Debris").transform;
				for (int i = 0; i < debrisParent.childCount; i++)
				{
					Transform child = debrisParent.GetChild(i);
					child.GetComponent<Rigidbody>().useGravity = false;
				}
			}
		}

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "Lifetime", 10f },
                { "OnBreak", new List<LE_Event>() }
            };
        }

        public override void InitComponent()
		{
			GameObject content = gameObject.GetChild("Content");
			content.SetActive(false);

			DestructibleWall wall = content.AddComponent<DestructibleWall>();
			wall.originalMesh = content.GetChild("OriginalMesh").GetComponent<MeshRenderer>();
			Transform shatteredParent = content.GetChild("DestrWall_Shattered").transform;
			for (int i = 0; i < shatteredParent.childCount; i++)
			{
				Transform piece = shatteredParent.GetChild(i);
				piece.tag = "Destructible";
				MovingPlatformProxy proxy = piece.gameObject.AddComponent<MovingPlatformProxy>();
				proxy.shouldReact = true;
				BrickMaterialController brick = piece.gameObject.AddComponent<BrickMaterialController>();
				brick.associatedWall = wall;
				brick.platformProxy = proxy;
				brick.taserMinimumLevel = 1;
				brick.disappearHeight = -100;
				brick.m_meshCollider = piece.GetComponent<MeshCollider>();
				brick.m_audioSource = piece.GetComponent<AudioSource>();
				brick.m_audioSource.outputAudioMixerGroup = t_breakableWall.allParts[0].m_audioSource.outputAudioMixerGroup;
				brick.m_bounceSounds = t_breakableWall.allParts[0].m_bounceSounds;
				brick.m_renderer = piece.GetComponent<MeshRenderer>();
				brick.broken = false;
				brick.rb = piece.GetComponent<Rigidbody>();
				brick.lifetime = GetProperty<float>("Lifetime");
				bricks.Add(brick);
			}
			wall.allParts = bricks.ToArray();
			wall.fakeBreakParts = bricks.ToArray();
			wall.m_audioSource = content.GetComponent<AudioSource>();
			wall.m_audioSource.outputAudioMixerGroup = t_breakableWall.m_audioSource.outputAudioMixerGroup;
			ConfigureEvents(wall);
			wall.useManualExplosion = false;
			wall.manualExplosionExplodesOtherWalls = false;
			wall.manualExplosionPosT = null;
			wall.manualExplosionForce = 60;
			wall.manualExplosionOtherBrickRadius = 8;
			wall.manualExplosionBrickRef = null;
			wall.manualExplosionDelay = 0;
			wall.delayIgnoresTimecale = true;
			wall.controlScript = null;
			wall.manualExplosionVFXIsBefore = true;

			//layers
			content.GetChildAt("OriginalMesh").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
			content.GetChildAt("OriginalMesh/PlayerCollisionOnly").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
			content.GetChildAt("OriginalMesh/PreciseWallDetection").layer = LayerMask.NameToLayer("LaserObstructionOnly");
			foreach(GameObject walldetect in content.GetChildAt("OriginalMesh/PreciseWallDetection").GetChilds(true))
			{
				walldetect.tag = "OriginalMesh_Destructible";
				walldetect.layer = LayerMask.NameToLayer("LaserObstructionOnly");
			}

			content.SetActive(true);
			initialized = true;
		}
		public override bool SetProperty(string name, object value)
		{
			if (GetAvailableEventsIDs().Contains(name))
			{
				if (value is List<LE_Event>)
				{
					properties[name] = (List<LE_Event>)value;
				}
			}
			else if (name == "Lifetime")
			{
				if (value is string)
				{
					if (Utils.TryParseFloat((string)value, out float result))
					{
						properties["Lifetime"] = result;
						return true;
					}
				}
				else if (value is float)
				{
					properties["Lifetime"] = (float)value;
					return true;
				}
			}
			return base.SetProperty(name, value);
		}
		void ConfigureEvents(DestructibleWall script)
		{
			script.onBreak = new UnityEngine.Events.UnityEvent();
			script.onBreak.AddListener((UnityAction)ExecuteOnBreakEvents);
		}
		void ExecuteOnBreakEvents()
		{
			eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnBreak"]);
		}
		public override List<string> GetAvailableEventsIDs()
		{
			return new List<string>()
			{
				"OnBreak"
			};
		}
	}
}
