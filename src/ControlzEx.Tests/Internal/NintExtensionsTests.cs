// --------------------------------------------------------------------------------------------------------------------
// <copyright company="PROSOZ Herten">
// (C) 2026 PROSOZ Herten GmbH. Diese Datei enthält vertrauliche und gesetzlich geschützte Informationen der PROSOZ Herten GmbH. Jegliche Reproduktion oder Veröffentlichung im Ganzen oder auch teilweise ist ausdrücklich verboten, es sei denn dies wurde spezifisch und in Schriftform durch die PROSOZ Herten GmbH autorisiert.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ControlzEx.Tests.Internal;

using System;
using ControlzEx.Internal;
using NUnit.Framework;

public class NintExtensionsTests
{
    [Test]
    public void TestIsZero()
    {
        Assert.That(((nint)0).IsZero(), Is.True);
        Assert.That(((nuint)0).IsZero(), Is.True);
        Assert.That(((IntPtr)0).IsZero(), Is.True);
        Assert.That(((UIntPtr)0).IsZero(), Is.True);

        Assert.That(((nint)1).IsZero(), Is.False);
        Assert.That(((nuint)1).IsZero(), Is.False);
        Assert.That(((IntPtr)1).IsZero(), Is.False);
        Assert.That(((UIntPtr)1).IsZero(), Is.False);
    }
}