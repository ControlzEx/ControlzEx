#pragma warning disable CA1060, CS0618, SA1307, SA1602
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Windows.Win32
{
    internal enum SC : uint
    {
        SIZE = 0xF000,
        MOVE = 0xF010,
        MOUSEMOVE = 0xF012,
        MINIMIZE = 0xF020,
        MAXIMIZE = 0xF030,
        NEXTWINDOW = 0xF040,
        PREVWINDOW = 0xF050,
        CLOSE = 0xF060,
        VSCROLL = 0xF070,
        HSCROLL = 0xF080,
        MOUSEMENU = 0xF090,
        KEYMENU = 0xF100,
        ARRANGE = 0xF110,
        RESTORE = 0xF120,
        TASKLIST = 0xF130,
        SCREENSAVE = 0xF140,
        HOTKEY = 0xF150,
        DEFAULT = 0xF160,
        MONITORPOWER = 0xF170,
        CONTEXTHELP = 0xF180,
        SEPARATOR = 0xF00F,

        /// <summary>
        /// SCF_ISSECURE
        /// </summary>
        F_ISSECURE = 0x00000001,
        ICON = MINIMIZE,
        ZOOM = MAXIMIZE,
    }
}
