using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Linq;

namespace AwA
{
    public class Parameter
    {
        public string Name; // Name of the parameter (in the FX controller and expression parameters)
        public List<VRCExpressionsMenu> Menus; // Menus that the paramater is in
        public string ReccomendedName; // Reccomended name of the parameter (first one found in the menus)
    }

    public class RenameParam : EditorWindow
    {
        #region Vars
        VRCAvatarDescriptor avatar;
        AnimatorController fx;
        AnimatorControllerParameter[] animatorParameters;
        VRCExpressionParameters vrcParameters;
        VRCExpressionsMenu vrcMainMenu;
        List<VRCExpressionsMenu> vrcMenus;
        List<Parameter> parameters = new List<Parameter>();

        // List of parameters to exclude
        // Default of VRC (https://creators.vrchat.com/avatars/animator-parameters/)
        // TODO: Move this to settings
        List<string> exclude = new List<string>() {
            "IsLocal",
            "Viseme",
            "Voice",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "VelocityMagnitude",
            "Upright",
            "Grounded",
            "Steated",
            "AFK",
            "Expression1",
            "Expression1",
            "Expression2",
            "Expression3",
            "Expression4",
            "Expression5",
            "Expression6",
            "Expression7",
            "Expression8",
            "Expression9",
            "Expression10",
            "Expression11",
            "Expression12",
            "Expression13",
            "Expression14",
            "Expression15",
            "Expression16",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation",
            "Earmuffs",
            "IsOnFriendsList",
            "AvatarVersion"
        };

        string oldParameterName;
        string newParameterName;

        Vector2 scrollPos;
        bool shouldRefresh = true;
        bool showParamsWithNoMenus = false;
        bool showParamsWithSameName = true;
        bool includeMenuName = true;
        #endregion

        [MenuItem("Tools/AwA/Rename Paramater")]
        public static void ShowWindow()
        {
            var window = GetWindow<RenameParam>("Rename Paramater");
            window.titleContent = new GUIContent(image: EditorGUIUtility.IconContent("d_editicon.sml").image, text: "Rename Paramater", tooltip: "Rename a parameter in the FX controller as well as the expression parameter, and all menus (if any)");
        }

        private void OnEnable()
        {
            var avatars = Core.GetAvatarsInScene();
            if (avatars.Length == 1)
            {
                avatar = avatars[0];
                Refresh();
            }
            else
            {
                Debug.LogError("No avatar descriptors found in the scene");
                EditorGUILayout.HelpBox("No avatar descriptors found in the scene", MessageType.Error);
            }
        }

