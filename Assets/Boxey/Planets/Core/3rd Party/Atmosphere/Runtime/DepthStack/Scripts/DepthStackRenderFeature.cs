using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Add this Render Feature to the active URP Renderer and assign the CopyDepth material in DepthStack/Shader to use encoded depth in a shader

public class DepthStackRenderFeature : ScriptableRendererFeature
{
    private DepthStackRenderPass cameraRenderPass;
    private Material copyDepth;


    public override void Create()
    {
        ValidateDepthMaterial();

        cameraRenderPass = new DepthStackRenderPass(copyDepth);
        cameraRenderPass.renderPassEvent = RenderPassEvent.AfterRendering;
        cameraRenderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.isPreviewCamera) renderer.EnqueuePass(cameraRenderPass);
    }


    private void ValidateDepthMaterial()
    {
        var copyDepthShader = AddAlwaysIncludedShader("Hidden/EncodeDepth");

        if (copyDepthShader == null)
        {
            Debug.LogError(
                "CopyDepth shader could not be found! Make sure Hidden/CopyDepth shader is located somewhere in your project and included in 'Always Included Shaders'",
                this);
            return;
        }

        copyDepth = new Material(copyDepthShader);
    }


    private static Shader AddAlwaysIncludedShader(string shaderName)
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