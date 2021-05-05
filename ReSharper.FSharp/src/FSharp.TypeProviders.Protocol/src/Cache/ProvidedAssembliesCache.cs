﻿using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
{
  internal class ProvidedAssembliesCache : ProvidedEntitiesCacheBase<ProvidedAssembly>
  {
    private RdProvidedAssemblyProcessModel ProvidedAssembliesProcessModel =>
      TypeProvidersContext.Connection.ProtocolModel.RdProvidedAssemblyProcessModel;

    public ProvidedAssembliesCache(TypeProvidersContext typeProvidersContext) : base(typeProvidersContext)
    {
    }

    protected override ProvidedAssembly Create(int key, int typeProviderId, ProvidedTypeContextHolder context)
      => ProxyProvidedAssembly.Create(
        TypeProvidersContext.Connection.ExecuteWithCatch(() =>
          ProvidedAssembliesProcessModel.GetProvidedAssembly.Sync(key)),
        TypeProvidersContext.Connection);

    protected override ProvidedAssembly[] CreateBatch(int[] keys, int typeProviderId, ProvidedTypeContextHolder context)
      => throw new System.NotSupportedException();

    public override string Dump() =>
      "Provided Assemblies:\n" + string.Join("\n",
        Entities
          .Select(t => (t.Key, Name: t.Value.GetLogName()))
          .OrderBy(t => t.Name)
          .Select(t => $"{t.Key} {t.Name}"));
  }
}