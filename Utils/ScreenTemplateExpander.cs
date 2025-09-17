using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using UnityEngine;
using Il2CppTMPro;

namespace FS_LevelEditor
{
	// Handles expanding template placeholders in screen text and optional runtime updates
	public static class ScreenTemplateExpander
	{
		// Regex to match %placeholder% tokens
		static readonly Regex TokenRegex = new("%[a-zA-Z_]+%", RegexOptions.Compiled);

		public static string Expand(string raw)
		{
			if (string.IsNullOrEmpty(raw)) return raw;
			return TokenRegex.Replace(raw, MatchEvaluator);
		}

		static string MatchEvaluator(Match m)
		{
			string token = m.Value.ToLowerInvariant();
			try
			{
				switch (token)
				{
					case "%username%":
						return GetUserName();
					case "%time%":
						return DateTime.Now.ToString("HH:mm:ss");
					case "%date%":
						return DateTime.Now.ToString("dd/MM");
					case "%year%":
						return DateTime.Now.Year.ToString();
					case "%name%":
						return GetLevelName();
					default:
						return m.Value; // leave unknown token as-is
				}
			}
			catch
			{
				return m.Value;
			}
		}

		static string GetLevelName()
		{
			// Prefer Editor name if present, else PlayMode name
			if (Editor.EditorController.Instance)
			{
				return Editor.EditorController.Instance.levelName ?? "";
			}
			if (Playmode.PlayModeController.Instance)
			{
				return Playmode.PlayModeController.Instance.levelName ?? "";
			}
			return string.Empty;
		}

		static string GetUserName()
		{
			// Try Steam persona name via reflection to avoid hard dependency on symbol names
			try
			{
				// Steamworks.NET typical namespace is Steamworks with class SteamFriends
				var steamFriendsType = Type.GetType("Steamworks.SteamFriends, Il2Cppcom.rlabrecque.steamworks.net");
				if (steamFriendsType != null)
				{
					var method = steamFriendsType.GetMethod("GetPersonaName", BindingFlags.Public | BindingFlags.Static);
					if (method != null)
					{
						var result = method.Invoke(null, null) as string;
						if (!string.IsNullOrWhiteSpace(result)) return result;
					}
				}
			}
			catch { }

			// Fallback to OS user name (Windows only requirement, but works cross-platform if allowed)
			try
			{
				return Environment.UserName;
			}
			catch { }

			return "Player";
		}

		public static bool ContainsDynamicToken(string raw)
		{
			if (string.IsNullOrEmpty(raw)) return false;
			// Tokens that can change over time (time / date / year / username / level name)
			return raw.Contains("%time%", StringComparison.OrdinalIgnoreCase)
				|| raw.Contains("%date%", StringComparison.OrdinalIgnoreCase)
				|| raw.Contains("%year%", StringComparison.OrdinalIgnoreCase)
				|| raw.Contains("%username%", StringComparison.OrdinalIgnoreCase)
				|| raw.Contains("%name%", StringComparison.OrdinalIgnoreCase);
		}

		public static void EnsureUpdater(TextMeshPro tmp, string rawTemplate)
		{
			if (!tmp) return;
			var updater = tmp.gameObject.GetComponent<ScreenTemplateUpdater>();
			if (!ContainsDynamicToken(rawTemplate))
			{
				if (updater) updater.enabled = false; // disable if previously used
				return;
			}
			if (!updater)
			{
				updater = tmp.gameObject.AddComponent<ScreenTemplateUpdater>();
			}
			updater.SetTemplate(rawTemplate);
		}
	}

	[MelonLoader.RegisterTypeInIl2Cpp]
	public class ScreenTemplateUpdater : MonoBehaviour
	{
		TextMeshPro _tmp;
		string _rawTemplate;
		float _nextUpdate;
		const float UPDATE_INTERVAL = 1f; // update once per second

		void Awake()
		{
			_tmp = gameObject.GetComponent<TextMeshPro>();
		}

		public void SetTemplate(string raw)
		{
			_rawTemplate = raw;
			// Force immediate update
			_nextUpdate = 0f;
			enabled = true;
		}

		void Update()
		{
			if (!_tmp || string.IsNullOrEmpty(_rawTemplate)) { enabled = false; return; }
			if (Time.unscaledTime >= _nextUpdate)
			{
				_tmp.text = ScreenTemplateExpander.Expand(_rawTemplate);
				_nextUpdate = Time.unscaledTime + UPDATE_INTERVAL;
			}
		}
	}
}