        public void OnGUI()
        {
            Core.Title("Rename Paramater");

            avatar = EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;

            if (avatar == null)
            {
                EditorGUILayout.HelpBox("Please Select Avatar", MessageType.Error);
                return;
            }
            else
            {
                // Set vars
                bool success = SetVars();
                if (!success)
                    return;

                // Get all submenus
                GetSubMenus(vrcMainMenu, vrcMenus);

                // Refresh if needed
                if (shouldRefresh)
                {
                    shouldRefresh = false;
                    Refresh();
                }

                // Begin Main UI
                EditorGUILayout.HelpBox("Some parameters Should not be renamed! Be careful!", MessageType.Warning);
                bool sameName = parameters.Any(x => parameters.Count(y => y.ReccomendedName == x.ReccomendedName) > 1);
                if (sameName)
                {
                    EditorGUILayout.HelpBox("Some parameters have the same recommended name! Be extra careful!", MessageType.Error);
                }

                showParamsWithNoMenus = EditorGUILayout.ToggleLeft("Show parameters with no menus", showParamsWithNoMenus);
                showParamsWithSameName = EditorGUILayout.ToggleLeft("Show parameters with same name", showParamsWithSameName);
                includeMenuName = EditorGUILayout.ToggleLeft("Include menu name in reccomended name", includeMenuName);

                if (GUILayout.Button("Refresh"))
                {
                    shouldRefresh = true;
                }

                // Header
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Menu", GUILayout.Width(50));
                EditorGUILayout.LabelField("Parameter");
                EditorGUILayout.LabelField("", GUILayout.Width(20));
                EditorGUILayout.LabelField("Reccomended Name");
                EditorGUILayout.LabelField("", GUILayout.Width(80));

                EditorGUILayout.EndHorizontal();

                scrollPos = GUILayout.BeginScrollView(scrollPos);

                // Show parameters
                foreach (var param in parameters)
                {
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Find", GUILayout.Width(50)))
                    {
                        if (param.Menus.Count > 0)
                        {
                            Selection.activeObject = param.Menus[0];
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("No menus found", "No menus found with a parameter named '" + param.Name + "'.", "OK");
                        }
                    }

                    EditorGUILayout.LabelField(param.Name);
                    if (parameters.Find(p => p != param && p.ReccomendedName == param.ReccomendedName) != null)
                    {
                        GUILayout.Label(EditorGUIUtility.IconContent("console.erroricon.sml"), GUILayout.Width(20));
                    }
                    else
                    {
                        GUILayout.Space(20);
                    }
                    EditorGUILayout.LabelField("->", GUILayout.Width(20));
                    param.ReccomendedName = EditorGUILayout.TextField(param.ReccomendedName);

                    if (GUILayout.Button("Rename", GUILayout.Width(80)))
                    {
                        var newName = param.ReccomendedName;
                        Debug.Log(newName);
                        var renameOK = EditorUtility.DisplayDialog("Rename Parameter", "Are you sure you want to rename '" + param.Name + "' to '" + newName + "'?\nMenus containing the parameter are: " + string.Join(", ", param.Menus.Select(x => x.name)), "Yes", "No");
                        if (renameOK)
                        {
                            // TODO: Fix undo (if possible???)
                            // Rename parameter in FX controller
                            Object[] undoObjects = new Object[] { fx, vrcParameters };
                            foreach (var menu in param.Menus)
                            {
                                undoObjects = undoObjects.Concat(new Object[] { menu }).ToArray();
                            }
                            Undo.RecordObjects(undoObjects, "Rename Parameter");
                            oldParameterName = param.Name;
                            newParameterName = newName;
                            RenameParameter(param.Name, newName);

                            // Rename vrc parameter
                            vrcParameters.parameters.FirstOrDefault(x => x.name == param.Name).name = newName;
                            EditorUtility.SetDirty(vrcParameters);

                            // Rename menus
                            foreach (var menu in param.Menus)
                            {
                                foreach (var control in menu.controls)
                                {
                                    if (control.parameter.name == param.Name)
                                    {
                                        control.parameter.name = newName;
                                    }
                                    else if (control.subParameters.Any(x => x.name == param.Name))
                                    {
                                        control.subParameters.FirstOrDefault(x => x.name == param.Name).name = newName;
                                    }
                                }

                                // Set menu as dirty
                                EditorUtility.SetDirty(menu);
                            }

                            // Refresh();
                            shouldRefresh = true;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }
        }

        /// <summary>
        ///     Sets all needed variables
        /// </summary>
        /// <returns>If success</returns>
        bool SetVars()
        {
            fx = Core.GetFXController(avatar);
            if (fx == null)
            {
                EditorGUILayout.HelpBox("No FX layer found!", MessageType.Error);
                return false;
            }

            animatorParameters = fx.parameters;
            if (animatorParameters.Length == 0)
            {
                EditorGUILayout.HelpBox("No parameters found in the FX controller!", MessageType.Error);
                return false;
            }

            vrcParameters = avatar.expressionParameters;
            if (vrcParameters == null)
            {
                EditorGUILayout.HelpBox("No parameters found in the avatar expression parameters!", MessageType.Error);
                return false;
            }

            vrcMainMenu = avatar.expressionsMenu;
            if (vrcMainMenu == null)
            {
                EditorGUILayout.HelpBox("No main menu found in the avatar descriptor!", MessageType.Error);
                return false;
            }

            vrcMenus = new List<VRCExpressionsMenu>
            {
                vrcMainMenu
            };

            return true;
        }

        void Refresh()
        {
            parameters.Clear();

            // Add parameters to list and get the menus each one is in
            foreach (var param in animatorParameters)
            {
                Parameter p = new Parameter();
                p.Name = param.name;
                p.Menus = new List<VRCExpressionsMenu>();
                foreach (var menu in vrcMenus)
                {
                    if (menu.controls.Any(x => x.parameter.name == param.name || x.subParameters.Any(x => x.name == param.name)))
                    {
                        p.Menus.Add(menu);
                    }
                }

                // Set reccomended name
                if (p.Menus.Count > 0)
                {
                    foreach (var control in p.Menus[0].controls)
                    {
                        if (control.parameter.name == p.Name || control.subParameters.Any(x => x.name == p.Name))
                        {
                            if (p.Name.Contains(p.Menus[0].name) || !includeMenuName)
                                p.ReccomendedName = GetSubstringBeforeLastSlash(p.Name) + control.name;
                            else
                                p.ReccomendedName = GetSubstringBeforeLastSlash(p.Name) + p.Menus[0].name + "/" + control.name;
                        }
                    }
                }
                else
                {
                    p.ReccomendedName = p.Name;
                }

                parameters.Add(p);
            }

            // Remove excluded parameters
            parameters.RemoveAll(x => exclude.Contains(x.Name) || exclude.Contains(x.ReccomendedName));

            // Remove parameters with no menus
            if (!showParamsWithNoMenus)
                parameters.RemoveAll(x => x.Menus.Count == 0);

            // Remove parameters with same name
            if (!showParamsWithSameName)
                parameters.RemoveAll(x => x.Name == x.ReccomendedName);
        }

        void GetSubMenus(VRCExpressionsMenu menu, List<VRCExpressionsMenu> menus)
        {
            if (menu == null) return;

            foreach (var control in menu.controls)
            {
                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                {
                    var submenu = (VRCExpressionsMenu)control.subMenu;
                    if (submenu != null)
                    {
                        menus.Add(submenu);
                        GetSubMenus(submenu, menus);
                    }
                }
            }
        }

        static string GetSubstringBeforeLastSlash(string input)
        {
            int index = input.LastIndexOf('/'); // Find the index of the last '/'

            if (index == -1) // If no '/' is found, return the original string
            {
                return input;
            }

            return input.Substring(0, index + 1); // Get the substring before the last '/' (including the '/')
        }

        #region Magic

        private void RenameParameter(string oldParameterName, string newParameterName)
        {
            // Start by renaming the parameter
            bool parameterRenamed = RenameParameterInAnimator();

            if (parameterRenamed)
            {
                // Update all references throughout the Animator Controller
                UpdateAllReferences();
                Debug.Log($"Parameter '{oldParameterName}' successfully renamed to '{newParameterName}' and all references updated.");
            }
            else
            {
                Debug.LogError($"Parameter '{oldParameterName}' not found in the Animator Controller.");
            }
        }

        private bool RenameParameterInAnimator()
        {
            SerializedObject serializedController = new SerializedObject(fx);
            SerializedProperty parameters = serializedController.FindProperty("m_AnimatorParameters");
            bool found = false;

            for (int i = 0; i < parameters.arraySize; i++)
            {
                SerializedProperty parameter = parameters.GetArrayElementAtIndex(i);
                SerializedProperty nameProperty = parameter.FindPropertyRelative("m_Name");

                if (nameProperty.stringValue == oldParameterName)
                {
                    Undo.RecordObject(fx, "Rename Animator Parameter");
                    nameProperty.stringValue = newParameterName;
                    serializedController.ApplyModifiedProperties();
                    EditorUtility.SetDirty(fx);
                    found = true;
                    break;
                }
            }
            return found;
        }

        private void UpdateAllReferences()
        {
            foreach (var layer in fx.layers)
            {
                UpdateStateMachine(layer.stateMachine);
            }
        }

        private void UpdateStateMachine(AnimatorStateMachine stateMachine)
        {
            foreach (var state in stateMachine.states)
            {
                UpdateStateTransitions(state.state);
                UpdateBlendTreeReferences(state.state.motion);
            }

            for (int i = 0; i < stateMachine.anyStateTransitions.Length; i++)
            {
                UpdateTransitionConditions(stateMachine.anyStateTransitions[i]);
            }

            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                UpdateStateMachine(childStateMachine.stateMachine);
            }
        }

        private void UpdateStateTransitions(AnimatorState state)
        {
            for (int i = 0; i < state.transitions.Length; i++)
            {
                UpdateTransitionConditions(state.transitions[i]);
            }
        }

        private void UpdateTransitionConditions(AnimatorStateTransition transition)
        {
            for (int i = 0; i < transition.conditions.Length; i++)
            {
                AnimatorCondition condition = transition.conditions[i];
                if (condition.parameter == oldParameterName)
                {
                    Undo.RecordObject(fx, "Update Animator Transition Condition");
                    condition.parameter = newParameterName;
                    EditorUtility.SetDirty(fx);
                }
            }
        }

        private void UpdateBlendTreeReferences(Motion motion)
        {
            if (motion is BlendTree blendTree)
            {
                foreach (var child in blendTree.children)
                {
                    UpdateBlendTreeReferences(child.motion);
                }

                if (blendTree.blendParameter == oldParameterName)
                {
                    Undo.RecordObject(fx, "Update Blend Tree Parameter");
                    blendTree.blendParameter = newParameterName;
                    EditorUtility.SetDirty(fx);
                }

                if (blendTree.blendParameterY == oldParameterName)
                {
                    Undo.RecordObject(fx, "Update Blend Tree Parameter");
                    blendTree.blendParameterY = newParameterName;
                    EditorUtility.SetDirty(fx);
                }
            }
        }

        #endregion
    }
}