using System;

namespace QuickEye.Editor.IconWindow
{
    [Flags]
    public enum IconFilter
    {
        None = 0,
        Everything = ~0,
        AlternativeSkin = 1,
        RetinaVersions = 2,
        OtherImages = 4
    }
}