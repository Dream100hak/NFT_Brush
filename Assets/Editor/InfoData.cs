using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class InfoData<T> where T : UnityEngine.Object, new()
{

    private static int s_generateId;
    public static int GenerateId { get { return s_generateId; } }

    private static SortedSet<int> s_emptyGenerateIds = new SortedSet<int>();
    public static SortedSet<int> EmptyGenerateIds { get => s_emptyGenerateIds; }

    public static List<int> ToDeleteIds { get; set; } = new List<int>(); // 삭제 예정 아이디
    public static Dictionary<int, Component> ToRestoreIds { get; set; } = new Dictionary<int, Component>(); // 복원 예정 아이디

    public static Action OnClear;

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

    private static void FindGenerateId<Comp>(Dictionary<int, Comp> dicDatas) where Comp : Component
    {
        while (true)
        {
            if (dicDatas.ContainsKey(s_generateId) == false)
                break;

            s_generateId++;
        }
    }

    public static int NewGenerateId<Comp>(Dictionary<int, Comp> dicDatas) where Comp : Component
    {
        int newLayerId;
        if (s_emptyGenerateIds.Count > 0)
        {
            newLayerId = s_emptyGenerateIds.First();
            s_emptyGenerateIds.Remove(newLayerId);
        }
        else
        {
            FindGenerateId(dicDatas);
            newLayerId = s_generateId;
        }

        return newLayerId;
    }
    public static void DeleteLayerIds<Comp>(Dictionary<int, Comp> dicDatas) where Comp : Component  // 아이디 삭제.. 
    {
        if (ToDeleteIds.Count > 0)
        {
            foreach (var id in ToDeleteIds)
                dicDatas.Remove(id);

            ToDeleteIds.Clear();
        }
    }
    public static void RestoreLayerIds<Comp>(Dictionary<int, Comp> dicDatas) where Comp : Component // 아이디 복구.. 
    {
        if (ToRestoreIds.Count > 0)
        {
            foreach (var restoreDics in ToRestoreIds)
                dicDatas.Add(restoreDics.Key, restoreDics.Value as Comp);

            ToRestoreIds.Clear();
        }
    }
    public static void Clear()
    {
        s_generateId = 0;
        s_emptyGenerateIds.Clear();
        ToDeleteIds.Clear();
        ToRestoreIds.Clear();
        OnClear?.Invoke();
    }

}
