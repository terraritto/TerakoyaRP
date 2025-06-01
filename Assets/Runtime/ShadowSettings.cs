using UnityEngine;

[System.Serializable]
public class ShadowSettings
{
    // ShadowMap�̃T�C�Y(2�̗ݏ�)
    public enum MapSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192,
    }

    // �e�̕`�������ő勗��
    [Min(0f)]
    public float maxDistance = 100f;

    // Shadow�pDirectionalLight
    [System.Serializable]
    public struct Directional
    {
        public MapSize atlasSize;
    }
    
    // �f�t�H���g�l
    public Directional directional =
        new Directional 
        { 
            atlasSize = MapSize._1024 
        };
}
