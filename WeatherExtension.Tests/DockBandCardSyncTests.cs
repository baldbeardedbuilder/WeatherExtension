// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CmdPal.Ext.Weather.DockBands;
using Microsoft.CmdPal.Ext.Weather.Pages;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

/// <summary>
/// Regression tests for issue #15: dock band timer updates must refresh the
/// expanded content page by calling WeatherBandCard.LoadWeatherDataAsync().
/// Covers PinnedWeatherBand (CurrentWeatherBand was removed in issue #80).
/// </summary>
[TestClass]
public class DockBandCardSyncTests
{
    private static readonly BindingFlags PrivateInstance =
        BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly BindingFlags AnyInstance =
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    // ---------------------------------------------------------------
    // Structural: _contentPage field exists and is the right type
    // ---------------------------------------------------------------

    [TestMethod]
    public void PinnedWeatherBand_HasContentPageFieldOfTypeWeatherBandCard()
    {
        var field = typeof(PinnedWeatherBand)
            .GetField("_contentPage", PrivateInstance);

        Assert.IsNotNull(field,
            "PinnedWeatherBand must have a _contentPage field");
        Assert.AreEqual(typeof(WeatherBandCard), field!.FieldType,
            "_contentPage must be of type WeatherBandCard");
    }

    // ---------------------------------------------------------------
    // Structural: LoadWeatherDataAsync method signature
    // ---------------------------------------------------------------

    [TestMethod]
    public void WeatherBandCard_LoadWeatherDataAsync_ExistsAndReturnsTask()
    {
        var method = typeof(WeatherBandCard)
            .GetMethod("LoadWeatherDataAsync", AnyInstance);

        Assert.IsNotNull(method,
            "WeatherBandCard must expose LoadWeatherDataAsync");
        Assert.AreEqual(typeof(Task), method!.ReturnType,
            "LoadWeatherDataAsync must return Task");
    }

    [TestMethod]
    public void WeatherBandCard_LoadWeatherDataAsync_TakesNoParameters()
    {
        var method = typeof(WeatherBandCard)
            .GetMethod("LoadWeatherDataAsync", AnyInstance);

        Assert.IsNotNull(method);
        Assert.AreEqual(0, method!.GetParameters().Length,
            "LoadWeatherDataAsync should be parameterless so timers can call it repeatedly");
    }

    // ---------------------------------------------------------------
    // Structural: UpdateWeatherAsync exists on both bands
    // ---------------------------------------------------------------

    [TestMethod]
    public void PinnedWeatherBand_HasUpdateWeatherAsyncMethod()
    {
        var method = typeof(PinnedWeatherBand)
            .GetMethod("UpdateWeatherAsync", PrivateInstance);

        Assert.IsNotNull(method,
            "PinnedWeatherBand must have UpdateWeatherAsync");
        Assert.AreEqual(typeof(Task), method!.ReturnType);
    }

    // ---------------------------------------------------------------
    // Structural: timer field for periodic updates
    // ---------------------------------------------------------------

    [TestMethod]
    public void PinnedWeatherBand_HasTimerFieldForPeriodicUpdates()
    {
        var field = typeof(PinnedWeatherBand)
            .GetField("_updateTimer", PrivateInstance);

        Assert.IsNotNull(field,
            "PinnedWeatherBand must have _updateTimer for periodic refresh");
        Assert.AreEqual(typeof(System.Timers.Timer), field!.FieldType);
    }

    // ---------------------------------------------------------------
    // Re-entrancy guard: both bands have _isUpdating field
    // ---------------------------------------------------------------

    [TestMethod]
    public void PinnedWeatherBand_HasReentrancyGuard()
    {
        var field = typeof(PinnedWeatherBand)
            .GetField("_isUpdating", PrivateInstance);

        Assert.IsNotNull(field,
            "PinnedWeatherBand must have _isUpdating re-entrancy guard");
        Assert.AreEqual(typeof(int), field!.FieldType,
            "_isUpdating should be int for Interlocked operations");
    }

    // ---------------------------------------------------------------
    // IL-level: UpdateWeatherAsync CALLS LoadWeatherDataAsync
    // (the actual issue #15 fix — this is the regression guard)
    // ---------------------------------------------------------------

    [TestMethod]
    public void PinnedWeatherBand_UpdateWeatherAsync_CallsLoadWeatherDataAsync()
    {
        AssertAsyncMethodCallsTarget(
            typeof(PinnedWeatherBand),
            "UpdateWeatherAsync",
            typeof(WeatherBandCard),
            "LoadWeatherDataAsync");
    }

    // ---------------------------------------------------------------
    // Contract: LoadWeatherDataAsync is callable by both band types
    // (the _contentPage field is readonly and assigned in the ctor)
    // ---------------------------------------------------------------

    [TestMethod]
    public void PinnedWeatherBand_ContentPageField_IsReadonly()
    {
        var field = typeof(PinnedWeatherBand)
            .GetField("_contentPage", PrivateInstance);

        Assert.IsNotNull(field);
        Assert.IsTrue(field!.IsInitOnly,
            "_contentPage should be readonly to guarantee the reference is stable across timer ticks");
    }

    // ---------------------------------------------------------------
    // Helper: scan async state-machine IL to verify a target call
    // ---------------------------------------------------------------

    private static void AssertAsyncMethodCallsTarget(
        Type containingType,
        string asyncMethodName,
        Type targetType,
        string targetMethodName)
    {
        // The C# compiler emits a nested struct named
        // <<asyncMethodName>>d__N that implements IAsyncStateMachine.
        var stateMachineType = containingType
            .GetNestedTypes(BindingFlags.NonPublic)
            .FirstOrDefault(t =>
                typeof(IAsyncStateMachine).IsAssignableFrom(t) &&
                t.Name.Contains($"<{asyncMethodName}>"));

        Assert.IsNotNull(stateMachineType,
            $"Could not find the async state machine for {containingType.Name}.{asyncMethodName}");

        var moveNext = stateMachineType!
            .GetMethod("MoveNext", AnyInstance);
        Assert.IsNotNull(moveNext,
            "State machine must have a MoveNext method");

        var body = moveNext!.GetMethodBody();
        Assert.IsNotNull(body);

        var il = body!.GetILAsByteArray();
        Assert.IsNotNull(il);
        Assert.IsTrue(il!.Length >= 5,
            "IL body is too small to contain any method call");

        var found = false;

        // Scan for call (0x28) and callvirt (0x6F) opcodes.
        // Each is followed by a 4-byte metadata token.
        for (var i = 0; i < il.Length - 4; i++)
        {
            if (il[i] != 0x28 && il[i] != 0x6F)
            {
                continue;
            }

            var token = BitConverter.ToInt32(il, i + 1);

            try
            {
                var resolved = stateMachineType.Module.ResolveMethod(token);

                if (resolved?.Name == targetMethodName &&
                    resolved.DeclaringType == targetType)
                {
                    found = true;
                    break;
                }
            }
            catch
            {
                // Token may refer to a non-method member; skip.
            }
        }

        Assert.IsTrue(found,
            $"{containingType.Name}.{asyncMethodName} must call " +
            $"{targetType.Name}.{targetMethodName}() to keep the expanded " +
            "content page in sync with band timer updates (issue #15)");
    }
}
