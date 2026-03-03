using UnityEngine;

namespace ThirdPersonController
{
    public static class LevelSelectionContext
    {
        public static LevelData SelectedLevelData { get; private set; }
        public static ChapterData SelectedChapterData { get; private set; }
        public static bool HasSelection => SelectedLevelData != null;

        public static void SetSelection(LevelData levelData, ChapterData chapterData)
        {
            SelectedLevelData = levelData;
            SelectedChapterData = chapterData;
        }

        public static void ClearSelection()
        {
            SelectedLevelData = null;
            SelectedChapterData = null;
        }
    }
}
