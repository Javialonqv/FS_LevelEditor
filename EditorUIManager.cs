using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2Cpp;
using Il2CppVLB;
using MelonLoader;
using UnityEngine;
using System.Reflection;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class EditorUIManager : MonoBehaviour
    {
        public static EditorUIManager Instance;

        GameObject editorUIParent;

        public List<GameObject> categoryButtons = new List<GameObject>();

        public GameObject currentCategoryBG;
        List<GameObject> currentCategoryButtons = new List<GameObject>();

        GameObject selectedObjPanel;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            SetupEditorUI();
        }

        void SetupEditorUI()
        {
            SetupObjectsCategories();
            CreateObjectsBackground();
            SetupCurrentCategoryButtons();
            CreateSelectedObjPanel();
        }


        void SetupObjectsCategories()
        {
            editorUIParent = new GameObject("LevelEditor");
            editorUIParent.transform.parent = GameObject.Find("MainMenu/Camera/Holder").transform;
            editorUIParent.transform.localScale = Vector3.one;

            GameObject buttonTemplate = GameObject.Find("MainMenu/Camera/Holder/TaserCustomization/Holder/Tabs/1_Taser");

            for (int i = 0; i < EditorController.Instance.categories.Count; i++)
            {
                string category = EditorController.Instance.categories[i];

                GameObject categoryButton = Instantiate(buttonTemplate, editorUIParent.transform);
                categoryButton.name = $"{category}_Button";
                categoryButton.transform.localPosition = new Vector3(-800f + (250f * i), 450f, 0f);
                Destroy(categoryButton.GetChildWithName("Label").GetComponent<UILocalize>());
                categoryButton.GetChildWithName("Label").GetComponent<UILabel>().text = category;

                categoryButton.GetComponent<UIToggle>().onChange.Clear();
                categoryButton.GetComponent<UIToggle>().Set(false);

                EventDelegate onChange = new EventDelegate(EditorController.Instance, "ChangeCategory");
                EventDelegate.Parameter parameter = new EventDelegate.Parameter
                {
                    field = "categoryID",
                    value = i,
                    obj = EditorController.Instance
                };
                onChange.mParameters = new EventDelegate.Parameter[] { parameter };
                categoryButton.GetComponent<UIToggle>().onChange.Add(onChange);

                categoryButtons.Add(categoryButton);
            }

            categoryButtons[EditorController.Instance.currentCategoryID].GetComponent<UIToggle>().Set(true);
        }

        void CreateObjectsBackground()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Background");

            currentCategoryBG = new GameObject("CategoryObjectsBackground");
            currentCategoryBG.transform.parent = editorUIParent.transform;
            currentCategoryBG.transform.localPosition = new Vector3(0f, 330f, 0f);
            currentCategoryBG.transform.localScale = Vector3.one;

            UISprite bgSprite = currentCategoryBG.AddComponent<UISprite>();
            bgSprite.atlas = template.GetComponent<UISprite>().atlas;
            bgSprite.spriteName = "Square_Border_Beveled_HighOpacity";
            bgSprite.type = UIBasicSprite.Type.Sliced;
            bgSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            bgSprite.width = 1800;
            bgSprite.height = 150;
        }

        public void SetupCurrentCategoryButtons()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/TaserCustomization/Holder/ColorSelection/ColorSwatch");

            currentCategoryButtons.Clear();
            currentCategoryBG.DeleteAllChildren();

            for (int i = 0; i < EditorController.Instance.allCategoriesObjects[EditorController.Instance.currentCategoryID].Count; i++)
            {
                var currentCategoryObj = EditorController.Instance.allCategoriesObjects[EditorController.Instance.currentCategoryID].ToList()[i];

                GameObject currentCategoryButton = Instantiate(template, currentCategoryBG.transform);
                currentCategoryButton.name = currentCategoryObj.Key;
                currentCategoryButton.transform.localPosition = new Vector3(-800 + (150f * i), -25f, 0f);
                currentCategoryButton.transform.localScale = Vector3.one * 0.8f;
                currentCategoryButton.GetChildWithName("ActiveSwatch").SetActive(false);
                currentCategoryButton.GetChildWithName("ColorSample").SetActive(false);
                currentCategoryButton.SetActive(true);

                currentCategoryButton.GetChildWithName("ColorName").GetComponent<UILabel>().text = currentCategoryObj.Key;
                currentCategoryButton.GetComponent<UIButton>().onClick.Clear();

                EventDelegate onChange = new EventDelegate(EditorController.Instance, "SelectObjectToBuild");
                EventDelegate.Parameter parameter = new EventDelegate.Parameter
                {
                    field = "objName",
                    value = currentCategoryObj.Key,
                    obj = EditorController.Instance
                };
                onChange.mParameters = new EventDelegate.Parameter[] { parameter };
                currentCategoryButton.GetComponent<UIButton>().onClick.Add(onChange);

                currentCategoryButton.GetComponent<UIButtonScale>().mScale = Vector3.one * 0.8f;

                currentCategoryButtons.Add(currentCategoryButton);
            }
        }

        public void CreateSelectedObjPanel()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Background");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            selectedObjPanel = new GameObject("CurrentSelectedObjPanel");
            selectedObjPanel.transform.parent = editorUIParent.transform;
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -220f, 0f);
            selectedObjPanel.transform.localScale = Vector3.one;

            UISprite headerSprite = selectedObjPanel.AddComponent<UISprite>();
            headerSprite.atlas = template.GetComponent<UISprite>().atlas;
            headerSprite.spriteName = "Square_Border_Beveled_HighOpacity";
            headerSprite.type = UIBasicSprite.Type.Sliced;
            headerSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            headerSprite.width = 520;
            headerSprite.height = 60;

            GameObject headerText = new GameObject("Label");
            headerText.transform.parent = selectedObjPanel.transform;
            headerText.transform.localPosition = Vector3.zero;
            headerText.transform.localScale = Vector3.one;

            UILabel headerLabel = headerText.AddComponent<UILabel>();
            headerLabel.font = labelTemplate.GetComponent<UILabel>().font;
            headerLabel.fontSize = 27;
            headerLabel.text = "No Object Selected";
            headerLabel.depth = 1;
            headerLabel.width = 520;
            headerLabel.height = 60;

            GameObject selectedObjPanelBody = new GameObject("Body");
            selectedObjPanelBody.transform.parent = selectedObjPanel.transform;
            selectedObjPanelBody.transform.localPosition = new Vector3(0f, -160f, 0f);
            selectedObjPanelBody.transform.localScale = Vector3.one;

            UISprite bodySprite = selectedObjPanelBody.AddComponent<UISprite>();
            bodySprite.atlas = template.GetComponent<UISprite>().atlas;
            bodySprite.spriteName = "Square_Border_Beveled_HighOpacity";
            bodySprite.type = UIBasicSprite.Type.Sliced;
            bodySprite.color = new Color(0.0039f, 0.3568f, 0.3647f, 1f);
            bodySprite.depth = -1;
            bodySprite.width = 500;
            bodySprite.height = 300;

            SetSelectedObjPanelAsNone();
            CreateNoAttributesPanel();
        }

        void CreateNoAttributesPanel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject noAttributes = new GameObject("NoAttributes");
            noAttributes.transform.parent = selectedObjPanel.GetChildWithName("Body").transform;
            noAttributes.transform.localPosition = Vector3.zero;
            noAttributes.transform.localScale = Vector3.one;

            UILabel label = noAttributes.AddComponent<UILabel>();
            label.font = labelTemplate.GetComponent<UILabel>().font;
            label.fontSize = 27;
            label.width = 500;
            label.height = 200;
            label.text = "No Attributes for this object.";
        }

        public void SetSelectedObjPanelAsNone()
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = "No Object Selected";
            selectedObjPanel.GetChildWithName("Body").SetActive(false);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -505f, 0f);
        }
        public void SetSelectedObject(string objName)
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = objName;
            selectedObjPanel.GetChildWithName("Body").SetActive(true);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -220, 0f);
        }
    }
}
