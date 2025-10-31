using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.UI_Related;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class SaveMetadataPopup : MonoBehaviour
	{
		public static SaveMetadataPopup Instance;

		public GameObject popupPanel;
		UICustomInputField levelNameField;
		UICustomInputField authorNameField;
		UICustomInputField tagsField;
		UICustomInputField descriptionField;
		UIButtonPatcher saveButton;
		UIButtonPatcher cancelButton;

		bool isShowing = false;
		bool wasPausedBeforeShow = false;

		public SaveMetadataPopup(IntPtr ptr) : base(ptr) { }

		public static void Create()
		{
			GameObject root = new GameObject("SaveMetadataPopup");
			root.transform.parent = EditorUIManager.Instance.editorUIParent.transform;
			root.transform.localPosition = Vector3.zero;
			root.transform.localScale = Vector3.one;

			root.AddComponent<SaveMetadataPopup>();
		}

		void Awake()
		{
			Instance = this;
			CreatePopupUI();
			Logger.Log("SaveMetadataPopup initialized successfully");
		}

		void CreatePopupUI()
		{
			// Create darkened background overlay
			GameObject overlay = new GameObject("DarkOverlay");
			overlay.transform.parent = transform;
			overlay.transform.localPosition = Vector3.zero;
			overlay.transform.localScale = Vector3.one;
			overlay.layer = LayerMask.NameToLayer("2D GUI");

			UISprite overlaySprite = overlay.AddComponent<UISprite>();
			overlaySprite.atlas = NGUI_Utils.fractalSpaceAtlas;
			overlaySprite.spriteName = "Square";
			overlaySprite.type = UIBasicSprite.Type.Sliced;
			overlaySprite.color = new Color(0f, 0f, 0f, 0.85f); // Dark semi-transparent
			overlaySprite.width = 10000;
			overlaySprite.height = 10000;
			overlaySprite.depth = 499; // Just below the popup

			// Add collider to block clicks
			BoxCollider overlayCollider = overlay.AddComponent<BoxCollider>();
			overlayCollider.size = new Vector3(10000, 10000, 1);

			// Create main popup panel
			popupPanel = new GameObject("SaveMetadataPanel");
			popupPanel.transform.parent = transform;
			popupPanel.transform.localPosition = Vector3.zero;
			popupPanel.transform.localScale = Vector3.one;
			popupPanel.layer = LayerMask.NameToLayer("2D GUI");

			// Background sprite - using much higher depth to be on top
			UISprite bgSprite = popupPanel.AddComponent<UISprite>();
			bgSprite.atlas = NGUI_Utils.UITexturesAtlas;
			bgSprite.spriteName = "Square_Border_Beveled_HighOpacity";
			bgSprite.type = UIBasicSprite.Type.Sliced;
			bgSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
			bgSprite.width = 550;
			bgSprite.height = 420;
			bgSprite.depth = 500;

			// Title - smaller font
			UILabel titleLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(0, 185), new Vector3Int(550, 35, 0), "Save Level", 
				NGUIText.Alignment.Center, UIWidget.Pivot.Center);
			titleLabel.fontSize = 32;
			titleLabel.depth = 501;

			// Level Name Field
			UILabel levelNameLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(-255, 135), new Vector3Int(90, 30, 0), "Level Name:");
			levelNameLabel.depth = 501;
			levelNameLabel.fontSize = 20;
			levelNameLabel.pivot = UIWidget.Pivot.Left;

			levelNameField = NGUI_Utils.CreateInputField(popupPanel.transform, new Vector3(15, 135), new Vector3Int(380, 32, 0), 20, 
				EditorController.Instance.levelName, false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.PLAIN_TEXT, depth: 501);
			levelNameField.name = "LevelNameField";

			// Author Name Field
			UILabel authorLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(-255, 90), new Vector3Int(90, 30, 0), "Author:");
			authorLabel.depth = 501;
			authorLabel.fontSize = 20;
			authorLabel.pivot = UIWidget.Pivot.Left;

			authorNameField = NGUI_Utils.CreateInputField(popupPanel.transform, new Vector3(15, 90), new Vector3Int(380, 32, 0), 20, 
				"", false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.PLAIN_TEXT, depth: 501);
			authorNameField.name = "AuthorNameField";

			// Tags Field
			UILabel tagsLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(-255, 45), new Vector3Int(90, 30, 0), "Tags:");
			tagsLabel.depth = 501;
			tagsLabel.fontSize = 20;
			tagsLabel.pivot = UIWidget.Pivot.Left;

			tagsField = NGUI_Utils.CreateInputField(popupPanel.transform, new Vector3(15, 45), new Vector3Int(380, 32, 0), 20, 
				"", false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.PLAIN_TEXT, depth: 501);
			tagsField.name = "TagsField";

			// Description Field (multiline)
			UILabel descLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(-255, 0), new Vector3Int(90, 30, 0), "Description:");
			descLabel.depth = 501;
			descLabel.fontSize = 20;
			descLabel.pivot = UIWidget.Pivot.Left;

			descriptionField = NGUI_Utils.CreateInputField(popupPanel.transform, new Vector3(0, -55), new Vector3Int(500, 85, 0), 18, 
				"", false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.PLAIN_TEXT, depth: 501);
			descriptionField.name = "DescriptionField";
			descriptionField.input.validation = UIInput.Validation.None;
			descriptionField.input.characterLimit = 500;
			// Enable multiline
			descriptionField.input.label.maxLineCount = 4;
			descriptionField.input.label.overflowMethod = UILabel.Overflow.ClampContent;

			// Save Button
			saveButton = NGUI_Utils.CreateButton(popupPanel.transform, new Vector3(-90, -160), new Vector3Int(160, 45, 0), "Save", 502, 26);
			saveButton.onClick += OnSaveButtonClicked;

			// Cancel Button
			cancelButton = NGUI_Utils.CreateButton(popupPanel.transform, new Vector3(90, -160), new Vector3Int(160, 45, 0), "Cancel", 502, 26);
			cancelButton.onClick += OnCancelButtonClicked;

			// Add scale animation
			TweenScale tweenScale = popupPanel.AddComponent<TweenScale>();
			tweenScale.from = Vector3.zero;
			tweenScale.to = Vector3.one;
			tweenScale.duration = 0.2f;
			tweenScale.ignoreTimeScale = true;

			popupPanel.SetActive(false);
			overlay.SetActive(false);
			
			Logger.Log("SaveMetadataPopup UI created successfully");
		}

		public void ShowPopup()
		{
			if (isShowing)
			{
				Logger.Warning("SaveMetadataPopup is already showing");
				return;
			}

			Logger.Log("Showing SaveMetadataPopup");
			
			// Store pause state and temporarily disable it to allow button interaction
			wasPausedBeforeShow = InGameUIManager.Instance.isInPauseMode;
			InGameUIManager.Instance.isInPauseMode = false;
			
			// Set showing flag AFTER storing pause state
			isShowing = true;

			// Load existing metadata if it exists
			LevelData existingData = LevelData.GetLevelData(EditorController.Instance.levelFileNameWithoutExtension);
			if (existingData != null)
			{
				levelNameField.SetText(existingData.levelName);
				authorNameField.SetText(existingData.authorName ?? "");
				tagsField.SetText(existingData.tags ?? "");
				descriptionField.SetText(existingData.description ?? "");
			}
			else
			{
				levelNameField.SetText(EditorController.Instance.levelName);
				authorNameField.SetText("");
				tagsField.SetText("");
				descriptionField.SetText("");
			}

			// Show overlay and popup
			transform.GetChild(0).gameObject.SetActive(true); // Overlay
			popupPanel.SetActive(true);
			popupPanel.GetComponent<TweenScale>().PlayForward();
			Utils.PlayFSUISound(Utils.FS_UISound.POPUP_UI_SHOW);
			
			Logger.Log("SaveMetadataPopup shown successfully");
		}

		public void HidePopup()
		{
			if (!isShowing) return;

			// Restore pause state IMMEDIATELY before animation starts
			if (wasPausedBeforeShow)
			{
				InGameUIManager.Instance.isInPauseMode = true;
			}

			// Set isShowing to false immediately to prevent double-hiding
			isShowing = false;

			popupPanel.GetComponent<TweenScale>().PlayReverse();
			MelonLoader.MelonCoroutines.Start(HideAfterAnimation());

			Utils.PlayFSUISound(Utils.FS_UISound.POPUP_UI_HIDE);
		}

		System.Collections.IEnumerator HideAfterAnimation()
		{
			yield return new WaitForSecondsRealtime(0.2f);
			popupPanel.SetActive(false);
			transform.GetChild(0).gameObject.SetActive(false); // Overlay
		}

		void OnSaveButtonClicked()
		{
			string levelName = levelNameField.GetText();
			string authorName = authorNameField.GetText();
			string tags = tagsField.GetText();
			string description = descriptionField.GetText();

			// Validate level name
			if (string.IsNullOrWhiteSpace(levelName))
			{
				Utils.ShowCustomNotificationRed("Level name cannot be empty", 2f);
				return;
			}

			// Get the old file name
			string oldFileNameWithoutExtension = EditorController.Instance.levelFileNameWithoutExtension;
			string oldLevelName = EditorController.Instance.levelName;

			// Update EditorController level name
			EditorController.Instance.levelName = levelName;

			// Create level data with metadata
			LevelData data = LevelData.CreateLevelData(levelName);
			data.authorName = authorName;
			data.tags = tags;
			data.description = description;
			
			// Capture and encode thumbnail
			try
			{
				data.thumbnailBase64 = CaptureThumbnail();
			}
			catch (Exception ex)
			{
				Logger.Warning($"Failed to capture thumbnail: {ex.Message}");
				data.thumbnailBase64 = null;
			}

			// Check if level name changed - need to rename the file
			if (levelName != oldLevelName)
			{
				// Sanitize the new level name for use as filename
				string newFileNameWithoutExtension = Utils.SanitizeFileName(levelName);
				
				// Check if a file with the new name already exists
				string levelsDirectory = Path.Combine(Application.persistentDataPath, "Custom Levels");
				string newFilePath = Path.Combine(levelsDirectory, newFileNameWithoutExtension + ".lvl");
				
				if (File.Exists(newFilePath) && newFileNameWithoutExtension != oldFileNameWithoutExtension)
				{
					// File already exists, get an available name
					newFileNameWithoutExtension = LevelData.GetAvailableLevelName(levelName);
					newFileNameWithoutExtension = Utils.SanitizeFileName(newFileNameWithoutExtension);
				}

				// Save with the new file name
				LevelData.SaveLevelData(levelName, newFileNameWithoutExtension, data);

				// Delete the old file if the name changed
				if (oldFileNameWithoutExtension != newFileNameWithoutExtension)
				{
					string oldFilePath = Path.Combine(levelsDirectory, oldFileNameWithoutExtension + ".lvl");
					if (File.Exists(oldFilePath))
					{
						File.Delete(oldFilePath);
						Logger.Log($"Deleted old level file: {oldFilePath}");
					}
				}

				// Update the editor controller with the new file name
				EditorController.Instance.levelFileNameWithoutExtension = newFileNameWithoutExtension;
				
				Logger.Log($"Level renamed: '{oldLevelName}' -> '{levelName}' (File: '{oldFileNameWithoutExtension}' -> '{newFileNameWithoutExtension}')");
			}
			else
			{
				// Name didn't change, just save normally
				LevelData.SaveLevelData(levelName, oldFileNameWithoutExtension, data);
			}

			EditorUIManager.Instance.PlaySavingLevelLabel();
			EditorController.Instance.levelHasBeenModified = false;

			HidePopup();

			Logger.Log($"Level saved with metadata - Name: {levelName}, Author: {authorName}, Tags: {tags}");
		}

		void OnCancelButtonClicked()
		{
			// Discard changes and close
			Logger.Log("Save popup cancelled - discarding changes");
			HidePopup();
		}

		void Update()
		{
			// Allow ESC to discard and close popup
			if (isShowing && Input.GetKeyDown(KeyCode.Escape))
			{
				OnCancelButtonClicked();
			}
		}

		/// <summary>
		/// Check if the save popup is currently active
		/// </summary>
		public static bool IsPopupActive()
		{
			return Instance != null && Instance.isShowing;
		}
		
		/// <summary>
		/// Captures a thumbnail of the current editor view and returns it as a base64-encoded PNG string
		/// </summary>
		string CaptureThumbnail()
		{
			Camera camera = Camera.main;
			if (camera == null)
			{
				Logger.Warning("No main camera found for thumbnail capture");
				return null;
			}

			// Define thumbnail dimensions (16:9 aspect ratio)
			int thumbnailWidth = 320;
			int thumbnailHeight = 180;

			// Create a temporary render texture
			RenderTexture currentRT = RenderTexture.active;
			RenderTexture tempRT = RenderTexture.GetTemporary(thumbnailWidth, thumbnailHeight, 24);
			RenderTexture.active = tempRT;

			// Temporarily set camera to render to our texture
			RenderTexture previousCameraRT = camera.targetTexture;
			camera.targetTexture = tempRT;
			
			// Render the camera view
			camera.Render();

			// Read pixels from the render texture
			Texture2D thumbnail = new Texture2D(thumbnailWidth, thumbnailHeight, TextureFormat.RGB24, false);
			thumbnail.ReadPixels(new Rect(0, 0, thumbnailWidth, thumbnailHeight), 0, 0);
			thumbnail.Apply();

			// Restore camera settings
			camera.targetTexture = previousCameraRT;
			RenderTexture.active = currentRT;
			RenderTexture.ReleaseTemporary(tempRT);

			// Encode to PNG and convert to base64
			byte[] pngBytes = thumbnail.EncodeToPNG();
			GameObject.Destroy(thumbnail);
			
			if (pngBytes == null || pngBytes.Length == 0)
			{
				Logger.Warning("Failed to encode thumbnail to PNG");
				return null;
			}

			string base64String = Convert.ToBase64String(pngBytes);
			Logger.Log($"Thumbnail captured successfully ({pngBytes.Length} bytes, {base64String.Length} chars)");
			
			return base64String;
		}
	}
}
