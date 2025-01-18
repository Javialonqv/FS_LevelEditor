using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_MenuUIManager : MonoBehaviour
    {
        public static LE_MenuUIManager Instance;

        public bool inLEMenu;

        GameObject mainMenu;
        AudioSource uiSoundSource;
        AudioClip okSound;
        AudioClip hidePageSound;

        GameObject levelEditorUIButton;
        public GameObject leMenuPanel;
        GameObject leMenuButtonsParent;
        GameObject backButton;
        GameObject addButton;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            GetSomeReferences();
            CreateLEButton();
            CreateLEMenuPanel();
            CreateBackButton();
            CreateAddButton();
        }

        void Update()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.F3))
            {
                SwitchBetweenMenuAndLEMenu();
            }
#endif
            if (EditorUIManager.Instance == null)
            {
                levelEditorUIButton.SetActive(Core.currentSceneName.Contains("Menu"));
            }
        }

        void GetSomeReferences()
        {
            mainMenu = GameObject.Find("MainMenu/Camera/Holder/Main");
            uiSoundSource = GameObject.Find("MainMenu/UISound").GetComponent<AudioSource>();
            // Ik this isn't the best way to get these clips, but it works, so... I'mn not touching it again lol.
            okSound = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/2_Chapters").GetComponent<ButtonController>().m_pressSound;
            okSound.hideFlags = HideFlags.DontUnloadUnusedAsset;
            hidePageSound = MenuController.GetInstance().hidePageSound;
            hidePageSound.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        // LE stands for "Level Editor" lmao.
        void CreateLEButton()
        {
            // The game disables the existing LE button since it detects we aren't in the unity editor or debugging, so I need to create a copy of the button.
            GameObject defaultLEButton = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/6_LevelEditor");
            levelEditorUIButton = GameObject.Instantiate(defaultLEButton, defaultLEButton.transform.parent);
            levelEditorUIButton.name = "6_Javi's LevelEditor";

            // And why not? Destroy the old button, since we don't need it anymore ;)
            GameObject.Destroy(defaultLEButton);

            // Change the button's label text.
            GameObject.Destroy(levelEditorUIButton.GetChildWithName("Label").GetComponent<UILocalize>());
            levelEditorUIButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Level Editor";

            // Change the button's on click action.
            GameObject.Destroy(levelEditorUIButton.GetComponent<ButtonController>());

            // So... I just realized if you add a class to a gameobject with also a UIButton, the button will automatically call a "OnClick" function inside of the class if it exists,
            // without adding it manually to the UIButton via code... good to know :)
            LE_UIButtonActionCtrl onClickClass = levelEditorUIButton.AddComponent<LE_UIButtonActionCtrl>();

            // Finally, enable the button.
            levelEditorUIButton.SetActive(true);
        }

        // And yes, this whole function is directly copied from the OST mod (almost), DON'T JUDGE ME.
        public void CreateLEMenuPanel()
        {
            // Get the Options menu and create a copy.
            GameObject originalOptionsMenu = GameObject.Find("MainMenu/Camera/Holder/Options");
            leMenuPanel = GameObject.Instantiate(originalOptionsMenu, originalOptionsMenu.transform.parent);

            // Change the name of the copy.
            leMenuPanel.name = "LE_Menu";

            // Remove the OptionsController and UILocalize components so I can change the title of the panel.
            GameObject.Destroy(leMenuPanel.GetComponent<OptionsController>());
            GameObject.Destroy(leMenuPanel.transform.GetChild(2).GetComponent<UILocalize>());

            // Change the title of the panel.
            leMenuPanel.transform.GetChild(2).GetComponent<UILabel>().text = "Level Editor";

            // Destroy the tabs and disable everything inside of the Game_Options object.
            GameObject.Destroy(leMenuPanel.GetChildWithName("Tabs"));
            leMenuPanel.GetChildWithName("Game_Options").SetActive(true);
            leMenuButtonsParent = leMenuPanel.GetChildAt("Game_Options/Buttons");
            leMenuButtonsParent.DisableAllChildren();

            // Disable the damn lines.
            leMenuPanel.GetChildAt("Game_Options/HorizontalLine").SetActive(false);
            leMenuPanel.GetChildAt("Game_Options/VerticalLine").SetActive(false);

            // Reset the scale of the new custom menu to one.
            leMenuPanel.transform.localScale = Vector3.one;

            // Add a UIPanel so the TweenScale can work.
            UIPanel panel = leMenuPanel.AddComponent<UIPanel>();
            leMenuPanel.GetComponent<TweenAlpha>().mRect = panel;

            leMenuPanel.GetChildWithName("Window").AddComponent<TweenAlpha>().duration = 0.2f;
        }

        public void CreateBackButton()
        {
            // Get the template, spawn the copy and set some parameters.
            GameObject template = leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            backButton = Instantiate(template, leMenuButtonsParent.transform);
            backButton.name = "BackButton";
            backButton.transform.localPosition = new Vector3(-690f, 290f, 0f);

            // Remove unnecesary components.
            GameObject.Destroy(backButton.GetComponent<ButtonController>());
            GameObject.Destroy(backButton.GetComponent<OptionsButton>());

            // Set the sprite width and height, and in the box collider as well.
            backButton.GetComponent<UISprite>().width = 250;
            backButton.GetComponent<UISprite>().height = 80;
            backButton.GetComponent<BoxCollider>().size = new Vector3(250, 80);

            // Destroy the FUCKING UILocalize component, I hate it.
            GameObject.Destroy(backButton.GetChildAt("Background/Label").GetComponent<UILocalize>());

            // Set the label data.
            UILabel label = backButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.SetAnchor((Transform)null);
            label.CheckAnchors();
            label.transform.localPosition = new Vector3(25f, 0f, 0f);
            label.width = 360;
            label.height = 67;
            label.text = "Back";
            label.fontSize = 35;

            // Set the in-button sprite data.
            UISprite sprite = new GameObject("Image").AddComponent<UISprite>();
            sprite.transform.parent = backButton.GetChildWithName("Background").transform;
            sprite.transform.localScale = Vector3.one;
            sprite.SetExternalSprite("BackArrow");
            sprite.color = new Color(0.6235f, 1f, 0.9843f, 1f);
            sprite.width = 20;
            sprite.height = 30;
            sprite.depth = 1;
            sprite.transform.localPosition = new Vector3(-45f, 3f, 0f);

            // Set OnClick action, which is go back lol.
            UIButton button = backButton.AddComponent<UIButton>();
            button.onClick.Add(new EventDelegate(this, nameof(LE_MenuUIManager.SwitchBetweenMenuAndLEMenu)));
        }

        // The same shit as the CreateBackButton function.
        public void CreateAddButton()
        {
            // Get the template, spawn the copy and set some parameters.
            GameObject template = leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            addButton = Instantiate(template, leMenuButtonsParent.transform);
            addButton.name = "AddButton";
            addButton.transform.localPosition = new Vector3(690f, 290f, 0f);

            // Remove unnecesary components.
            GameObject.Destroy(addButton.GetComponent<ButtonController>());
            GameObject.Destroy(addButton.GetComponent<OptionsButton>());

            // Set the sprite width and height, and in the box collider as well.
            addButton.GetComponent<UISprite>().width = 250;
            addButton.GetComponent<UISprite>().height = 80;
            addButton.GetComponent<BoxCollider>().size = new Vector3(250, 80);

            // Destroy the FUCKING UILocalize component, I hate it.
            GameObject.Destroy(addButton.GetChildAt("Background/Label").GetComponent<UILocalize>());

            // Set the label data.
            UILabel label = addButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.SetAnchor((Transform)null);
            label.CheckAnchors();
            label.transform.localPosition = new Vector3(25f, 0f, 0f);
            label.width = 360;
            label.height = 67;
            label.text = "Add";
            label.fontSize = 35;

            // Set the in-button sprite data.
            UISprite sprite = new GameObject("Image").AddComponent<UISprite>();
            sprite.transform.parent = addButton.GetChildWithName("Background").transform;
            sprite.transform.localScale = Vector3.one;
            sprite.SetExternalSprite("Plus");
            sprite.color = new Color(0.6235f, 1f, 0.9843f, 1f);
            sprite.width = 30;
            sprite.height = 30;
            sprite.depth = 1;
            sprite.transform.localPosition = new Vector3(-45f, 5f, 0f);

            // Set OnClick action, which is creating a new level with a new name.
            UIButton button = addButton.AddComponent<UIButton>();
            button.onClick.Add(new EventDelegate(this, nameof(LE_MenuUIManager.CreateNewLevel)));
        }

        void CreateNewLevel()
        {
            MelonCoroutines.Start(Init());

            IEnumerator Init()
            {
                // It seems even if you specify te fade to be 3 seconds long, the fade lasts less time, so I need to "split" the wait instruction.
                InGameUIManager.Instance.StartTotalFadeOut(3, true);
                yield return new WaitForSecondsRealtime(1.5f);

                SwitchBetweenMenuAndLEMenu();
                Melon<Core>.Instance.SetupTheWholeEditor();

                yield return new WaitForSecondsRealtime(1.5f);
                InGameUIManager.Instance.StartTotalFadeIn(3, true);
                EditorController.Instance.levelName = LevelData.GetAvailableLevelName();
            }
        }


        public void SwitchBetweenMenuAndLEMenu()
        {
            // Switch!
            inLEMenu = !inLEMenu;

            MelonCoroutines.Start(Animation());

            IEnumerator Animation()
            {
                if (inLEMenu)
                {
                    uiSoundSource.clip = okSound;
                    uiSoundSource.Play();

                    mainMenu.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(true);
                    leMenuPanel.SetActive(true);
                    leMenuPanel.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(false);
                    leMenuPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(false);
                    yield return new WaitForSecondsRealtime(0.2f);
                    mainMenu.SetActive(false);
                }
                else
                {
                    uiSoundSource.clip = hidePageSound;
                    uiSoundSource.Play();

                    mainMenu.SetActive(true);
                    mainMenu.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(false);
                    leMenuPanel.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(true);
                    leMenuPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(true);
                    yield return new WaitForSecondsRealtime(0.2f);
                    leMenuPanel.SetActive(false);
                }
            }
        }
    }
}
