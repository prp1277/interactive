﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public class KeyValueStoreKernel :
        DotNetKernel,
        IKernelCommandHandler<SubmitCode>
    {
        internal const string DefaultKernelName = "value";

        private readonly ConcurrentDictionary<string, object> _values = new ConcurrentDictionary<string, object>();

        public KeyValueStoreKernel() : base(DefaultKernelName)
        {
        }

        public override Task SetVariableAsync(string name, object value)
        {
            _values[name] = value;
            return Task.CompletedTask;
        }

        public override IReadOnlyCollection<string> GetVariableNames() =>
            _values.Keys.ToArray();

        public override bool TryGetVariable<T>(string name, out T value)
        {
            if (_values.TryGetValue(name, out var obj) &&
                obj is T t)
            {
                value = t;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            if (command.KernelNameDirectiveNode is null)
            {
                context.Fail(message: $"Missing required #!{Name} details.");

                return;
            }

            var parseResult = command.KernelNameDirectiveNode.GetDirectiveParseResult();

            var name = parseResult.ValueForOption<string>("--name");

            var value = command.LanguageNode.Text.Trim();

            await SetVariableAsync(name, value);

            if (parseResult.ValueForOption("--mime-type") is string mimeType)
            {
                await context.DisplayAsync(value, mimeType);
            }
        }

        protected internal override ChooseKernelDirective CreateChooseKernelDirective() =>
            new ChooseKernelDirective(this, "Stores a value ")
            {
                new Option<string>(
                    "--name", 
                    "The name of the value to create. You can use #!share to retrieve this value from another subkernel.")
                {
                    IsRequired = true
                },
                new Option<string>("--mime-type", "A mime type for the value. If specified, displays the value immediately as a cell output using the specified mime type.")
                    .AddSuggestions(_ => new[]
                    {
                        "application/json",
                        "text/html",
                        "text/plain",
                        "text/csv"
                    })
            };
    }
}