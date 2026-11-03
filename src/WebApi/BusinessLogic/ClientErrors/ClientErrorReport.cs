namespace WebApi.BusinessLogic.ClientErrors;

/// <summary>An error from a visitor's browser, sent by frontend/src/utils/errorReporting.ts.</summary>
/// <param name="Kind">What caught it: <c>error</c> (window.onerror), <c>unhandledrejection</c> or <c>render</c> (the root error boundary)</param>
/// <param name="Name">The error's name, e.g. <c>TypeError</c></param>
/// <param name="Message">The error's message</param>
/// <param name="Stack">The stack trace, if the browser gave one</param>
/// <param name="Page">The page's path, e.g. <c>/game/2026020123</c></param>
public record ClientErrorReport(string Kind, string? Name, string Message, string? Stack, string? Page);
