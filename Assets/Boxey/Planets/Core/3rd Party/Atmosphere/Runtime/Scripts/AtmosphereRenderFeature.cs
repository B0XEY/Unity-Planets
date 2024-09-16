using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AtmosphereRenderFeature : ScriptableRendererFeature
{
    private AtmosphereRenderPass atmospherePass;
    private Shader atmosphereShader;


    public override void Create()
    {
        ValidateShader();

        atmospherePass = new AtmosphereRenderPass(atmosphereShader);

        // Effect does not work with transparents since they do not write to the depth buffer. Sorry if you wanted to have a planet made of glass.
        atmospherePass.renderPassEvent =
            RenderPassEvent.AfterRenderingSkybox; //RenderPassEvent.BeforeRenderingTransparents;

        atmospherePass.ConfigureInput(ScriptableRenderPassInput.Depth);
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Prevent renering in material previews.
        if (!renderingData.cameraData.isPreviewCamera) renderer.EnqueuePass(atmospherePass);
    }


    private void ValidateShader()
    {
        var shader = AddAlwaysIncludedShader("Hidden/Atmosphere");

        if (shader == null)
        {
            Debug.LogError(
                "Atmosphere shader could not be found! Make sure Hidden/Atmosphere is located somewhere in your project and included in 'Always Included Shaders'",
                this);
            return;
        }

        atmosphereShader = shader;
    }


    // NOTE: Does not always immediately add the shader. If the shader was just recently imported with the project, may return null if the asset isn't loaded and compiled.
    public static Shader AddAlwaysIncludedShader(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) return null;

#if UNITY_EDITOR
        var graphicsSettingsObj =
            AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        var serializedObject = new SerializedObject(graphicsSettingsObj);
        var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");
        var hasShader = false;

        for (var i = 0; i < arrayProp.arraySize; ++i)
        {
            var arrayElem = arrayProp.GetArrayElementAtIndex(i);
            if (shader == arrayElem.objectReferenceValue)
            {
                hasShader = true;
                break;
            }
        }

        if (!hasShader)
        {
            var arrayIndex = arrayProp.arraySize;
            arrayProp.InsertArrayElementAtIndex(arrayIndex);
            var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
            arrayElem.objectReferenceValue = shader;

            serializedObject.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
        }
#endif

        return shader;
    }
}