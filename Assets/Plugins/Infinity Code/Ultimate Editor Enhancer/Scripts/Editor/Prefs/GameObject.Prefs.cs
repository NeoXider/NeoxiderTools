/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;

namespace InfinityCode.UltimateEditorEnhancer
{
    public static partial class Prefs
    {
        public class GameObjectManager : StandalonePrefManager<GameObjectManager>, IStateablePref
        {
            public override IEnumerable<string> keywords
            {
                get
                {
                    return SelectionBoundsManager.GetKeywords()
                        .Concat(DropToFloorManager.GetKeywords())
                        .Concat(GroupManager.GetKeywords())
                        .Concat(UngroupManager.GetKeywords())
                        .Concat(RenameManager.GetKeywords())
                        .Concat(ReplaceManager.GetKeywords())
                        .Concat(RotateByShortcutManager.GetKeywords());
                }
            }

            public override void Draw()
            {
                SelectionBoundsManager.Draw(null);
                DropToFloorManager.Draw(null);
                GroupManager.Draw(null);
                UngroupManager.Draw(null);
                RenameManager.Draw(null);
                ReplaceManager.Draw(null);
                RotateByShortcutManager.Draw(null);
            }

            public string GetMenuName()
            {
                return "Game Object";
            }

            public void SetState(bool state)
            {
                SelectionBoundsManager.SetState(state);
                DropToFloorManager.SetState(state);
                GroupManager.SetState(state);
                UngroupManager.SetState(state);
                RenameManager.SetState(state);
                ReplaceManager.SetState(state);
                RotateByShortcutManager.SetState(state);
            }
        }
    }
}