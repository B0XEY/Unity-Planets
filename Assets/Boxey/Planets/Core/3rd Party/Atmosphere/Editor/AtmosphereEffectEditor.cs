using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CustomEditor(typeof(AtmosphereEffect))]
public class AtmosphereEffectEditor : Editor
{
    private SerializedProperty atmosphereScale;
    private SerializedProperty cutoffDepth;
    private SerializedProperty directional;
    private SerializedProperty planetRadius;
    private SerializedProperty profile;
    private SerializedProperty sun;


    private void OnEnable()
    {
        profile = serializedObject.FindProperty("profile");
        sun = serializedObject.FindProperty("sun");
        directional = serializedObject.FindProperty("directional");
        planetRadius = serializedObject.FindProperty("planetRadius");
        cutoffDepth = serializedObject.FindProperty("cutoffDepth");
        atmosphereScale = serializedObject.FindProperty("atmosphereScale");
    }


    public void OnSceneGUI()
    {
        var effects = Array.ConvertAll(targets, item => (AtmosphereEffect)item);

        for (var i = 0; i < effects.Length; i++)
        {
            var effect = effects[i];

            EditorGUI.BeginChangeCheck();
            Handles.color = Color.yellow;
            var newPlanet = Handles.RadiusHandle(Quaternion.identity, effect.transform.position, effect.planetRadius);

            Handles.color = Color.red;
            var newCutoff = Handles.RadiusHandle(Quaternion.identity, effect.transform.position,
                -effect.cutoffDepth + effect.planetRadius);

            Handles.color = Color.blue;
            var newAtmo = Handles.RadiusHandle(Quaternion.identity, effect.transform.position, effect.AtmosphereSize);


            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(effect, "Changed Atmosphere Radii");

                effect.atmosphereScale = newAtmo / effect.planetRadius - 1;
                effect.planetRadius = newPlanet;
                effect.cutoffDepth = -(newCutoff - effect.planetRadius);
            }
        }
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ShowWarnings();

        DrawPropertyLabelControl(profile,
            new GUIContent("Profile", "The Atmosphere Profile used for rendering the Atmosphere Effect."),
            GUILayout.Width(110));

        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Sun", "The main light that affects the atmosphere."),
            GUILayout.Width(110));
        EditorGUILayout.PropertyField(sun, GUIContent.none);

        TightLabel("Directional",
            "How the atmosphere should treat light direction. If Directional is enabled, atmosphere will treat light direction as applying evenly everywhere. Otherwise, shader will calculate light direction based on direction to sun transform.");
        EditorGUILayout.PropertyField(directional, GUIContent.none, GUILayout.Width(15));

        GUILayout.EndHorizontal();

        var prevWidth = EditorGUIUtility.labelWidth;

        EditorGUIUtility.labelWidth = 110;

        EditorGUILayout.PropertyField(planetRadius,
            new GUIContent("Planet Radius", "The radius of the planet the atmosphere renders above."));
        EditorGUILayout.PropertyField(cutoffDepth,
            new GUIContent("Cutoff Depth",
                "The depth below the Planet Radius in which the atmosphere will no longer render. Change this value to prevent flickering or infinitely bright objects at the planet center"));
        EditorGUILayout.PropertyField(atmosphereScale,
            new GUIContent("Atmosphere Scale", "The scale of the planet atmosphere relative to the planet radius"));

        EditorGUIUtility.labelWidth = prevWidth;

        serializedObject.ApplyModifiedProperties();
    }


    private void ShowWarnings()
    {
        var effect = (AtmosphereEffect)target;

        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            EditorGUILayout.HelpBox("Effect is only compatible with the Universal Render Pipeline!", MessageType.Error);
        else if (effect.profile == null)
            EditorGUILayout.HelpBox("Atmosphere Profile required to display effect!", MessageType.Error);
        else if (effect.sun == null)
            EditorGUILayout.HelpBox("Sun transform required to display effect!", MessageType.Error);
    }


    private void DrawPropertyLabelControl(SerializedProperty property, GUIContent content,
        params GUILayoutOption[] options)
    {
        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(content, options);
        EditorGUILayout.PropertyField(property, GUIContent.none);

        GUILayout.EndHorizontal();
    }


    public static void TightLabel(string labelStr, string tooltip = null)
    {
        var label = tooltip == null ? new GUIContent(labelStr) : new GUIContent(labelStr, tooltip);
        EditorGUILayout.LabelField(label, GUILayout.Width(GUI.skin.label.CalcSize(label).x));
    }
}