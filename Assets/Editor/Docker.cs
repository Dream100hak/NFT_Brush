#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public static class Docker
{

    #region Reflection Types
    private class _EditorWindow
    {
        private EditorWindow instance;
        private Type type;

        public _EditorWindow(EditorWindow instance)
        {
            this.instance = instance;
            type = instance.GetType();
        }

        public object m_Parent
        {
            get
            {
                var field = type.GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
                return field.GetValue(instance);
            }
        }
    }

    private class _DockArea
    {
        private object instance;
        private Type type;

        public _DockArea(object instance)
        {
            this.instance = instance;
            type = instance.GetType();
        }

        public object window
        {
            get
            {
                var property = type.GetProperty("window", BindingFlags.Instance | BindingFlags.Public);
                return property.GetValue(instance, null);
            }
        }

        public object s_OriginalDragSource
        {
            set
            {
                var field = type.GetField("s_OriginalDragSource", BindingFlags.Static | BindingFlags.NonPublic);
                field.SetValue(null, value);
            }
        }
    }

    private class _ContainerWindow
    {
        private object instance;
        private Type type;

        public _ContainerWindow(object instance)
        {
            this.instance = instance;
            type = instance.GetType();
        }


        public object rootSplitView
        {
            get
            {
                var property = type.GetProperty("rootSplitView", BindingFlags.Instance | BindingFlags.Public);
                return property.GetValue(instance, null);
            }
        }
    }

    private class _SplitView
    {
        private object instance;
        private Type type;

        public _SplitView(object instance)
        {
            this.instance = instance;
            type = instance.GetType();
        }

        public object DragOver(EditorWindow child, Vector2 screenPoint)
        {
            var method = type.GetMethod("DragOver", BindingFlags.Instance | BindingFlags.Public);
            return method.Invoke(instance, new object[] { child, screenPoint });
        }

        public void PerformDrop(EditorWindow child, object dropInfo, Vector2 screenPoint)
        {
            var method = type.GetMethod("PerformDrop", BindingFlags.Instance | BindingFlags.Public);
            method.Invoke(instance, new object[] { child, dropInfo, screenPoint });
        }
        public Rect position
        {
            get
            {
                var property = type.GetProperty("screenPosition", BindingFlags.Instance | BindingFlags.Public);
                return (Rect)property.GetValue(instance, null);
            }
        }
    }
    #endregion

    public static void Dock(this EditorWindow wnd, EditorWindow other, E_DockPosition position)
    {
        var parent = new _EditorWindow(wnd);
        var child = new _EditorWindow(other);
        var dockArea = new _DockArea(parent.m_Parent);
        var containerWindow = new _ContainerWindow(dockArea.window);
        var splitView = new _SplitView(containerWindow.rootSplitView);
        var mousePosition = GetFakeMousePosition(splitView, position , 20);
        var dropInfo = splitView.DragOver(other, mousePosition);
        dockArea.s_OriginalDragSource = child.m_Parent;
        splitView.PerformDrop(other, dropInfo, mousePosition);
    }

    private static Vector2 GetFakeMousePosition(_SplitView view, E_DockPosition position, float offset)
    {
        Vector2 mousePosition = Vector2.zero;

        switch (position)
        {
            case E_DockPosition.Left:
                mousePosition = new Vector2(offset, view.position.height / 2);
                break;
            case E_DockPosition.Top:
                mousePosition = new Vector2(view.position.width / 2, offset);
                break;
            case E_DockPosition.Right:
                mousePosition = new Vector2(view.position.width - offset, view.position.size.y / 2);
                break;
            case E_DockPosition.Bottom:
                mousePosition = new Vector2(view.position.width / 2, view.position.height - offset);
                break;
        }

        return new Vector2(view.position.x + mousePosition.x, view.position.y + mousePosition.y);
    }
}
#endif