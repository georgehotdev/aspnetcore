// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;
using PhotinoNET;

namespace Microsoft.AspNetCore.Components.WebView.Photino;

internal class PhotinoWebViewManager : WebViewManager
{
    private readonly PhotinoWindow _window;

    // On Windows, we can't use a custom scheme to host the initial HTML,
    // because webview2 won't let you do top-level navigation to such a URL.
    // On Linux/Mac, we must use a custom scheme, because their webviews
    // don't have a way to intercept http:// scheme requests.
    internal static readonly string BlazorAppScheme = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "http"
        : "app";

    internal static readonly string AppBaseUri
        = $"{BlazorAppScheme}://0.0.0.0/";

    public PhotinoWebViewManager(PhotinoWindow window, IServiceProvider provider, Dispatcher dispatcher, Uri appBaseUri, IFileProvider fileProvider, JSComponentConfigurationStore jsComponents, string hostPageRelativePath)
        : base(provider, dispatcher, appBaseUri, fileProvider, jsComponents, hostPageRelativePath)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _window.WebMessageReceived += (sender, message) =>
        {
            // On some platforms, we need to move off the browser UI thread
            Task.Factory.StartNew(message =>
            {
                // TODO: Fix this. Photino should ideally tell us the URL that the message comes from so we
                // know whether to trust it. Currently it's hardcoded to trust messages from any source, including
                // if the webview is somehow navigated to an external URL.
                var messageOriginUrl = new Uri(AppBaseUri);

                MessageReceived(messageOriginUrl, (string)message!);
            }, message, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        };
    }

    public Stream? HandleWebRequest(string url, out string? contentType)
    {
        // It would be better if we were told whether or not this is a navigation request, but
        // since we're not, guess.
        var hasFileExtension = url.LastIndexOf('.') > url.LastIndexOf('/');

        if (url.StartsWith(AppBaseUri, StringComparison.Ordinal)
            && TryGetResponseContent(url, !hasFileExtension, out var statusCode, out var statusMessage, out var content, out var headers))
        {
            headers.TryGetValue("Content-Type", out contentType);
            return content;
        }
        else
        {
            contentType = default;
            return null;
        }
    }

    protected override void NavigateCore(Uri absoluteUri)
    {
        _window.Load(absoluteUri);
    }

    protected override void SendMessage(string message)
    {
        _window.SendWebMessage(message);
    }
}
