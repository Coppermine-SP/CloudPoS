namespace CloudInteractive.CloudPos.Services.Debounce;

public sealed record DebouncePolicy(
    TimeSpan Debounce,
    TimeSpan MaxInterval
);
