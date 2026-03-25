using Il2CppInterop.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    internal static class AnimationUtils
    {
        delegate void AddClipDelegate(IntPtr animPtr, IntPtr clipPtr, ref ManagedSpanWrapper newName, int firstFrame, int lastFrame, bool addLoopFrame);
        static AddClipDelegate AddClipDelegate_Field;

        static AnimationUtils()
        {
            AddClipDelegate_Field = IL2CPP.ResolveICall<AddClipDelegate>("UnityEngine.Animation::AddClip_Injected");
        }

        public static void AddClipFixed(this Animation anim, AnimationClip clip, string newName, int firstFrame, int lastFrame, bool addLoopFrame)
        {
            if (!Utils.IsUnity6) // This fix is for Unity only.
            {
                anim.AddClip(clip, newName, firstFrame, lastFrame, addLoopFrame);
                return;
            }

            // Not use Il2CppObjectBaseToPtr, not Pointer, they point to the object with the IL2CPP wrapper/headers, we need the object data directly.
            IntPtr compPtr = anim.m_CachedPtr;
            IntPtr clipPtr = clip.m_CachedPtr;

            unsafe
            {
                fixed (char* newNamePtr = newName)
                {
                    ManagedSpanWrapper span = new ManagedSpanWrapper()
                    {
                        begin = newNamePtr,
                        length = newName.Length
                    };

                    AddClipDelegate_Field(compPtr, clipPtr, ref span, firstFrame, lastFrame, addLoopFrame);
                }
            }
        }

        public static void AddClipFixed(this Animation anim, AnimationClip clip, string newName)
        {
            AddClipFixed(anim, clip, newName, int.MinValue, int.MaxValue);
        }

        public static void AddClipFixed(this Animation anim, AnimationClip clip, string newName, int firstFrame, int lastFrame)
        {
            AddClipFixed(anim, clip, newName, firstFrame, lastFrame, addLoopFrame: false);
        }
    }
}