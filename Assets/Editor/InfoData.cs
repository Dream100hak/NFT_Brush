using UnityEngine;

public abstract class  InfoData<T> where T : UnityEngine.Object, new()
{
    private static T s_editorData;
    public static T ED
    {
        get
        {
            if (s_editorData == null)
            {
                s_editorData = GetEditorData();
            }
            return s_editorData;
        }
    }

    public static T GetEditorData() => Resources.Load<T>("Data/" + typeof(T).Name);

}
