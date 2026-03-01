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
            #region Create Dark Background
            GameObject backgroundObj = new GameObject("DarkOverlay");
			backgroundObj.transform.parent = transform;
			backgroundObj.transform.localPosition = Vector3.zero;
			backgroundObj.transform.localScale = Vector3.one;
			backgroundObj.layer = LayerMask.NameToLayer("2D GUI");

			UISprite backgroundSprite = backgroundObj.AddComponent<UISprite>();
			backgroundSprite.atlas = NGUI_Utils.fractalSpaceAtlas;
			backgroundSprite.spriteName = "Square";
			backgroundSprite.type = UIBasicSprite.Type.Sliced;
			backgroundSprite.color = new Color(0f, 0f, 0f, 0.85f); // Dark semi-transparent
			backgroundSprite.width = 10000;
			backgroundSprite.height = 10000;
			backgroundSprite.depth = 99; // Just below the popup

			// Add collider to block clicks
			BoxCollider backgroundCollider = backgroundObj.AddComponent<BoxCollider>();
			backgroundCollider.size = new Vector3(10000, 10000, 1);
            #endregion

            #region Create Main Popup
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
			bgSprite.depth = 100;
            #endregion

            // Title - smaller font
            UILabel titleLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(0, 180), new Vector3Int(520, 35, 0), "Save Level", 
				NGUIText.Alignment.Center, UIWidget.Pivot.Center);
			titleLabel.name = "Title";
			titleLabel.fontSize = 32;
			titleLabel.depth = 101;

            #region Level Name
            UILabel levelNameLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(-260, 130), new Vector3Int(100, 30, 0), "Level Name:");
			levelNameLabel.name = "LevelNameLabel";
			levelNameLabel.depth = 101;
			levelNameLabel.fontSize = 20;
			levelNameLabel.pivot = UIWidget.Pivot.Left;

			levelNameField = NGUI_Utils.CreateInputField(popupPanel.transform, new Vector3(55, 130), new Vector3Int(410, 32, 0), 20, 
				EditorController.Instance.levelName, false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.PLAIN_TEXT, depth: 101);
			levelNameField.name = "LevelNameField";
			levelNameField.input.label.font = NGUI_Utils.notoSansFont;
            #endregion

            #region Author Name
            UILabel authorLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(-260, 90), new Vector3Int(100, 30, 0), "Author:");
			authorLabel.name = "AuthorNameLabel";
			authorLabel.depth = 101;
			authorLabel.fontSize = 20;
			authorLabel.pivot = UIWidget.Pivot.Left;

			authorNameField = NGUI_Utils.CreateInputField(popupPanel.transform, new Vector3(55, 90), new Vector3Int(410, 32, 0), 20, 
				"", false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.PLAIN_TEXT, depth: 101);
			authorNameField.name = "AuthorNameField";
            #endregion

            #region Tags Name
            UILabel tagsLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(-260, 50), new Vector3Int(100, 30, 0), "Tags:");
			tagsLabel.name = "TagsLabel";
			tagsLabel.depth = 101;
			tagsLabel.fontSize = 20;
			tagsLabel.pivot = UIWidget.Pivot.Left;

			tagsField = NGUI_Utils.CreateInputField(popupPanel.transform, new Vector3(55, 50), new Vector3Int(410, 32, 0), 20, 
				"", false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.PLAIN_TEXT, depth: 101);
			tagsField.name = "TagsField";
            #endregion

            #region Description Field
            UILabel descLabel = NGUI_Utils.CreateLabel(popupPanel.transform, new Vector3(-260, 10), new Vector3Int(150, 30, 0), "Description:");
			descLabel.name = "DescriptionLabel";
			descLabel.depth = 101;
			descLabel.fontSize = 20;
			descLabel.pivot = UIWidget.Pivot.Left;

			descriptionField = NGUI_Utils.CreateInputField(popupPanel.transform, new Vector3(0, -55), new Vector3Int(520, 85, 0), 18, 
				"", false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.PLAIN_TEXT, depth: 101);
			descriptionField.name = "DescriptionField";
			descriptionField.input.validation = UIInput.Validation.None;
			descriptionField.input.characterLimit = 500;
			// Enable multiline
			descriptionField.input.label.maxLineCount = 4;
			descriptionField.input.label.overflowMethod = UILabel.Overflow.ClampContent;
            #endregion

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
			backgroundObj.SetActive(false);
			
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

			EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
			EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.SAVE_METADATA_PANEL);
			
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

			EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
			EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
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

			EditorController.Instance.levelHasBeenModified = false;

			// Hide popup first, then show notification to avoid visual conflicts
			HidePopup();

			// Show "Saved!" notification after popup is hidden
			if (NotificationSystem.Instance != null)
			{
				NotificationSystem.Instance.ShowNotification("Level saved!", "WhiteSquare");
			}

			Logger.Log($"Level saved with metadata - Name: {levelName}, Author: {authorName}, Tags: {tags}");
		}

		public void OnCancelButtonClicked()
		{
			// Discard changes and close
			Logger.Log("Save popup cancelled - discarding changes");
			HidePopup();
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
