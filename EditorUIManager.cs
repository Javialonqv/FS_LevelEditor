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
        GameObject editorUIParent;

        List<GameObject> categoryButtons = new List<GameObject>();

        GameObject currentCategoryBG;
        List<GameObject> currentCategoryButtons = new List<GameObject>();

        void Start()
        {
            SetupEditorUI();
        }

        void SetupEditorUI()
        {
            SetupObjectsCategories();
            CreateObjectsBackground();
            SetupCurrentCategoryButtons();
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

        void SetupCurrentCategoryButtons()
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
                currentCategoryButton.GetComponent<UIButtonScale>().mScale = Vector3.one * 0.8f;

                currentCategoryButtons.Add(currentCategoryButton);
            }
        }
    }
}
