using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using System;

namespace AwA
{
    public class Core
    {
        public static void Title(string name)
        {
            EditorGUILayout.BeginVertical(GUI.skin.window, GUILayout.Height(25));

            // Title
            EditorGUILayout.LabelField(name, new GUIStyle(EditorStyles.boldLabel)
            { 
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fixedHeight = 17
            });

            // Author
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
            };
            if (GUILayout.Button("by AwA", style))
            {
                Application.OpenURL("https://x.com/awa_vrc");
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(20);
        }

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