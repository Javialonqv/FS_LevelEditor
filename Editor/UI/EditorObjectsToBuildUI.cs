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
    public class EditorObjectsToBuildUI : MonoBehaviour
    {
        public static EditorObjectsToBuildUI Instance;

        public GameObject root;

        public GameObject categoryButtonsParent;
        float currentCategoryButtonXPos, currentCategoryButtonYPos;
        bool categoryButtonsAreHidden = false;

        // For the objects to build buttons.
        public GameObject objectsToBuildMainParent;
        List<GameObject> objectsToBuildParentsByCategories = new();
        List<List<GameObject>> objectsToBuildGrids = new();

        UIButtonPatcher previousGridButton, nextGridButton;

        int currentCategoryID;
        int currentGridID;

        public static void Create(Transform editorUIParent)
        {
            GameObject root = new GameObject("ObjectsToBuildUI");
            root.transform.parent = editorUIParent;
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one;

            root.AddComponent<EditorObjectsToBuildUI>();
        }

        void Awake()
        {
            Instance = this;
            root = gameObject;

            CreateObjectsCategories();
            CreateObjectsBackground();
            CreateALLOfTheObjectsButtons();

            CreatePreviousGridButton();
            CreateNextGridButton();
        }

        void Start()
        {
            Invoke("ForceEnableFirstCategory", 0.1f);
        }

        #region Create UI
        void ForceEnableFirstCategory()
        {
            // For some fucking reason the code enables the content in the SECOND category, I need to force it... damn it.
            EditorController.Instance.ChangeCategory(0);
            ChangeCategory(0);
            SelectObjToBuild(0);
        }

        void CreateObjectsCategories()
        {
            // Setup the category buttons parent and add a panel to it so I can modify the alpha of the whole buttons inside of it with just one panel.
            categoryButtonsParent = new GameObject("CategoryButtons");
            categoryButtonsParent.transform.parent = transform;
            categoryButtonsParent.transform.localPosition = Vector3.zero;
            categoryButtonsParent.transform.localScale = Vector3.one;
            categoryButtonsParent.layer = LayerMask.NameToLayer("2D GUI");
            categoryButtonsParent.AddComponent<UIPanel>();

            currentCategoryButtonXPos = -800f;
            currentCategoryButtonYPos = 450f;
            for (int i = 0; i < EditorController.Instance.categoriesNames.Count; i++)
            {
                string category = EditorController.Instance.categoriesNames[i];
                Vector3 buttonPosition = new Vector3(currentCategoryButtonXPos, currentCategoryButtonYPos, 0f);

                UITogglePatcher categoryButton = NGUI_Utils.CreateTabToggle(categoryButtonsParent.transform, buttonPosition, category);
                categoryButton.name = $"{category}_Button";
                // The toggle is set to false by default.

                // It seems it's a bug, I need to create a copy of 'i'. Otherwise ALL of the toggles will end using the same value.
                int index = i;
                categoryButton.onClick += () => EditorController.Instance.ChangeCategory(index);
                categoryButton.onClick += () => ChangeCategory(index);

                currentCategoryButtonXPos += 250f;
                if (currentCategoryButtonXPos >= 700f)
                {
                    currentCategoryButtonXPos = -800f;
                    currentCategoryButtonYPos -= 75f;
                }
            }

            categoryButtonsParent.transform.GetChild(0).GetComponent<UITogglePatcher>().toggle.Set(true);
        }

        void CreateObjectsBackground()
        {
            objectsToBuildMainParent = new GameObject("CategoryObjectsButtons");
            objectsToBuildMainParent.transform.parent = transform;
            objectsToBuildMainParent.transform.localPosition = new Vector3(0f, 330f, 0f);
            objectsToBuildMainParent.transform.localScale = Vector3.one;
            objectsToBuildMainParent.layer = LayerMask.NameToLayer("2D GUI");
            objectsToBuildMainParent.AddComponent<UIPanel>();

            UISprite bgSprite = objectsToBuildMainParent.AddComponent<UISprite>();
            bgSprite.atlas = NGUI_Utils.UITexturesAtlas;
            bgSprite.spriteName = "Square_Border_Beveled_HighOpacity";
            bgSprite.type = UIBasicSprite.Type.Sliced;
            bgSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            bgSprite.width = 1850;
            bgSprite.height = 150;

            BoxCollider collider = objectsToBuildMainParent.AddComponent<BoxCollider>();
            collider.size = new Vector3(1800f, 150f, 1f);
        }
        void CreateALLOfTheObjectsButtons()
        {
            for (int i = 0; i < EditorController.Instance.categoriesNames.Count; i++)
            {
                GameObject createdButtonsParent = CreateObjectsForCategory(i);

                // Only enable the very first category.
                createdButtonsParent.SetActive(i == 0);
            }
        }
        GameObject CreateObjectsForCategory(int categoryID)
        {
            GameObject categoryObjectsBtnParent = new GameObject(EditorController.Instance.categoriesNames[categoryID]);
            categoryObjectsBtnParent.transform.parent = objectsToBuildMainParent.transform;
            categoryObjectsBtnParent.transform.localPosition = Vector3.zero;
            categoryObjectsBtnParent.transform.localScale = Vector3.one;

            List<GameObject> grids = new();
            Transform currentGrid = null;
            UITable currentGridTable = null;
            for (int i = 0; i < EditorController.Instance.allCategoriesObjectsSorted[categoryID].Count; i++)
            {
                // Create a new grid.
                if (i % 12 == 0 || i == 0)
                {
                    currentGrid = new GameObject("Grid " + i).transform;
                    currentGrid.parent = categoryObjectsBtnParent.transform;
                    currentGrid.localScale = Vector3.one;
                    currentGrid.gameObject.SetActive(i == 0); // Only enable the first grid by default.

                    currentGridTable = currentGrid.gameObject.AddComponent<UITable>();
                    currentGridTable.cellAlignment = UIWidget.Pivot.Center;
                    currentGridTable.pivot = UIWidget.Pivot.Left;
                    currentGridTable.padding = new Vector2(8f, 12.04f);

                    currentGrid.localPosition = new Vector3(-870f, 0f, 0f);

                    grids.Add(currentGrid.gameObject);
                }

                // Get the object.
                var objectInfo = EditorController.Instance.allCategoriesObjectsSorted[categoryID].ToList()[i];

                var button = NGUI_Utils.CreateColorButton(currentGrid, Vector3.zero, objectInfo.Key);
                button.name = objectInfo.Key;

                button.onClick += () => EditorController.Instance.SelectObjectToBuild(objectInfo.Key);
                int index = i;
                button.onClick += () => SelectObjToBuild(index % 12);

                button.transform.localScale = Vector3.one * 0.8f;
                button.GetComponent<UIButtonScale>().mScale = Vector3.one * 0.8f;

                if (i % 12 == 0 || i == 0) currentGridTable.Reposition(); // Reposition if in this iteration we created a grid.
            }

            objectsToBuildParentsByCategories.Add(categoryObjectsBtnParent);
            objectsToBuildGrids.Add(grids);

            return categoryObjectsBtnParent;
        }

        void CreatePreviousGridButton()
        {
            previousGridButton = NGUI_Utils.CreateButton(objectsToBuildMainParent.transform, new Vector3(-892, 0), new Vector3Int(50, 128, 0), "<");
            previousGridButton.gameObject.RemoveComponent<UIButtonScale>();
            previousGridButton.buttonSprite.depth = 1;
            previousGridButton.onClick += PreviousGridPage;
        }
        void CreateNextGridButton()
        {
            nextGridButton = NGUI_Utils.CreateButton(objectsToBuildMainParent.transform, new Vector3(892, 0), new Vector3Int(50, 128, 0), ">");
            nextGridButton.gameObject.RemoveComponent<UIButtonScale>();
            nextGridButton.buttonSprite.depth = 1;
            nextGridButton.onClick += NextGridPage;
        }
        #endregion


        public void ChangeCategory(int categoryID)
        {
            currentCategoryID = categoryID;

            foreach (var parent in objectsToBuildParentsByCategories)
            {
                parent.SetActive(false);
            }

            objectsToBuildParentsByCategories[categoryID].SetActive(true);

            SetCurrentSelectedCategoryGrid(0);

            // Select the very first element on the objects list on the very first grid.
            GameObject firstGridOfNewCurrentCategory = objectsToBuildGrids[categoryID][0];
            UIButtonPatcher firstButtonInGrid = firstGridOfNewCurrentCategory.transform.GetChild(0).GetComponent<UIButtonPatcher>();
            firstButtonInGrid.OnClick();

            //objectsToBuildParentsByCategories[categoryID].transform.GetChild(0).GetChild(0).GetComponent<UIButton>().OnClick();
            //UICamera.Notify(objectsToBuildParentsByCategories[categoryID].transform.GetChild(0).GetChild(0).gameObject, "OnClick", null);
        }
        public void SelectObjToBuild(int buttonID)
        {
            // Disable the "selected" obj in the other buttons.
            foreach (var grid in objectsToBuildParentsByCategories[currentCategoryID].GetChilds())
            {
                foreach (var button in grid.GetChilds())
                {
                    button.GetChildWithName("ActiveSwatch").SetActive(false);
                }
            }

            GameObject currentGrid = objectsToBuildGrids[currentCategoryID][currentGridID];
            GameObject newSelectedButton = currentGrid.transform.GetChild(buttonID).gameObject;
            newSelectedButton.GetChildWithName("ActiveSwatch").SetActive(true);
        }

        void PreviousGridPage()
        {
            if (currentGridID > 0)
            {
                SetCurrentSelectedCategoryGrid(currentGridID - 1);
            }
        }
        void NextGridPage()
        {
            if (currentGridID < objectsToBuildGrids[currentCategoryID].Count - 1)
            {
                SetCurrentSelectedCategoryGrid(currentGridID + 1);
            }
        }
        void SetCurrentSelectedCategoryGrid(int gridIndex)
        {
            currentGridID = gridIndex;

            objectsToBuildGrids[currentCategoryID].ForEach(grid => grid.SetActive(false));
            objectsToBuildGrids[currentCategoryID][gridIndex].SetActive(true);

            //objectsToBuildParentsByCategories[currentCategoryID].DisableAllChildren();
            //objectsToBuildParentsByCategories[currentCategoryID].transform.GetChild(gridIndex).gameObject.SetActive(true);

            SelectObjToBuild(0);
            UpdatePreviousAndNextGridButtonsState();
        }
        void UpdatePreviousAndNextGridButtonsState()
        {
            if (objectsToBuildGrids[currentCategoryID].Count == 1)
            {
                previousGridButton.gameObject.SetActive(false);
                nextGridButton.gameObject.SetActive(false);
            }
            else if (objectsToBuildGrids[currentCategoryID].Count > 1)
            {
                previousGridButton.gameObject.SetActive(true);
                nextGridButton.gameObject.SetActive(true);

                previousGridButton.button.isEnabled = currentGridID > 0;
                nextGridButton.button.isEnabled = currentGridID < objectsToBuildGrids[currentCategoryID].Count - 1;
            }
        }

        public void HideOrShowCategoryButtons()
        {
            categoryButtonsAreHidden = !categoryButtonsAreHidden;

            if (categoryButtonsAreHidden)
            {
                TweenAlpha.Begin(categoryButtonsParent, 0.2f, 0f);
                TweenPosition.Begin(objectsToBuildMainParent, 0.2f, new Vector3(0f, 410f, 0f));
                InGameUIManager.Instance.m_uiAudioSource.PlayOneShot(InGameUIManager.Instance.hideHUDSound);
            }
            else
            {
                TweenAlpha.Begin(categoryButtonsParent, 0.2f, 1f);
                TweenPosition.Begin(objectsToBuildMainParent, 0.2f, new Vector3(0f, 330f, 0f));
                InGameUIManager.Instance.m_uiAudioSource.PlayOneShot(InGameUIManager.Instance.showHUDSound);
            }
        }
    }
}
