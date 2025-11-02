using FS_LevelEditor.UI_Related;
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{
	[RegisterTypeInIl2Cpp]
	public class NotificationSystem : MonoBehaviour
	{
		public static NotificationSystem Instance;

		private GameObject notificationPanel;
		private UISprite notificationBg;
		private UISprite notificationIcon;
		private UILabel notificationLabel;
		
		private Coroutine currentNotificationCoroutine;
		private const float NOTIFICATION_DURATION = 3f;
		private const float SLIDE_DURATION = 0.3f;
		
		// Position constants
		private const float VISIBLE_X = 955f; // Position when visible on screen (flush with right edge)
		private const float HIDDEN_X = 1355f; // Position when hidden off-screen (further right)
		private const float Y_POSITION = 100f; // Higher than center
		
		// Track current notification state
		private string currentMessage = "";
		private bool isSliding = false; // Track if we're currently animating
		private bool isFullyVisible = false; // Track if notification is fully visible and ready for updates

		public NotificationSystem(IntPtr ptr) : base(ptr) { }

		public static void Create(Transform parent)
		{
			if (Instance != null)
			{
				Logger.Error("NotificationSystem instance already exists!");
				return;
			}

			GameObject obj = new GameObject("NotificationSystem");
			obj.transform.parent = parent;
			obj.transform.localScale = Vector3.one;
			obj.transform.localPosition = Vector3.zero;

			Instance = obj.AddComponent<NotificationSystem>();
			Instance.CreateNotificationPanel();
		}

		void CreateNotificationPanel()
		{
			// Create the main panel container - start off-screen
			notificationPanel = new GameObject("NotificationPanel");
			notificationPanel.transform.parent = transform;
			notificationPanel.transform.localScale = Vector3.one;
			notificationPanel.transform.localPosition = new Vector3(HIDDEN_X, Y_POSITION, 0f); // Start hidden off-screen
			notificationPanel.layer = LayerMask.NameToLayer("2D GUI");

			// Background sprite
			notificationBg = notificationPanel.AddComponent<UISprite>();
			notificationBg.atlas = NGUI_Utils.UITexturesAtlas;
			notificationBg.spriteName = "Square_Border_Beveled_HighOpacity";
			notificationBg.type = UIBasicSprite.Type.Sliced;
			notificationBg.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
			notificationBg.width = 350;
			notificationBg.height = 70;
			notificationBg.pivot = UIWidget.Pivot.Right;
			notificationBg.depth = 100;
			notificationBg.autoResizeBoxCollider = true; // Auto-resize collider with sprite

			// Icon sprite (on the left inside the notification)
			GameObject iconObj = new GameObject("Icon");
			iconObj.transform.parent = notificationPanel.transform;
			iconObj.transform.localScale = Vector3.one;
			iconObj.transform.localPosition = new Vector3(-315f, 0f, 0f);

			notificationIcon = iconObj.AddComponent<UISprite>();
			notificationIcon.atlas = NGUI_Utils.fractalSpaceAtlas;
			notificationIcon.spriteName = "WhiteSquare";
			notificationIcon.type = UIBasicSprite.Type.Simple;
			notificationIcon.width = 40;
			notificationIcon.height = 40;
			notificationIcon.pivot = UIWidget.Pivot.Center;
			notificationIcon.depth = 101;
			notificationIcon.color = Color.white;

			// Text label (on the right of the icon)
			notificationLabel = NGUI_Utils.CreateLabel(notificationPanel.transform, new Vector3(-150f, 0f, 0f), 
				new Vector3Int(260, 60, 0), "", NGUIText.Alignment.Left, UIWidget.Pivot.Center);
			notificationLabel.fontSize = 24;
			notificationLabel.depth = 101;
			notificationLabel.overflowMethod = UILabel.Overflow.ResizeFreely; // Allow label to resize freely
			notificationLabel.multiLine = true; // Enable multi-line support for welcome message

			// Start hidden
			notificationPanel.SetActive(false);
		}

		public void ShowNotification(string message, string iconSpriteName = "WhiteSquare")
		{
			// If we're currently sliding in, ignore new requests to prevent flickering
			if (isSliding)
			{
				return;
			}

			// If already fully visible, just update text and reset timer
			if (isFullyVisible)
			{
				// Just update the text and restart the timer without animation
				notificationLabel.text = message;
				notificationIcon.spriteName = iconSpriteName;
				currentMessage = message;
				
				// Update background height based on label content
				UpdateNotificationHeight();
				
				// Stop and restart the timer
				if (currentNotificationCoroutine != null)
				{
					MelonCoroutines.Stop(currentNotificationCoroutine);
				}
				currentNotificationCoroutine = (Coroutine)MelonCoroutines.Start(WaitAndHideCoroutine());
				return;
			}

			// If there's already a notification showing but not fully visible, stop it
			if (currentNotificationCoroutine != null)
			{
				MelonCoroutines.Stop(currentNotificationCoroutine);
				currentNotificationCoroutine = null;
			}

			// Update content
			notificationLabel.text = message;
			notificationIcon.spriteName = iconSpriteName;
			currentMessage = message;
			
			// Update background height based on label content
			UpdateNotificationHeight();

			// Start the notification coroutine
			currentNotificationCoroutine = (Coroutine)MelonCoroutines.Start(ShowNotificationCoroutine());
		}
		
		void UpdateNotificationHeight()
		{
			// Force label to recalculate its size
			notificationLabel.UpdateNGUIText();
			
			// Get the actual rendered height of the label (printedSize is a Vector2)
			Vector2 labelSize = notificationLabel.printedSize;
			
			// Add padding (20 pixels top and bottom)
			int totalHeight = Mathf.Max(70, Mathf.RoundToInt(labelSize.y + 40));
			
			// Update background height
			notificationBg.height = totalHeight;
			
			// Reposition icon to be centered vertically
			notificationIcon.transform.localPosition = new Vector3(-315f, 0f, 0f);
		}

		IEnumerator ShowNotificationCoroutine()
		{
			isSliding = true;
			isFullyVisible = false;
			
			// Ensure panel is active and at hidden position
			if (!notificationPanel.activeSelf)
			{
				notificationPanel.SetActive(true);
			}
			
			notificationPanel.transform.localPosition = new Vector3(HIDDEN_X, Y_POSITION, 0f);
			
			// Slide in from right
			TweenPosition slideIn = TweenPosition.Begin(notificationPanel, SLIDE_DURATION, new Vector3(VISIBLE_X, Y_POSITION, 0f));
			slideIn.method = UITweener.Method.EaseOut;
			slideIn.ignoreTimeScale = true;
			
			yield return new WaitForSecondsRealtime(SLIDE_DURATION);
			
			isSliding = false;
			isFullyVisible = true;

			// Wait for the notification duration
			yield return new WaitForSecondsRealtime(NOTIFICATION_DURATION);

			// Slide out to the right
			isSliding = true;
			isFullyVisible = false;
			TweenPosition slideOut = TweenPosition.Begin(notificationPanel, SLIDE_DURATION, new Vector3(HIDDEN_X, Y_POSITION, 0f));
			slideOut.method = UITweener.Method.EaseIn;
			slideOut.ignoreTimeScale = true;
			
			yield return new WaitForSecondsRealtime(SLIDE_DURATION);

			// Hide the panel
			isSliding = false;
			notificationPanel.SetActive(false);
			currentNotificationCoroutine = null;
			currentMessage = "";
		}

		IEnumerator WaitAndHideCoroutine()
		{
			// Wait for the notification duration
			yield return new WaitForSecondsRealtime(NOTIFICATION_DURATION);

			// Slide out to the right
			isSliding = true;
			isFullyVisible = false;
			TweenPosition slideOut = TweenPosition.Begin(notificationPanel, SLIDE_DURATION, new Vector3(HIDDEN_X, Y_POSITION, 0f));
			slideOut.method = UITweener.Method.EaseIn;
			slideOut.ignoreTimeScale = true;
			
			yield return new WaitForSecondsRealtime(SLIDE_DURATION);

			// Hide the panel
			isSliding = false;
			notificationPanel.SetActive(false);
			currentNotificationCoroutine = null;
			currentMessage = "";
		}

		void OnDestroy()
		{
			if (currentNotificationCoroutine != null)
			{
				MelonCoroutines.Stop(currentNotificationCoroutine);
			}
			Instance = null;
		}
	}
}
