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
        public static Material propsMat, propsTransMat;
        public static Material propsNoSpecMat, propsTransNoSpecMat;
        public static Material newPropsv1Mat, newPropsv1TransMat;
        public static Material newPropsv2Mat, newPropsv2TransMat;
        public static Material newPropsv3Mat, newPropsv3TransMat;
        public static Material propsXMASLitMat, propsXMASLitTransMat;
        public static Material propsXMASUnlitMat, propsXMASUnlitTransMat;

        static readonly Dictionary<(string name, Color matColor, Color emissionColor), Material> createdMaterialsWithColors = new();

        public static Material GetMaterialWithColor(Material original, Color matColor)
        {
            return GetMaterialWithColor(original, matColor, original.HasColor("_EmissionColor") ? original.GetColor("_EmissionColor") : Color.white);
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

            newPropsv2Mat = bundle.Load<Material>("NewProps_v2");
            newPropsv2TransMat = bundle.Load<Material>("NewProps_v2_Transparent");

            newPropsv3Mat = bundle.Load<Material>("NewProps_v3");
            newPropsv3TransMat = bundle.Load<Material>("NewProps_v3_Transparent");

            propsXMASLitMat = bundle.Load<Material>("Props_XMAS_Lit");
            propsXMASLitTransMat = bundle.Load<Material>("Props_XMAS_Lit_Transparent");

            propsXMASUnlitMat = bundle.Load<Material>("Props_XMAS_Unlit");
            propsXMASUnlitTransMat = bundle.Load<Material>("Props_XMAS_Unlit_Transparent");
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
                    else if (matName.Contains("NewProps_v2"))
                        toAssign = newPropsv2TransMat;
                    else if (matName.Contains("NewProps_v3"))
                        toAssign = newPropsv3TransMat;
                    else if (matName.Contains("Props_XMAS_Lit"))
                        toAssign = propsXMASLitTransMat;
                    else if (matName.Contains("Props_XMAS_Unlit"))
                        toAssign = propsXMASUnlitTransMat;

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
                    else if (matName.Contains("NewProps_v2_Transparent"))
                        toAssign = newPropsv2Mat;
                    else if (matName.Contains("NewProps_v3_Transparent"))
                        toAssign = newPropsv3Mat;
                    else if (matName.Contains("Props_XMAS_Lit_Transparent"))
                        toAssign = propsXMASLitMat;
                    else if (matName.Contains("Props_XMAS_Unlit_Transparent"))
                        toAssign = propsXMASUnlitMat;

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
