using FS_LevelEditor.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    public static class MaterialUtils
    {
        static Material propsMat, propsTransMat;
        static Material propsNoSpecMat, propsTransNoSpecMat;
        static Material newPropsv1Mat, newPropsv1TransMat;

        static readonly Dictionary<(string name, Color matColor, Color emissionColor), Material> createdMaterialsWithColors = new();

        public static Material GetMaterialWithColor(Material original, Color matColor)
        {
            return GetMaterialWithColor(original, matColor, original.GetColor("_EmissionColor"));
        }
        public static Material GetMaterialWithColor(Material original, Color matColor, Color emissionColor)
        {
            string matName = original.name.Replace(" (Instance)", "");

            if (!createdMaterialsWithColors.TryGetValue((matName, matColor, emissionColor), out Material mat))
            {
                Material newMat = new Material(original);
                newMat.color = matColor;
                createdMaterialsWithColors.Add((matName, matColor, emissionColor), newMat);

                return newMat;
            }

            return mat;
        }
        public static void ResetMaterialWithColorsReferences()
        {
            createdMaterialsWithColors.Clear();
        }

        public static void LoadMaterials(Il2CppAssetBundle bundle)
        {
            propsMat = bundle.Load<Material>("Props_Mat");
            propsTransMat = bundle.Load<Material>("PropsTransparent_Mat");

            propsNoSpecMat = bundle.Load<Material>("Props_NoSpec");
            propsTransNoSpecMat = bundle.Load<Material>("PropsTransparent_NoSpec");

            newPropsv1Mat = bundle.Load<Material>("NewProps_v1");
            newPropsv1TransMat = bundle.Load<Material>("NewProps_v1_Transparent");
        }

        public static void SetTransparentMaterials(this GameObject gameObject)
        {
            foreach (var renderer in gameObject.TryGetComponents<MeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                        continue;

                    string matName = materials[i].name;
                    Material toAssign = null;

                    if (matName.Contains("Props_Mat"))
                        toAssign = propsTransMat;
                    else if (matName.Contains("Props_NoSpec"))
                        toAssign = propsTransNoSpecMat;
                    else if (matName.Contains("NewProps_v1_Light_")) { }
                        // Do nothing
                    else if (matName.Contains("NewProps_v1"))
                        toAssign = newPropsv1TransMat;

                    if (toAssign)
                    {
                        toAssign.color = new Color(toAssign.color.r, toAssign.color.g, toAssign.color.b, 0.392f);
                        materials[i] = toAssign;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }
        public static void SetOpaqueMaterials(this GameObject gameObject)
        {
            foreach (var renderer in gameObject.TryGetComponents<MeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                        continue;

                    string matName = materials[i].name;
                    Material toAssign = null;

                    if (matName.Contains("PropsTransparent_Mat"))
                        toAssign = propsMat;
                    else if (matName.Contains("PropsTransparent_NoSpec"))
                        toAssign = propsNoSpecMat;
                    else if (matName.Contains("NewProps_v1_Transparent"))
                        toAssign = newPropsv1Mat;

                    if (toAssign)
                    {
                        toAssign.color = new Color(toAssign.color.r, toAssign.color.g, toAssign.color.b, 0.392f);
                        materials[i] = toAssign;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }

        public static void SetAllTransparent()
        {
            foreach (var obj in EditorController.Instance.currentInstantiatedObjects)
            {
                obj.gameObject.SetTransparentMaterials();
            }
        }
        public static void SetAllOpaque()
        {
            foreach (var obj in EditorController.Instance.currentInstantiatedObjects)
            {
                obj.gameObject.SetOpaqueMaterials();
            }
        }
    }
}
