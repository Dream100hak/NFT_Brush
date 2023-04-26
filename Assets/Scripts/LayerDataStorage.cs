using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "LayerDataStorage", menuName = "LayerDataStorage/Data", order = 1)]
public class LayerDataStorage : ScriptableObject
{
//    private SortedSet<int> _emptyLayerIds;
//    public SortedSet<int> EmptyLayerIds { get => _emptyLayerIds; }

//#if UNITY_EDITOR
//    public int GenerateId
//    {

//        get => EditorPrefs.GetInt("GenerateId", 0);
//        set => EditorPrefs.SetInt("GenerateId", value);
//    }

//#endif


//#if UNITY_EDITOR
//    private void OnEnable()
//    {

//        if (EditorPrefs.HasKey("EmptyLayerIds"))
//        {
//            string[] ids = EditorPrefs.GetString("EmptyLayerIds").Split(',');
//            _emptyLayerIds = new SortedSet<int>(ids.Select(s =>
//            {
//                int.TryParse(s, out int result);
//                return result;
//            }).Where(i => i != 0));
//        }
//        else
//        {
//            _emptyLayerIds = new SortedSet<int>();
//        }

//    }

//    private void OnDisable()
//    {
//        EditorPrefs.SetString("EmptyLayerIds", string.Join(",", _emptyLayerIds));
//    }
//#endif
}