const clarityFunctionName = "clarity";
const clarityProviderName = "clarity";
const clarityScriptAttribute = "prompterone-clarity";
const clarityScriptBaseUrl = "https://www.clarity.ms/tag/";
const googleAnalyticsFunctionName = "gtag";
const googleAnalyticsProviderName = "google-analytics";
const googleAnalyticsScriptAttribute = "prompterone-google-analytics";
const googleAnalyticsScriptBaseUrl = "https://www.googletagmanager.com/gtag/js?id=";
const googleDataLayerName = "dataLayer";
const harnessEventsKey = "events";
const harnessInitializationsKey = "initializations";
const harnessName = "__prompterOneTelemetryHarness";
const harnessPageViewsKey = "pageViews";
const harnessVendorLoadsKey = "vendorLoads";
const runtimeName = "__prompterOneRuntime";
const scriptAttributeName = "data-prompterone-telemetry";
const scriptTagName = "script";

const telemetryState = {
    clarityConfigured: false,
    clarityProjectId: "",
    googleAnalyticsConfigured: false,
    googleAnalyticsMeasurementId: "",
    initialized: false,
    runtimeEnabled: false
};

function getHarness() {
    return window[harnessName] ?? null;
}

function getRuntime() {
    return window[runtimeName] ?? {};
}

function ensureHarnessCollection(name) {
    const harness = getHarness();
    if (harness === null) {
        return null;
    }

    if (!Array.isArray(harness[name])) {
        harness[name] = [];
    }

    return harness[name];
}

function recordHarnessEntry(name, payload) {
    const collection = ensureHarnessCollection(name);
    if (collection !== null) {
        collection.push(payload);
    }
}

function shouldBlockVendorScripts() {
    const harness = getHarness();
    return harness?.blockVendorScripts === true;
}

function installGoogleAnalyticsStub() {
    window[googleDataLayerName] = window[googleDataLayerName] ?? [];

    if (typeof window[googleAnalyticsFunctionName] !== "function") {
        window[googleAnalyticsFunctionName] = function () {
            window[googleDataLayerName].push(arguments);
        };
    }
}

function installClarityStub() {
    if (typeof window[clarityFunctionName] === "function") {
        return;
    }

    const clarityStub = function () {
        clarityStub.q = clarityStub.q ?? [];
        clarityStub.q.push(arguments);
    };

    window[clarityFunctionName] = clarityStub;
}

async function loadVendorScript(url, attributeValue) {
    const existing = document.querySelector(`${scriptTagName}[${scriptAttributeName}="${attributeValue}"]`);
    if (existing) {
        return;
    }

    await new Promise(resolve => {
        const script = document.createElement(scriptTagName);
        script.async = true;
        script.src = url;
        script.setAttribute(scriptAttributeName, attributeValue);
        script.onload = () => resolve();
        script.onerror = () => resolve();

        const firstScript = document.getElementsByTagName(scriptTagName)[0];
        if (firstScript?.parentNode) {
            firstScript.parentNode.insertBefore(script, firstScript);
            return;
        }

        document.head.appendChild(script);
        resolve();
    });
}

async function ensureGoogleAnalyticsConfigured() {
    if (!telemetryState.runtimeEnabled || !telemetryState.googleAnalyticsMeasurementId || telemetryState.googleAnalyticsConfigured) {
        return;
    }

    installGoogleAnalyticsStub();

    const scriptUrl = `${googleAnalyticsScriptBaseUrl}${telemetryState.googleAnalyticsMeasurementId}`;
    const blocked = shouldBlockVendorScripts();
    recordHarnessEntry(harnessVendorLoadsKey, {
        blocked,
        provider: googleAnalyticsProviderName,
        url: scriptUrl
    });

    if (!blocked) {
        await loadVendorScript(scriptUrl, googleAnalyticsScriptAttribute);
    }

    window[googleAnalyticsFunctionName]("js", new Date());
    window[googleAnalyticsFunctionName]("config", telemetryState.googleAnalyticsMeasurementId, { send_page_view: false });
    telemetryState.googleAnalyticsConfigured = true;
}

async function ensureClarityConfigured() {
    if (!telemetryState.runtimeEnabled || !telemetryState.clarityProjectId || telemetryState.clarityConfigured) {
        return;
    }

    installClarityStub();

    const scriptUrl = `${clarityScriptBaseUrl}${telemetryState.clarityProjectId}`;
    const blocked = shouldBlockVendorScripts();
    recordHarnessEntry(harnessVendorLoadsKey, {
        blocked,
        provider: clarityProviderName,
        url: scriptUrl
    });

    if (!blocked) {
        await loadVendorScript(scriptUrl, clarityScriptAttribute);
    }

    telemetryState.clarityConfigured = true;
}

function buildInitializationSnapshot(config) {
    const runtime = getRuntime();
    const sentryConfigured = config?.sentryConfigured === true;

    return {
        clarityConfigured: Boolean(config?.clarityProjectId),
        debugEnabled: runtime.wasmDebugEnabled === true,
        googleAnalyticsConfigured: Boolean(config?.googleAnalyticsMeasurementId),
        hostEnabled: config?.hostEnabled === true,
        runtimeEnabled: telemetryState.runtimeEnabled,
        sentryConfigured,
        sentryRuntimeEnabled: sentryConfigured && telemetryState.runtimeEnabled
    };
}

function normalizePayload(eventName, payload) {
    const safePayload = payload && typeof payload === "object" ? payload : {};
    return {
        eventName,
        ...safePayload
    };
}

export async function initializeRuntimeTelemetry(config) {
    telemetryState.initialized = true;
    telemetryState.googleAnalyticsMeasurementId = config?.googleAnalyticsMeasurementId ?? "";
    telemetryState.clarityProjectId = config?.clarityProjectId ?? "";
    telemetryState.runtimeEnabled = config?.hostEnabled === true && getRuntime().wasmDebugEnabled !== true;

    recordHarnessEntry(harnessInitializationsKey, buildInitializationSnapshot(config));

    if (!telemetryState.runtimeEnabled) {
        return false;
    }

    await ensureGoogleAnalyticsConfigured();
    await ensureClarityConfigured();
    return true;
}

export async function trackRuntimeTelemetryPageView(eventName, payload) {
    return trackRuntimeTelemetryEventInternal(eventName, payload, harnessPageViewsKey);
}

export async function trackRuntimeTelemetryEvent(eventName, payload) {
    return trackRuntimeTelemetryEventInternal(eventName, payload, harnessEventsKey);
}

async function trackRuntimeTelemetryEventInternal(eventName, payload, harnessCollectionName) {
    if (!telemetryState.initialized || !telemetryState.runtimeEnabled || !telemetryState.googleAnalyticsMeasurementId) {
        return false;
    }

    await ensureGoogleAnalyticsConfigured();

    const normalizedPayload = normalizePayload(eventName, payload);
    recordHarnessEntry(harnessCollectionName, normalizedPayload);
    window[googleAnalyticsFunctionName]("event", eventName, payload ?? {});
    return true;
}
