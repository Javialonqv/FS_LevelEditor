using UnityEngine;

namespace FS_LevelEditor
{

    public class LE_Collider : LE_Object
    {
        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                gameObject.GetChildAt("Content/Mesh").SetActive(false);
            }

            base.OnInstantiated(scene);
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(0f, 1f, 0.07843138f);
        }
    }
}
