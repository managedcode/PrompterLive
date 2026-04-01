(() => {
    const settingsPrefix = "prompterone.settings.";
    const settingsPageKey = `${settingsPrefix}prompterone.settings-page`;
    const darkTheme = "dark";
    const lightTheme = "light";
    const systemTheme = "system";
    const defaultAccent = "#C4A060";
    const defaultDensity = "default";
    const root = document.documentElement;
    const themeMedia = typeof window.matchMedia === "function"
        ? window.matchMedia("(prefers-color-scheme: light)")
        : null;
    let selectedTheme = darkTheme;

    function clampColor(value) {
        return Math.max(0, Math.min(255, Math.round(value)));
    }

    function setBodyClass(className, enabled) {
        if (document.body) {
            document.body.classList.toggle(className, enabled);
        }
    }

    function buildColor(red, green, blue) {
        return `rgb(${red}, ${green}, ${blue})`;
    }

    function buildRgba(red, green, blue, alpha) {
        return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
    }

    function hexToRgb(hex) {
        const match = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex ?? "");
        if (!match) {
            return null;
        }

        return {
            red: Number.parseInt(match[1], 16),
            green: Number.parseInt(match[2], 16),
            blue: Number.parseInt(match[3], 16)
        };
    }

    function mixWithWhite(rgb, ratio) {
        return {
            red: clampColor(rgb.red + ((255 - rgb.red) * ratio)),
            green: clampColor(rgb.green + ((255 - rgb.green) * ratio)),
            blue: clampColor(rgb.blue + ((255 - rgb.blue) * ratio))
        };
    }

    function mixWithBlack(rgb, ratio) {
        return {
            red: clampColor(rgb.red * (1 - ratio)),
            green: clampColor(rgb.green * (1 - ratio)),
            blue: clampColor(rgb.blue * (1 - ratio))
        };
    }

    function resolveEffectiveTheme(theme) {
        if (theme === systemTheme) {
            return themeMedia?.matches ? lightTheme : darkTheme;
        }

        return theme === lightTheme ? lightTheme : darkTheme;
    }

    function setAccentTokens(accentHex, effectiveTheme) {
        const rgb = hexToRgb(accentHex) ?? hexToRgb(defaultAccent);
        if (!rgb) {
            return;
        }

        const accentText = effectiveTheme === lightTheme
            ? mixWithBlack(rgb, 0.35)
            : mixWithWhite(rgb, 0.45);
        const accentLight = effectiveTheme === lightTheme
            ? mixWithBlack(rgb, 0.12)
            : mixWithWhite(rgb, 0.72);
        const accentMid = effectiveTheme === lightTheme
            ? mixWithBlack(rgb, 0.22)
            : mixWithWhite(rgb, 0.28);

        root.style.setProperty("--accent-color", accentHex);
        root.style.setProperty("--accent-rgb", `${rgb.red}, ${rgb.green}, ${rgb.blue}`);
        root.style.setProperty("--gold", accentHex);
        root.style.setProperty("--accent", accentHex);
        root.style.setProperty("--gold-text", buildColor(accentText.red, accentText.green, accentText.blue));
        root.style.setProperty("--gold-light", buildColor(accentLight.red, accentLight.green, accentLight.blue));
        root.style.setProperty("--gold-mid", buildColor(accentMid.red, accentMid.green, accentMid.blue));
        root.style.setProperty(
            "--gold-gradient",
            `linear-gradient(135deg, ${buildColor(accentLight.red, accentLight.green, accentLight.blue)}, ${buildColor(accentMid.red, accentMid.green, accentMid.blue)})`);

        [
            ["--gold-03", 0.03],
            ["--gold-04", 0.04],
            ["--gold-05", 0.05],
            ["--gold-06", 0.06],
            ["--gold-07", effectiveTheme === lightTheme ? 0.08 : 0.07],
            ["--gold-08", effectiveTheme === lightTheme ? 0.10 : 0.08],
            ["--gold-09", effectiveTheme === lightTheme ? 0.12 : 0.09],
            ["--gold-10", effectiveTheme === lightTheme ? 0.14 : 0.10],
            ["--gold-12", effectiveTheme === lightTheme ? 0.18 : 0.12],
            ["--gold-14", effectiveTheme === lightTheme ? 0.22 : 0.14],
            ["--gold-15", effectiveTheme === lightTheme ? 0.25 : 0.15],
            ["--gold-16", effectiveTheme === lightTheme ? 0.28 : 0.16],
            ["--gold-20", effectiveTheme === lightTheme ? 0.32 : 0.20],
            ["--gold-25", effectiveTheme === lightTheme ? 0.38 : 0.25],
            ["--gold-30", effectiveTheme === lightTheme ? 0.45 : 0.30],
            ["--gold-35", effectiveTheme === lightTheme ? 0.52 : 0.35],
            ["--gold-45", effectiveTheme === lightTheme ? 0.65 : 0.45]
        ].forEach(([token, alpha]) => root.style.setProperty(token, buildRgba(rgb.red, rgb.green, rgb.blue, alpha)));
    }

    function applySettingsTheme(theme, accentColor, density) {
        selectedTheme = theme ?? darkTheme;
        const effectiveTheme = resolveEffectiveTheme(selectedTheme);

        root.setAttribute("data-theme", effectiveTheme);
        root.setAttribute("data-theme-source", selectedTheme);
        root.setAttribute("data-density", density ?? defaultDensity);
        root.classList.toggle("theme-light", effectiveTheme === lightTheme);
        root.classList.toggle("theme-dark", effectiveTheme === darkTheme);
        setBodyClass("theme-light", effectiveTheme === lightTheme);
        setBodyClass("theme-dark", effectiveTheme === darkTheme);
        setAccentTokens(accentColor ?? defaultAccent, effectiveTheme);
    }

    function loadStoredPreferences() {
        try {
            const stored = window.localStorage.getItem(settingsPageKey);
            if (!stored) {
                applySettingsTheme(darkTheme, defaultAccent, defaultDensity);
                return;
            }

            const preferences = JSON.parse(stored);
            applySettingsTheme(
                preferences?.ColorScheme ?? darkTheme,
                preferences?.AccentColor ?? defaultAccent,
                preferences?.UiDensity ?? defaultDensity);
        } catch {
            applySettingsTheme(darkTheme, defaultAccent, defaultDensity);
        }
    }

    if (themeMedia) {
        themeMedia.addEventListener("change", () => {
            if (selectedTheme === systemTheme) {
                loadStoredPreferences();
            }
        });
    }

    window.prompterOneTheme = {
        applySettingsTheme
    };

    loadStoredPreferences();
})();
