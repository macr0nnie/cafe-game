using UnityEditor;

/// Import settings for the Blender lookdev export (Assets/Art/Lookdev/*.fbx):
/// axis conversion baked in so transforms read as Unity axes, no cameras or lights,
/// no FBX animation (prop clips come from CafeLookdev_data.json instead).
public class LookdevImport : AssetPostprocessor
{
    public const string Folder = "Assets/Art/Lookdev/";

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var m = (ModelImporter)assetImporter;
        m.bakeAxisConversion = true;
        m.useFileScale = true;
        m.globalScale = 1f;
        m.importCameras = false;
        m.importLights = false;
        m.importAnimation = false;
        m.animationType = ModelImporterAnimationType.None;
        m.addCollider = false;
        m.isReadable = false;
        m.importBlendShapes = false;
        m.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
    }
}
