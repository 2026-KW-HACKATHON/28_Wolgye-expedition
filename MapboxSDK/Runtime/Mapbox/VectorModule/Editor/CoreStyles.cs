using UnityEditor;
using UnityEngine;

namespace Mapbox.VectorModule.Editor
{
    public static class CoreStyles
    {
        static readonly Color k_Normal_AllTheme = new Color32(0, 0, 0, 0);
        //static readonly Color k_Hover_Dark = new Color32(70, 70, 70, 255);
        //static readonly Color k_Hover = new Color32(193, 193, 193, 255);
        static readonly Color k_Active_Dark = new Color32(80, 80, 80, 255);
        static readonly Color k_Active = new Color32(216, 216, 216, 255);

        static readonly int s_MoreOptionsHash = "MoreOptions".GetHashCode();

        static public GUIContent moreOptionsLabel { get; private set; }
        static public GUIStyle moreOptionsStyle { get; private set; }
        static public GUIStyle moreOptionsLabelStyle { get; private set; }

        static CoreStyles()
        {
            moreOptionsLabel = EditorGUIUtility.TrIconContent("MoreOptions", "More Options");

            moreOptionsStyle = new GUIStyle(GUI.skin.toggle);
            Texture2D normalColor = new Texture2D(1, 1);
            normalColor.SetPixel(1, 1, k_Normal_AllTheme);
            moreOptionsStyle.normal.background = normalColor;
            moreOptionsStyle.onActive.background = normalColor;
            moreOptionsStyle.onFocused.background = normalColor;
            moreOptionsStyle.onNormal.background = normalColor;
            moreOptionsStyle.onHover.background = normalColor;
            moreOptionsStyle.active.background = normalColor;
            moreOptionsStyle.focused.background = normalColor;
            moreOptionsStyle.hover.background = normalColor;

            moreOptionsLabelStyle = new GUIStyle(GUI.skin.label);
            moreOptionsLabelStyle.padding = new RectOffset(0, 0, 0, -1);
        }

        //Note:
        // - GUIStyle seams to be broken: all states have same state than normal light theme
        // - Hover with event will not be updated right when we enter the rect
        //-> Removing hover for now. Keep theme color for refactoring with UIElement later
        static public bool DrawMoreOptions(Rect rect, bool active)
        {
            int id = GUIUtility.GetControlID(s_MoreOptionsHash, FocusType.Passive, rect);
            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                    Color background = k_Normal_AllTheme;
                    if (active)
                        background = EditorGUIUtility.isProSkin ? k_Active_Dark : k_Active;
                    EditorGUI.DrawRect(rect, background);
                    GUI.Label(rect, moreOptionsLabel, moreOptionsLabelStyle);
                    break;
                case EventType.KeyDown:
                    bool anyModifiers = (evt.alt || evt.shift || evt.command || evt.control);
                    if ((evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && !anyModifiers && GUIUtility.keyboardControl == id)
                    {
                        evt.Use();
                        GUI.changed = true;
                        return !active;
                    }
                    break;
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition))
                    {
                        GrabMouseControl(id);
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (HasMouseControl(id))
                    {
                        ReleaseMouseControl();
                        evt.Use();
                        if (rect.Contains(evt.mousePosition))
                        {
                            GUI.changed = true;
                            return !active;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (HasMouseControl(id))
                        evt.Use();
                    break;
            }

            return active;
        }

        static int s_GrabbedID = -1;
        static void GrabMouseControl(int id) => s_GrabbedID = id;
        static void ReleaseMouseControl() => s_GrabbedID = -1;
        static bool HasMouseControl(int id) => s_GrabbedID == id;
    }
}