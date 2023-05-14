using System;
using System.Reflection;
using UnityEditor;

public class CustomGameViewSize
{
    private static object gameViewSizesInstance;
    private static MethodInfo getGroupMethod;


    static CustomGameViewSize()
    {
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleType.GetProperty("instance");
        getGroupMethod = sizesType.GetMethod("GetGroup");
        gameViewSizesInstance = instanceProp.GetValue(null, null);
    }

    public enum GameViewSizeType
    {
        AspectRatio,
        FixedResolution
    }



    public static void SetSize(int index)
    {
        var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");

        //if (Utils.IsWindowOpen(gameViewType))
          //  return;

        var gameViewWindow = EditorWindow.GetWindow(gameViewType);
        var sizeSelectionCallback = gameViewType.GetMethod("SizeSelectionCallback");
        sizeSelectionCallback.Invoke(gameViewWindow, new object[] { index, null });
    }

    public static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
    {
        Type type = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");

        var group = GetGroup(sizeGroupType);
        var addCustomSize = getGroupMethod.ReturnType.GetMethod("AddCustomSize");
        var gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");

        var constructor = gameViewSizeType.GetConstructor(new Type[] { type, typeof(int), typeof(int), typeof(string) });
        var newSize = constructor.Invoke(new object[] { (int)viewSizeType, width, height, text });

        addCustomSize.Invoke(group, new object[] { newSize });
    }

    public static bool SizeExists(GameViewSizeGroupType sizeGroupType, string text)
    {
        return FindSize(sizeGroupType, text) != -1;
    }

    public static int FindSize(GameViewSizeGroupType sizeGroupType, string text)
    {
        var group = GetGroup(sizeGroupType);
        var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
        var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
        for (int i = 0; i < displayTexts.Length; i++)
        {
            string display = displayTexts[i];
            int pren = display.IndexOf('(');
            if (pren != -1)
                display = display.Substring(0, pren - 1);
            if (display == text)
                return i;
        }

        return -1;
    }

    public static bool SizeExists(GameViewSizeGroupType sizeGroupType, int width, int height)
    {
        return FindSize(sizeGroupType, width, height) != -1;
    }

    public static int FindSize(GameViewSizeGroupType sizeGroupType, int width, int height)
    {
        var group = GetGroup(sizeGroupType);
        var groupType = group.GetType();

        var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
        var getCustomCount = groupType.GetMethod("GetCustomCount");

        int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);

        var getGameViewSize = groupType.GetMethod("GetGameViewSize");
        var gvsType = getGameViewSize.ReturnType;

        var widthProp = gvsType.GetProperty("width");
        var heightProp = gvsType.GetProperty("height");

        var indexValue = new object[1];
        for (int i = 0; i < sizesCount; i++)
        {
            indexValue[0] = i;
            var size = getGameViewSize.Invoke(group, indexValue);
            int sizeWidth = (int)widthProp.GetValue(size, null);
            int sizeHeight = (int)heightProp.GetValue(size, null);
            if (sizeWidth == width && sizeHeight == height)
                return i;
        }

        return -1;
    }

    public static object GetGroup(GameViewSizeGroupType type)
    {
        return getGroupMethod.Invoke(gameViewSizesInstance, new object[] { (int)type });
    }

    public static GameViewSizeGroupType GetCurrentGroupType()
    {
        var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
        return (GameViewSizeGroupType)(int)getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
    }
}