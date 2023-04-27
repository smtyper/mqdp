using System;
using Microsoft.Extensions.Hosting;

namespace Workers;

public interface IConfigurableHostedService : IHostedService, IDisposable
{
}
