(() => {
    const defaultCulture = "en";
    const namespace = "prompterOneCulture";

    function getBrowserLanguages() {
        const browserLanguages = Array.isArray(window.navigator.languages) && window.navigator.languages.length > 0
            ? window.navigator.languages
            : [window.navigator.language || defaultCulture];

        return browserLanguages
            .filter(value => typeof value === "string")
            .map(value => value.trim())
            .filter(value => value.length > 0);
    }

    function setDocumentLanguage(cultureName) {
        const resolvedCulture = typeof cultureName === "string" && cultureName.trim().length > 0
            ? cultureName.trim()
            : defaultCulture;
        document.documentElement.lang = resolvedCulture;
    }

    window[namespace] = Object.freeze({
        getBrowserLanguages,
        setDocumentLanguage
    });
})();
