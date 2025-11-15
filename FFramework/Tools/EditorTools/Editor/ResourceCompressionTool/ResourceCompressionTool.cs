using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public partial class ResourceCompressionTool : EditorWindow
{
    private enum CompressionPage
    {
        Texture = 0,
        Audio = 1,
        Model = 2,
    }

    private CompressionPage currentPage = CompressionPage.Texture;
    private Vector2 scrollPosition;

    // 纹理数据
    private List<Texture2D> selectedTextures = new List<Texture2D>();
    private TextureCompressionSettings textureSettings = new TextureCompressionSettings();
    private TextureSettings texturePreSettings = new TextureSettings();

    // 音频数据
    private List<AudioClip> selectedAudioClips = new List<AudioClip>();
    private AudioCompressionSettings audioSettings = new AudioCompressionSettings();

    // 统计信息
    private CompressionResult lastResult;

    // GUI状态
    private bool showTextureList = true;
    private bool showCompressionSettings = true;
    private bool showTextureSettings = true;
    private bool showAudioList = true;
    private bool showAudioSettings = true;
    private bool showAudioCompressionSettings = true;
    private Dictionary<string, bool> textureItemFoldouts = new Dictionary<string, bool>();
    private Dictionary<string, bool> audioItemFoldouts = new Dictionary<string, bool>();

    [MenuItem("FFramework/Tools/资源压缩工具")]
    public static void ShowWindow()
    {
        ResourceCompressionTool window = GetWindow<ResourceCompressionTool>("资源压缩工具");
        window.minSize = new Vector2(400, 610);
        window.maxSize = new Vector2(500, 1000);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width));
        DrawHeader();
        DrawNavigation();
        DrawCurrentPage();
        DrawFooter();
        EditorGUILayout.EndVertical();
    }
}