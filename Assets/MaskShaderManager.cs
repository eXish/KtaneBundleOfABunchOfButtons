using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class MaskShaderManager : MonoBehaviour
{
    public Shader MaskShader;
    public Shader DiffuseTintShader;
    public Shader TextShader;
    public Shader DiffuseTextShader;

    public static HashSet<int> UsedMaskLayers = new HashSet<int>();

    public void Clear()
    {
        UsedMaskLayers.Clear();
    }

    public MaskMaterials MakeMaterials()
    {
        if (UsedMaskLayers.Count >= 255)
            UsedMaskLayers.Clear();

        int layer;
        if (UsedMaskLayers.Count < 128)
        {
            do
                layer = Rnd.Range(1, 256);
            while (UsedMaskLayers.Contains(layer));
        }
        else
        {
            var available = Enumerable.Range(1, 255).Where(i => !UsedMaskLayers.Contains(i)).ToArray();
            layer = available[Rnd.Range(0, available.Length)];
        }

        UsedMaskLayers.Add(layer);

        var maskMat = new Material(MaskShader);
        maskMat.SetInt("_Layer", layer);
        maskMat.renderQueue = 1000;

        var diffuseTint = new Material(DiffuseTintShader);
        diffuseTint.SetInt("_Layer", layer);

        var diffuseText = new Material(DiffuseTextShader);
        diffuseText.SetInt("_Layer", layer);

        var text = new Material(TextShader);
        text.SetInt("_Layer", layer);

        return new MaskMaterials
        {
            Mask = maskMat,
            DiffuseTint = diffuseTint,
            DiffuseText = diffuseText,
            Text = text
        };
    }
}
