﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
  public class ProxyProvidedType : ProvidedType, IRdProvidedEntity
  {
    private record ProvidedTypeContent(
      ProxyProvidedType[] Interfaces,
      ProxyProvidedConstructorInfo[] Constructors,
      ProxyProvidedMethodInfo[] Methods,
      ProxyProvidedPropertyInfo[] Properties,
      ProxyProvidedFieldInfo[] Fields,
      ProxyProvidedEventInfo[] Events);

    private readonly RdOutOfProcessProvidedType myRdProvidedType;
    private readonly int myTypeProviderId;
    private readonly TypeProvidersContext myTypeProvidersContext;
    public int EntityId => myRdProvidedType.EntityId;
    public RdProvidedEntityType EntityType => RdProvidedEntityType.TypeInfo;

    private RdProvidedTypeProcessModel RdProvidedTypeProcessModel =>
      myTypeProvidersContext.Connection.ProtocolModel.RdProvidedTypeProcessModel;

    private ProxyProvidedType(RdOutOfProcessProvidedType rdProvidedType, int typeProviderId,
      TypeProvidersContext typeProvidersContext) : base(null, ProvidedConst.EmptyContext)
    {
      myRdProvidedType = rdProvidedType;
      myTypeProviderId = typeProviderId;
      myTypeProvidersContext = typeProvidersContext;

      myGenericArguments = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(myRdProvidedType.GenericArguments, typeProviderId));

      myStaticParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(() => myTypeProvidersContext.Connection
        .ExecuteWithCatch(() => RdProvidedTypeProcessModel.GetStaticParameters.Sync(EntityId, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedParameterInfo.Create(t, myTypeProviderId, myTypeProvidersContext))
        .ToArray());

      myCustomAttributes = new InterruptibleLazy<RdCustomAttributeData[]>(() =>
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetCustomAttributes(this));

      myContent = new InterruptibleLazy<ProvidedTypeContent>(() =>
      {
        var rdProvidedTypeContent = myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
          RdProvidedTypeProcessModel.GetContent.Sync(EntityId, RpcTimeouts.Maximal));

        var interfaces = myTypeProvidersContext.ProvidedTypesCache
          .GetOrCreateBatch(rdProvidedTypeContent.Interfaces, typeProviderId);

        var constructors = rdProvidedTypeContent.Constructors
          .Select(t => ProxyProvidedConstructorInfo.Create(t, myTypeProviderId, myTypeProvidersContext))
          .ToArray();

        var methods = rdProvidedTypeContent.Methods
          .Select(t => ProxyProvidedMethodInfo.Create(t, typeProviderId, typeProvidersContext))
          .ToArray();

        var properties = rdProvidedTypeContent.Properties
          .Select(t => ProxyProvidedPropertyInfo.Create(t, myTypeProviderId, myTypeProvidersContext))
          .ToArray();

        var fields = rdProvidedTypeContent.Fields
          .Select(t => ProxyProvidedFieldInfo.Create(t, myTypeProviderId, typeProvidersContext))
          .ToArray();

        var events = rdProvidedTypeContent.Events
          .Select(t => ProxyProvidedEventInfo.Create(t, myTypeProviderId, myTypeProvidersContext))
          .ToArray();

        return new ProvidedTypeContent(interfaces, constructors, methods, properties, fields, events);
      });

      myAllNestedTypes = new InterruptibleLazy<ProxyProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetAllNestedTypes.Sync(EntityId, RpcTimeouts.Maximal)), typeProviderId));
    }

    [ContractAnnotation("type:null => null")]
    public static ProxyProvidedType Create(RdOutOfProcessProvidedType type, int typeProviderId,
      TypeProvidersContext typeProvidersContext) =>
      type == null ? null : new ProxyProvidedType(type, typeProviderId, typeProvidersContext);

    public override string Name => myRdProvidedType.Name;
    public override string FullName => myRdProvidedType.FullName;
    public override string Namespace => myRdProvidedType.Namespace;

    public override bool IsGenericParameter => HasFlag(RdProvidedTypeFlags.IsGenericParameter);
    public override bool IsValueType => HasFlag(RdProvidedTypeFlags.IsValueType);
    public override bool IsByRef => HasFlag(RdProvidedTypeFlags.IsByRef);
    public override bool IsPointer => HasFlag(RdProvidedTypeFlags.IsPointer);
    public override bool IsPublic => HasFlag(RdProvidedTypeFlags.IsPublic);
    public override bool IsNestedPublic => HasFlag(RdProvidedTypeFlags.IsNestedPublic);
    public override bool IsArray => HasFlag(RdProvidedTypeFlags.IsArray);
    public override bool IsEnum => HasFlag(RdProvidedTypeFlags.IsEnum);
    public override bool IsClass => HasFlag(RdProvidedTypeFlags.IsClass);
    public override bool IsSealed => HasFlag(RdProvidedTypeFlags.IsSealed);
    public override bool IsAbstract => HasFlag(RdProvidedTypeFlags.IsAbstract);
    public override bool IsInterface => HasFlag(RdProvidedTypeFlags.IsInterface);
    public override bool IsSuppressRelocate => HasFlag(RdProvidedTypeFlags.IsSuppressRelocate);
    public override bool IsErased => HasFlag(RdProvidedTypeFlags.IsErased);
    public override bool IsGenericType => HasFlag(RdProvidedTypeFlags.IsGenericType);
    public override bool IsVoid => HasFlag(RdProvidedTypeFlags.IsVoid);
    public override bool IsMeasure => HasFlag(RdProvidedTypeFlags.IsMeasure);

    public override int GenericParameterPosition =>
      myGenericParameterPosition ??=
        myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
          RdProvidedTypeProcessModel.GenericParameterPosition.Sync(myRdProvidedType.EntityId));

    public override ProvidedType BaseType =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(myRdProvidedType.BaseType, myTypeProviderId);

    public override ProvidedType DeclaringType =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(myRdProvidedType.DeclaringType, myTypeProviderId);

    public override ProvidedType GetNestedType(string nm) => GetAllNestedTypes().FirstOrDefault(t => t.Name == nm);

    public override ProvidedType[] GetNestedTypes() =>
      GetAllNestedTypes().Where(t => t.IsPublic || t.IsNestedPublic).ToArray();

    public override ProvidedType[] GetAllNestedTypes() => myAllNestedTypes.Value;

    public override ProvidedType GetGenericTypeDefinition() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myGenericTypeDefinitionId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetGenericTypeDefinition.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedPropertyInfo[] GetProperties() => myContent.Value.Properties;

    public override ProvidedPropertyInfo GetProperty(string nm) => GetProperties().FirstOrDefault(t => t.Name == nm);

    public override int GetArrayRank() =>
      myArrayRank ??=
        myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
          RdProvidedTypeProcessModel.GetArrayRank.Sync(EntityId));

    public override ProvidedType GetElementType() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myElementTypeId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetElementType.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    public override ProvidedType GetEnumUnderlyingType() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myEnumUnderlyingTypeId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.GetEnumUnderlyingType.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedParameterInfo[] GetStaticParameters(ITypeProvider provider) => myStaticParameters.Value;

    public override ProvidedType ApplyStaticArguments(ITypeProvider provider, string[] fullTypePathAfterArguments,
      object[] staticArgs) => IsErased
      ? ApplyStaticArguments(fullTypePathAfterArguments, staticArgs)
      : ApplyStaticArgumentsGenerative(fullTypePathAfterArguments, staticArgs);

    private ProvidedType ApplyStaticArguments(string[] fullTypePathAfterArguments, object[] staticArgs)
    {
      var staticArgDescriptions = staticArgs.Select(PrimitiveTypesBoxer.BoxToServerStaticArg).ToArray();
      return myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myTypeProvidersContext.Connection.ExecuteWithCatch(
          () => RdProvidedTypeProcessModel.ApplyStaticArguments.Sync(
            new ApplyStaticArgumentsParameters(EntityId, fullTypePathAfterArguments, staticArgDescriptions),
            RpcTimeouts.Maximal)),
        myTypeProviderId);
    }

    // Since we distinguish different generative types by assembly name
    // and, at the same time, even for the same types, the generated assembly names will be different,
    // we will cache such types on the ReSharper side to avoid leaks.
    private ProvidedType ApplyStaticArgumentsGenerative(string[] fullTypePathAfterArguments, object[] staticArgs)
    {
      var key = string.Join(".", fullTypePathAfterArguments) + "+" + string.Join(",", staticArgs);
      var staticArgDescriptions = staticArgs.Select(PrimitiveTypesBoxer.BoxToServerStaticArg).ToArray();

      return myTypeProvidersContext.AppliedProvidedTypesCache.GetOrCreate((EntityId, key), myTypeProviderId,
        new ApplyStaticArgumentsParameters(EntityId, fullTypePathAfterArguments, staticArgDescriptions));
    }

    public override ProvidedType[] GetInterfaces() => myContent.Value.Interfaces;

    public override ProvidedMethodInfo[] GetMethods() => myContent.Value.Methods;

    public override ProvidedType MakeArrayType() => MakeArrayType(1);

    public override ProvidedType MakeArrayType(int rank) =>
      myTypeProvidersContext.ArrayProvidedTypesCache.GetOrCreate((EntityId, rank), myTypeProviderId,
        new MakeArrayTypeArgs(EntityId, rank));

    public override ProvidedType MakeGenericType(ProvidedType[] args)
    {
      var key = string.Join(",", args.Select(t => $"{t.Assembly.FullName} {t.FullName}"));

      var argIds = args
        .Cast<IRdProvidedEntity>()
        .Select(t => t.EntityId)
        .ToArray();

      return myTypeProvidersContext.GenericProvidedTypesCache.GetOrCreate((EntityId, key), myTypeProviderId,
        new MakeGenericTypeArgs(EntityId, argIds));
    }

    public override ProvidedType MakePointerType() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myMakePointerTypeId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.MakePointerType.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedType MakeByRefType() =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(
        myMakeByRefTypeId ??=
          myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
            RdProvidedTypeProcessModel.MakeByRefType.Sync(EntityId)),
        myTypeProviderId);

    public override ProvidedEventInfo[] GetEvents() => myContent.Value.Events;

    public override ProvidedEventInfo GetEvent(string nm) => GetEvents().FirstOrDefault(t => t.Name == nm);

    public override ProvidedFieldInfo[] GetFields() => myContent.Value.Fields;

    public override ProvidedFieldInfo GetField(string nm) => GetFields().FirstOrDefault(t => t.Name == nm);

    public override ProvidedConstructorInfo[] GetConstructors() => myContent.Value.Constructors;

    public override ProvidedType ApplyContext(ProvidedTypeContext context) =>
      ProxyProvidedTypeWithContext.Create(this, context);

    public override ProvidedAssembly Assembly => myProvidedAssembly ??=
      myTypeProvidersContext.ProvidedAssembliesCache.GetOrCreate(myRdProvidedType.Assembly, myTypeProviderId);

    public override ProvidedVar AsProvidedVar(string nm) =>
      ProxyProvidedVar.Create(nm, false, this);

    public override
      FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider _, string attribName) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetAttributeConstructorArgs(myCustomAttributes.Value,
        attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetDefinitionLocationAttribute(myCustomAttributes.Value);

    public override string[] GetXmlDocAttributes(ITypeProvider _) =>
      myXmlDocs ??=
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetXmlDocAttributes(myCustomAttributes.Value);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetHasTypeProviderEditorHideMethodsAttribute(
        myCustomAttributes.Value);

    private bool HasFlag(RdProvidedTypeFlags flag) => (myRdProvidedType.Flags & flag) == flag;

    private int? myArrayRank;
    private string[] myXmlDocs;
    private int? myMakePointerTypeId;
    private int? myMakeByRefTypeId;
    private int? myGenericParameterPosition;
    private int? myGenericTypeDefinitionId;
    private int? myElementTypeId;
    private int? myEnumUnderlyingTypeId;
    private ProvidedAssembly myProvidedAssembly;
    private readonly InterruptibleLazy<ProvidedType[]> myGenericArguments;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly InterruptibleLazy<ProvidedTypeContent> myContent;
    private readonly InterruptibleLazy<ProxyProvidedType[]> myAllNestedTypes;
    private readonly InterruptibleLazy<RdCustomAttributeData[]> myCustomAttributes;
  }
}
