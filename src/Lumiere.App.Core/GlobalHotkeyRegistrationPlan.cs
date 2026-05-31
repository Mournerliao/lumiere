using Lumiere.Settings;

namespace Lumiere.App;

public sealed record GlobalHotkeyRegistrationPlan(
    HotkeyBindingProjection Fullscreen,
    HotkeyBindingProjection Region)
{
    public static GlobalHotkeyRegistrationPlan Project(ISettingsProvider settingsProvider)
    {
        ArgumentNullException.ThrowIfNull(settingsProvider);

        var fullscreen = HotkeyBindingProjection.FromShortcut(
            GlobalHotkeyCommand.FullscreenCapture,
            "Fullscreen shortcut",
            settingsProvider.FullscreenShortcut);
        var region = HotkeyBindingProjection.FromShortcut(
            GlobalHotkeyCommand.RegionCapture,
            "Region shortcut",
            settingsProvider.RegionShortcut);

        if (fullscreen.Gesture is not null
            && region.Gesture is not null
            && fullscreen.Gesture.Equals(region.Gesture))
        {
            const string conflict = "Shortcut conflicts with another Lumiere capture command; registration is skipped.";
            fullscreen = fullscreen with { CanRegister = false, StatusMessage = conflict };
            region = region with { CanRegister = false, StatusMessage = conflict };
        }

        return new GlobalHotkeyRegistrationPlan(fullscreen, region);
    }

    public IEnumerable<HotkeyBindingProjection> RegistrableBindings()
    {
        if (Fullscreen.CanRegister)
        {
            yield return Fullscreen;
        }

        if (Region.CanRegister)
        {
            yield return Region;
        }
    }
}

public sealed record HotkeyBindingProjection(
    GlobalHotkeyCommand Command,
    string Label,
    string DisplayValue,
    HotkeyGesture? Gesture,
    bool CanRegister,
    string StatusMessage)
{
    public static HotkeyBindingProjection FromShortcut(
        GlobalHotkeyCommand command,
        string label,
        string? shortcut)
    {
        var display = MainPanelProjection.FormatShortcut(shortcut);
        if (display == "Not assigned")
        {
            return new HotkeyBindingProjection(
                command,
                label,
                display,
                Gesture: null,
                CanRegister: false,
                "No shortcut is assigned; global registration is skipped.");
        }

        if (!HotkeyGesture.TryParse(display, out var gesture, out var error))
        {
            return new HotkeyBindingProjection(
                command,
                label,
                display,
                Gesture: null,
                CanRegister: false,
                error);
        }

        return new HotkeyBindingProjection(
            command,
            label,
            gesture!.DisplayText,
            gesture,
            CanRegister: true,
            "Shortcut is valid and will be registered where Windows allows it.");
    }
}

public sealed record HotkeyGesture(
    bool Control,
    bool Shift,
    bool Alt,
    bool Windows,
    string Key,
    int VirtualKey,
    string DisplayText)
{
    public static bool TryParse(string shortcut, out HotkeyGesture? gesture, out string error)
    {
        gesture = null;
        error = string.Empty;

        var parts = shortcut
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            error = "Shortcut must include at least one modifier and one key.";
            return false;
        }

        var control = false;
        var shift = false;
        var alt = false;
        var windows = false;
        string? key = null;

        foreach (var part in parts)
        {
            if (IsControl(part))
            {
                if (control)
                {
                    error = "Shortcut contains duplicate modifier 'Ctrl'.";
                    return false;
                }

                control = true;
            }
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                if (shift)
                {
                    error = "Shortcut contains duplicate modifier 'Shift'.";
                    return false;
                }

                shift = true;
            }
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                if (alt)
                {
                    error = "Shortcut contains duplicate modifier 'Alt'.";
                    return false;
                }

                alt = true;
            }
            else if (IsWindows(part))
            {
                if (windows)
                {
                    error = "Shortcut contains duplicate modifier 'Win'.";
                    return false;
                }

                windows = true;
            }
            else if (key is null)
            {
                key = part;
            }
            else
            {
                error = "Shortcut contains more than one non-modifier key.";
                return false;
            }
        }

        if (!control && !shift && !alt && !windows)
        {
            error = "Shortcut must include Ctrl, Alt, Shift, or Win.";
            return false;
        }

        if (key is null)
        {
            error = "Shortcut is missing a key.";
            return false;
        }

        if (!TryMapVirtualKey(key, out var virtualKey, out var normalizedKey))
        {
            error = $"Shortcut key '{key}' is not supported for global registration.";
            return false;
        }

        var modifiers = new List<string>(capacity: 4);
        if (control)
        {
            modifiers.Add("Ctrl");
        }

        if (alt)
        {
            modifiers.Add("Alt");
        }

        if (shift)
        {
            modifiers.Add("Shift");
        }

        if (windows)
        {
            modifiers.Add("Win");
        }

        modifiers.Add(normalizedKey);
        gesture = new HotkeyGesture(
            control,
            shift,
            alt,
            windows,
            normalizedKey,
            virtualKey,
            string.Join("+", modifiers));
        return true;
    }

    private static bool IsControl(string value) =>
        value.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
        || value.Equals("Control", StringComparison.OrdinalIgnoreCase);

    private static bool IsWindows(string value) =>
        value.Equals("Win", StringComparison.OrdinalIgnoreCase)
        || value.Equals("Windows", StringComparison.OrdinalIgnoreCase);

    private static bool TryMapVirtualKey(string key, out int virtualKey, out string normalizedKey)
    {
        virtualKey = 0;
        normalizedKey = key.Trim().ToUpperInvariant();

        if (normalizedKey.Length == 1)
        {
            var character = normalizedKey[0];
            if (character is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                virtualKey = character;
                return true;
            }
        }

        if (normalizedKey.StartsWith('F')
            && int.TryParse(normalizedKey[1..], out var functionNumber)
            && functionNumber is >= 1 and <= 24)
        {
            virtualKey = 0x70 + functionNumber - 1;
            return true;
        }

        return false;
    }
}

public enum GlobalHotkeyCommand
{
    FullscreenCapture = 1,
    RegionCapture,
}
