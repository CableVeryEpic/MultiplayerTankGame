using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(MapGeneratorDeterministic))]
public class Map_Inspector : Editor
{
    public VisualTreeAsset mapUI;

    public override VisualElement CreateInspectorGUI()
    {
        serializedObject.Update();

        VisualElement mapInspector = new VisualElement();
        
        if (mapUI != null)
        {
            VisualElement uxmlContent = mapUI.CloneTree();
            uxmlContent.Bind(serializedObject);
            mapInspector.Add(uxmlContent);

            Button generate = uxmlContent.Q<Button>("Generate");
            if (generate != null)
            {
                generate.clicked += () =>
                {
                    serializedObject.ApplyModifiedProperties();

                    var gen = (MapGeneratorDeterministic)target;

                    if (!EditorApplication.isPlaying) return;
                    gen.GenerateFromCurrent();
                };
            }
        }
        return mapInspector;
    }
}
