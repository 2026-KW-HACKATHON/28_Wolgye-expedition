public static class DalsuSceneContext
{
    public static string SelectedDalsuId;
    public static string CapturedPhotoPath;

    public static bool ShowStampOnMap = false;

    public static void Clear()
    {
        SelectedDalsuId = null;
        CapturedPhotoPath = null;
        ShowStampOnMap=false;
    }
}