using UnityEngine;

public static class ItemIconLibrary
{
    private const string AtlasResourcePath = "ItemIcons/ItemIconAtlas";
    private const int ColumnCount = 4;
    private const int RowCount = 2;

    private static Texture2D atlas;
    private static Sprite[] icons;

    public static Sprite Get(ItemId itemId)
    {
        int index = (int)itemId - 1;
        if (index < 0 || index >= ColumnCount * RowCount)
            return null;

        EnsureLoaded();
        return icons != null ? icons[index] : null;
    }

    private static void EnsureLoaded()
    {
        if (icons != null)
            return;

        atlas = Resources.Load<Texture2D>(AtlasResourcePath);
        if (atlas == null)
        {
            Debug.LogWarning($"Item icon atlas not found at Resources/{AtlasResourcePath}.");
            icons = new Sprite[ColumnCount * RowCount];
            return;
        }

        icons = new Sprite[ColumnCount * RowCount];
        float cellWidth = atlas.width / (float)ColumnCount;
        float cellHeight = atlas.height / (float)RowCount;
        for (int index = 0; index < icons.Length; index++)
        {
            int column = index % ColumnCount;
            int rowFromTop = index / ColumnCount;
            float x = column * cellWidth;
            float y = atlas.height - (rowFromTop + 1) * cellHeight;
            icons[index] = Sprite.Create(
                atlas,
                new Rect(x, y, cellWidth, cellHeight),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(cellWidth, cellHeight)
            );
            icons[index].name = ((ItemId)(index + 1)) + " Icon";
        }
    }
}