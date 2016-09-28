using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FindReferencesInProject : EditorWindow {

    const string dummyFileName = "FindReferencesInProject.txt";
    public Object objectToExamine;              //Object that we are looking dependecies for
    [SerializeField]
    public List<Object> ObjectsToExamine;
    List<Object> References;                    //A list of found references for the object, including scenes
    Vector2 scrollPos = new Vector2();          //scroll position for scroll area

    bool fastMode;

    static Dictionary<Object, List<Object>> DependenciesCash;

    [MenuItem("Assets/Find References In Project", false, 20)]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        FindReferencesInProject window = (FindReferencesInProject)EditorWindow.GetWindow(typeof(FindReferencesInProject));
        window.Show();
        if (Selection.activeObject != null && AssetDatabase.IsMainAsset(Selection.activeObject))
        {
            window.objectToExamine = Selection.activeObject;
            
        }
        
    }

    [MenuItem("Assets/Find References In Project", true)]
    static bool Validate()
    {
        //Validate if we selected any asset and not scene object
        return (Selection.activeObject != null && AssetDatabase.IsMainAsset(Selection.activeObject)); 
    }

    void BugWorkAround()
    {
        if (!System.IO.File.Exists(Application.dataPath.Replace("Assets", Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this))) + "/" + dummyFileName)))
        {
            System.IO.File.Create(Application.dataPath.Replace("Assets", Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this))) + "/" + dummyFileName)).Close();
        }
        AssetDatabase.ImportAsset(Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this))) + "/" + dummyFileName); //Used for removing some bug with fonts (In some projects after the script all texts in the editor are missing by some rison) Some editor redraw bug. After reimporting any asset everything becomes normal.
    }

    void OnGUI()
    {
        GUILayout.Label("Find references of object in the project.");

        var so = new SerializedObject(this);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(so.FindProperty("ObjectsToExamine"), true);
        if (EditorGUI.EndChangeCheck())
            so.ApplyModifiedProperties();
        
        fastMode = GUILayout.Toggle(fastMode, new GUIContent("fast mode", "In fast mode all dependencies are cashed."));
        if (fastMode)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cash dependencies"))
            {
                CashDependencies();
                BugWorkAround();
            }

            if (DependenciesCash == null)
                GUILayout.Label("null");
            else
                GUILayout.Label("Cashed " + DependenciesCash.Count + "assets");
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Find refs in project", "Find refenrences in the project")))
        {
            if (References == null)
                References = new List<Object>();
            else
                References.Clear();

            if (!fastMode)
                References = CollectReverseDependencies(ObjectsToExamine);
            else
                References = CollectReverseDependenciesFast(ObjectsToExamine);

            BugWorkAround();
        }

        if (GUILayout.Button(new GUIContent("Find ref in scene", "Find references in currently open scene.")))
        {
            Selection.objects = ObjectsToExamine.ToArray();
            EditorApplication.ExecuteMenuItem("Assets/Find References In Scene");
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Found references:");

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.BeginVertical();
        if (References != null)
            foreach (Object refObj in References)
            {
                EditorGUILayout.ObjectField(refObj, typeof(Object), false);
            }
        GUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    public static List<Object> CollectDependencies(List<Object> b)
    {
        HashSet<Object> result = new HashSet<Object>();
        string[] AssetPaths = AssetDatabase.GetAllAssetPaths();
        List<Object> objs = new List<Object>();
        foreach (Object objToTest in b)
            objs.Add(objToTest);
        Object[] dependencies = EditorUtility.CollectDependencies(objs.ToArray());
        int i = 0;
        foreach (string assetPath in AssetPaths)
        {
            i++;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
                if (dependencies.Contains(asset))
                    result.Add(asset);
            if (EditorUtility.DisplayCancelableProgressBar("Find references in the project.", "Looking for references in " + AssetPaths.Length + " assets...", (float)i / AssetPaths.Length))
                break;
        }
        return result.ToList();
    }
    
    public static List<Object> CollectReverseDependencies(List<Object> b)
    {
        HashSet<Object> result = new HashSet<Object>();
        int i = 0;
        string[] AssetPaths = AssetDatabase.GetAllAssetPaths();
        List<Object> objs = new List<Object>();
        foreach (Object objToTest in b)
            objs.AddRange(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(objToTest)));

        foreach (string assetPath in AssetPaths)
        {
            i++;
            Object obj = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (b.Contains(obj)) continue;
            Object[] dependencies = EditorUtility.CollectDependencies(new Object[1] { obj });
            if (dependencies.Length < 2) continue;

            foreach (Object dp in dependencies)
                foreach(Object child in objs)
                    if (dp == child)
                    {
                        result.Add(obj);
                        break;
                    }

            if (EditorUtility.DisplayCancelableProgressBar("Find references in the project.", "Looking for references in " + AssetPaths.Length + " assets...", (float)i / AssetPaths.Length))
                break;
        }
        EditorUtility.ClearProgressBar();
        return result.ToList();
    }

    public static List<Object> CollectReverseDependenciesFast(List<Object> b)
    {
        if (DependenciesCash == null)
            CashDependencies();

        HashSet<Object> result = new HashSet<Object>();
        int i = 0;

        List<Object> objs = new List<Object>();
        foreach (Object objToTest in b)
            objs.AddRange(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(objToTest)));

        foreach (KeyValuePair<Object, List<Object>> pair in DependenciesCash)
        {
            i++;

            if (b.Contains(pair.Key)) continue;
            if (pair.Value.Count < 2) continue;
            foreach (Object dp in pair.Value)
                foreach(Object child in objs)
                    if (dp == child)
                    {
                        result.Add(pair.Key);
                        break;
                    }
            if (EditorUtility.DisplayCancelableProgressBar("Find references in the project.", "Looking for references in " + DependenciesCash.Count + " cashed assets...", (float)i / DependenciesCash.Count))
                break;
        }
        EditorUtility.ClearProgressBar();
        return result.ToList();
    }

    public static void CashDependencies()
    {
        if (DependenciesCash != null)
            DependenciesCash.Clear();
        else
            DependenciesCash = new Dictionary<Object, List<Object>>();

        string[] AssetPaths = AssetDatabase.GetAllAssetPaths();
        int i = 0;
        foreach (string assetPath in AssetPaths)
        {
            i++;

            Object obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            Object[] dependencies = EditorUtility.CollectDependencies(new Object[1] { obj });
            if (dependencies.Length < 2) continue;
            DependenciesCash.Add(obj, new List<Object>(dependencies));
            if (EditorUtility.DisplayCancelableProgressBar("Ccahing dependencies.", "Caching dependencies for " + AssetPaths.Length + " assets...", (float)i / AssetPaths.Length))
                break;
        }
        EditorUtility.ClearProgressBar();
    }
}
