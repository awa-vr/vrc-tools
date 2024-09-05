using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;

namespace AwA
{
    public class Core
    {
        public static AnimatorController GetFXController(VRCAvatarDescriptor avatar)
        {
            if (avatar.baseAnimationLayers[4].animatorController != null)
                return (AnimatorController)avatar.baseAnimationLayers[4].animatorController;
            else
                return null;
        }

        public static VRCAvatarDescriptor[] GetAvatarsInScene()
        {
            var avatar = SceneAsset.FindObjectsOfType<VRCAvatarDescriptor>();

            return avatar;
        }
    }
}