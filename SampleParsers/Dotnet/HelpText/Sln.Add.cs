// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Cli.CommandLine.SampleParsers.Dotnet.HelpText
{
    public partial class Sln
    {
        public static class Add
        {
            public const string HelpText = @".NET Add project(s) to a solution file Command

Usage: dotnet sln <SLN_FILE> add [options] [args]

Arguments:
  <SLN_FILE>  Solution file to operate on. If not specified, the command will search the current directory for one.

Options:
  -h|--help  Show help information

Additional Arguments:
 Add a specified project(s) to the solution.";
        }
    }
}