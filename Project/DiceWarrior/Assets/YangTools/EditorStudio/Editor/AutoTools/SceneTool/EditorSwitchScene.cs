#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YangTools
{
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;
#if UNITY_2019_1_OR_NEWER
    using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

    /// <summary>
    /// 扩展Unity的按钮栏
    /// </summary>
    [InitializeOnLoad]
    public static class EditorSwitchScene
    {
        public static Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        public static ScriptableObject m_currentToolbar;

        private static GUIContent switchSceneBtContent;
        private static List<string> sceneAssetList;

        static EditorSwitchScene()
        {
            EditorApplication.delayCall += OnUpdate;
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            sceneAssetList = new List<string>();
            var curOpenSceneName = SceneManager.GetActiveScene().name;
            switchSceneBtContent = EditorGUIUtility.TrTextContentWithIcon(string.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName, "切换场景", "UnityLogo");
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            switchSceneBtContent.text = scene.name;
        }

        static int m_toolCount;
        public static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                // 查找系统自带的Toolbar
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

                if (m_currentToolbar != null)
                {
                    // 固定写法
                    var root = m_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (root != null)
                    {
                        var rawRoot = root.GetValue(m_currentToolbar);
                        var mRoot = rawRoot as VisualElement;
                        RegisterCallback("ToolbarZoneLeftAlign", GUILeft);
                        RegisterCallback("ToolbarZoneRightAlign", GUIRight);

                        void RegisterCallback(string root, Action cb)
                        {
                            var toolbarZone = mRoot.Q(root);

                            var parent = new VisualElement()
                            {
                                style =
                                {
                                    flexGrow = 1,
                                    flexDirection = FlexDirection.Row,
                                }
                            };
                            var container = new IMGUIContainer();
                            container.onGUIHandler += () =>
                            {
                                cb?.Invoke();
                            };
                            parent.Add(container);
                            toolbarZone.Add(parent);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制左侧的元素
        /// </summary>
        private static void GUILeft()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("可扩展"))
            { }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制右侧的元素
        /// </summary>
        private static void GUIRight()
        {
            GUILayout.BeginHorizontal();
            if (EditorGUILayout.DropdownButton(switchSceneBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150)))
            {
                DrawSwithSceneDropdownMenus();
            }
            GUILayout.EndHorizontal();
        }

        #region 绘制切换场景的Toolbar
        /// <summary>
        /// 绘制SwitchScene的下拉菜单
        /// </summary>
        static void DrawSwithSceneDropdownMenus()
        {
            GenericMenu popMenu = new GenericMenu();
            popMenu.allowDuplicateNames = true;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets" });
            sceneAssetList.Clear();
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                sceneAssetList.Add(scenePath);
                string fileDir = System.IO.Path.GetDirectoryName(scenePath);
                bool isInRootDir = GetRegularPath("Assets").TrimEnd('/') == GetRegularPath(fileDir).TrimEnd('/');
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                string displayName = sceneName;
                if (!isInRootDir)
                {
                    var sceneDir = System.IO.Path.GetRelativePath("Assets", fileDir);
                    displayName = $"{sceneDir}/{sceneName}";
                }

                popMenu.AddItem(new GUIContent(displayName), false, menuIdx => { SwitchScene((int)menuIdx); }, i);
            }
            popMenu.ShowAsContext();
        }

        static string GetRegularPath(string path)
        {
            return path?.Replace('\\', '/');
        }

        /// <summary>
        /// 执行切换场景的事件
        /// </summary>
        /// <param name="menuIdx"></param>
        private static void SwitchScene(int menuIdx)
        {
            if (menuIdx >= 0 && menuIdx < sceneAssetList.Count)
            {
                var scenePath = sceneAssetList[menuIdx];
                var curScene = SceneManager.GetActiveScene();
                if (curScene.isDirty)
                {
                    int opIndex = EditorUtility.DisplayDialogComplex("警告", $"当前场景{curScene.name}未保存,是否保存?", "保存", "取消", "不保存");
                    switch (opIndex)
                    {
                        case 0:
                            if (!EditorSceneManager.SaveOpenScenes())
                            {
                                return;
                            }
                            break;
                        case 1:
                            return;
                    }
                }
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                  
                }
                EditorSceneManager.CloseScene(curScene, false);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
        }
        #endregion
    }
}
#endif
