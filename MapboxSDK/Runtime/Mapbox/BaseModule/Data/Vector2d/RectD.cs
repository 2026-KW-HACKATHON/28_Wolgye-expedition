namespace Mapbox.BaseModule.Data.Vector2d
{
    public struct RectD
    {
        public Vector2d TopLeft;
        public Vector2d BottomRight;
        //size is absolute width&height so Min+size != max
        public Vector2d Size;
        public Vector2d Center;
        
        public RectD(Vector2d topLeft, Vector2d size)
        {
            TopLeft = topLeft;
            BottomRight = new Vector2d(topLeft.x + size.x, topLeft.y + size.y); //topLeft + size;
            Center = new Vector2d(TopLeft.x + size.x / 2, TopLeft.y + size.y / 2);
            Size = size; //new Vector2d(Mathd.Abs(size.x), Mathd.Abs(size.y));
        }

        public bool Contains(Vector2d point)
        {
            bool flag = Size.x < 0.0 && point.x <= TopLeft.x && point.x > (TopLeft.x + Size.x) || Size.x >= 0.0 && point.x >= TopLeft.x && point.x < (TopLeft.x + Size.x);
            return flag && (Size.y < 0.0 && point.y <= TopLeft.y && point.y > (TopLeft.y + Size.y) || Size.y >= 0.0 && point.y >= TopLeft.y && point.y < (TopLeft.y + Size.y));
        }
    }
}