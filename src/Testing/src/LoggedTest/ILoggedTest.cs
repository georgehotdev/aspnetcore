﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Testing;

public interface ILoggedTest : IDisposable
{
    ILogger Logger { get; }

    ILoggerFactory LoggerFactory { get; }

    ITestOutputHelper TestOutputHelper { get; }

    // For back compat
    IDisposable StartLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, string testName);

    void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper);
}
