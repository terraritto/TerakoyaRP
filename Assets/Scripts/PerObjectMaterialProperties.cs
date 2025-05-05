using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int cutoffId = Shader.PropertyToID("_Cutoff");
    static int metallicId = Shader.PropertyToID("_Metallic");
    static int smoothnessId = Shader.PropertyToID("_Smoothness");
    static int alphaXId = Shader.PropertyToID("_AlphaX");
    static int alphaYId = Shader.PropertyToID("_AlphaY");

    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField, Range(0f, 1f)]
    float cutoff = 0.5f;

    [SerializeField, Range(0f, 1f)]
    float metallic = 0f;

    [SerializeField, Range(0f, 1f)]
    float smoothness = 0.5f;

    [SerializeField, Range(0.15f, 1f)]
    float alphaX = 0.5f;

    [SerializeField, Range(0.15f, 1f)]
    float alphaY = 0.5f;

    static MaterialPropertyBlock block;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutoffId, cutoff);
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
        block.SetFloat(alphaXId, alphaX);
        block.SetFloat(alphaYId, alphaY);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}
