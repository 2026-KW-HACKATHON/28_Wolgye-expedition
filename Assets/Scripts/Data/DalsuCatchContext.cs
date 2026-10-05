public static class DalsuCaptureContext
{
    public static DalsuData CurrentDalsu { get; private set; }

    public static void Set(DalsuData dalsuData)
    {
        CurrentDalsu = dalsuData;
    }

    public static void Clear()
    {
        CurrentDalsu = null;
    }
}