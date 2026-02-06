// --------------------------------------------------------------------------------------------------------------------
// <copyright company="PROSOZ Herten">
// (C) 2026 PROSOZ Herten GmbH. Diese Datei enthält vertrauliche und gesetzlich geschützte Informationen der PROSOZ Herten GmbH. Jegliche Reproduktion oder Veröffentlichung im Ganzen oder auch teilweise ist ausdrücklich verboten, es sei denn dies wurde spezifisch und in Schriftform durch die PROSOZ Herten GmbH autorisiert.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ControlzEx.Internal;

using System;

public static class NintExtensions
{
    public static bool IsZero(this nint value)
    {
        return value is 0;
    }

    [CLSCompliant(false)]
    public static bool IsZero(this nuint value)
    {
        return value is 0;
    }
}