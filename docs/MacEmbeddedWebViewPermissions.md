# macOS Embedded WebView Media Permissions

This repo is currently browser-first Blazor WebAssembly only. There is no active macOS embedded browser host in `src/`.

This note exists so that if a Mac host or `BlazorWebView` returns later, camera and microphone access is implemented correctly the first time.

## What Goes Wrong

On macOS, there are two permission layers:

- the system permission granted to the app
- the web-content permission granted inside `WKWebView`

If the app only asks for the system permission and ignores the `WKWebView` layer, `getUserMedia()` can keep prompting on each launch.

This usually happens when:

- a fresh `WKWebView` is created every run without a stable configuration
- `requestMediaCapturePermissionFor` is not handled
- `WKWebsiteDataStore.nonPersistent()` is used
- the embedded origin changes between launches, for example due to random localhost ports

## Required Rules

If a native Mac host is reintroduced:

1. Add `NSCameraUsageDescription` and `NSMicrophoneUsageDescription` to `Info.plist`.
2. Keep the web origin stable across launches.
3. Use `WKWebsiteDataStore.default()`, not `.nonPersistent()`.
4. Assign a `WKUIDelegate`.
5. Implement `webView(_:requestMediaCapturePermissionFor:initiatedByFrame:type:decisionHandler:)`.
6. Auto-grant trusted origins after the app already has system-level permission.

## Reference Shape

```swift
final class WebViewPermissionDelegate: NSObject, WKUIDelegate {
    func webView(
        _ webView: WKWebView,
        requestMediaCapturePermissionFor origin: WKSecurityOrigin,
        initiatedByFrame frame: WKFrameInfo,
        type: WKMediaCaptureType,
        decisionHandler: @escaping (WKPermissionDecision) -> Void
    ) {
        let trustedHosts = ["localhost", "yourdomain.com"]

        if trustedHosts.contains(origin.host) {
            decisionHandler(.grant)
        } else {
            decisionHandler(.prompt)
        }
    }
}

let configuration = WKWebViewConfiguration()
configuration.websiteDataStore = .default()

let webView = WKWebView(frame: .zero, configuration: configuration)
webView.uiDelegate = permissionDelegate
```

## Repo Policy

- Do not introduce a Mac embedded host that depends on random ports.
- Do not assume system-level permission is enough for embedded web content.
- Do not use an ephemeral website data store for camera or microphone scenarios.
- If the host is added later, capture this behavior in automated host-level tests before calling the integration complete.
