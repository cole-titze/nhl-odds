namespace WebApi.BusinessLogic.ClientErrors;

/// <summary>An error from a visitor's browser, sent by frontend/src/utils/errorReporting.ts.</summary>
/// <param name="Kind">What caught it: <c>error</c> (window.onerror), <c>unhandledrejection</c>, <c>render</c> (the root error
/// boundary), <c>api</c> (an API call that got a 5xx or no answer) or <c>chunk</c> (a code file that failed to load, or that
/// frontend/client-errors.mjs saw requested from an older build)</param>
/// <param name="Name">The error's name, e.g. <c>TypeError</c></param>
/// <param name="Message">The error's message</param>
/// <param name="Stack">The stack trace, if the browser gave one; frontend/client-errors.mjs maps it to the source first</param>
/// <param name="Page">The page's path, e.g. <c>/game/2026020123</c></param>
/// <param name="Version">The frontend build that sent it (the site footer's "Build 2026.11.03-abc1234")</param>
public record ClientErrorReport(string Kind, string? Name, string Message, string? Stack, string? Page, string? Version = null);
