using UnityEditor;

/// <summary>
/// รูปทุกไฟล์ที่ลากเข้า Assets/Resources/Expressions จะถูกตั้งเป็น Sprite (Single) ให้เองอัตโนมัติ
/// อย่าลืมตั้งชื่อไฟล์เป็น neutral, flat, smirk, grin, angry, sweat (ตัวพิมพ์เล็ก)
/// </summary>
public class ExpressionImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Expressions/")) return;
        TextureImporter ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
    }
}
