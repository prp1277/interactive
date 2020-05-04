﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.App.HttpRouting
{
    public class KernelHub : Hub
    {
        private readonly CompositeKernel _kernel;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public KernelHub(CompositeKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _disposables.Add( _kernel.KernelEvents.Subscribe(onNext: async kernelEvent => await PublishEvent(kernelEvent)) );
        }

        public async Task SubmitCode(string code, string kernelName)
        {
            var command = new SubmitCode(code, targetKernelName:kernelName);
            var token = command.GetToken();
            await Clients.Caller.SendAsync("commandSubmitted", token);
            await _kernel.SendAsync(command);
        }

        private async Task PublishEvent(IKernelEvent kernelEvent)
        {
            var eventEnvelope = KernelEventEnvelope.Create(kernelEvent);
            await Clients.All.SendAsync("onKernelEvent", KernelEventEnvelope.Serialize(eventEnvelope));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposables.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}