using UnityEngine;

namespace FS_LevelEditor
{

    public class FixedUpdateProvider : MonoBehaviour
    {
        static FixedUpdateProvider _instance;

        public static Action OnFixedUpdate;

        public static void Init()
        {
            if (!_instance)
            {
                _instance = new GameObject("FixedUpdateProvider").AddComponent<FixedUpdateProvider>();
                DontDestroyOnLoad(_instance.gameObject);
            }
        }

        void FixedUpdate()
        {
            if (OnFixedUpdate != null)
                OnFixedUpdate.Invoke();
        }
    }
}
