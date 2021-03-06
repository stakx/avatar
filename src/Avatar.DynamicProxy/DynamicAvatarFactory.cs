﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;

namespace Avatars
{
    /// <summary>
    /// Provides a <see cref="IAvatarFactory"/> that creates types at run-time using Castle DynamicProxy.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DynamicAvatarFactory : IAvatarFactory
    {
        static readonly ProxyGenerator generator;
        static readonly ProxyGenerationOptions options;

        static DynamicAvatarFactory()
        {
#pragma warning disable 618
            AttributesToAvoidReplicating.Add<SecurityPermissionAttribute>();
#pragma warning restore 618

            AttributesToAvoidReplicating.Add<System.Runtime.InteropServices.MarshalAsAttribute>();
            AttributesToAvoidReplicating.Add<System.Runtime.InteropServices.TypeIdentifierAttribute>();

            options = new ProxyGenerationOptions { Hook = new ObjectMethodsHook() };
#if DEBUG
            // This allows invoking generator.ProxyBuilder.ModuleScope.SaveAssembly() for troubleshooting.
            generator = new ProxyGenerator(new DefaultProxyBuilder(new ModuleScope(true)));
#else
            generator = new ProxyGenerator();
#endif
        }

        /// <inheritdoc />
        public object CreateAvatar(Assembly assembly, Type baseType, Type[] implementedInterfaces, object?[] constructorArguments)
        {
            var notImplemented = false;

            if (baseType.IsInterface)
            {
                var fixedInterfaces = new Type[implementedInterfaces.Length + 1];
                fixedInterfaces[0] = baseType;
                implementedInterfaces.CopyTo(fixedInterfaces, 1);
                implementedInterfaces = fixedInterfaces;
                baseType = typeof(object);
                notImplemented = true;
            }

            if (!implementedInterfaces.Contains(typeof(IAvatar)))
            {
                var fixedInterfaces = new Type[implementedInterfaces.Length + 1];
                fixedInterfaces[0] = typeof(IAvatar);
                implementedInterfaces.CopyTo(fixedInterfaces, 1);
                implementedInterfaces = fixedInterfaces;
            }

            if (baseType.BaseType == typeof(MulticastDelegate))
            {
                var mixinOptions = new ProxyGenerationOptions();
                mixinOptions.AddDelegateTypeMixin(baseType);
                var proxy = CreateProxy(typeof(object), implementedInterfaces, Array.Empty<object>(), mixinOptions, () => new DynamicAvatarInterceptor(notImplemented));
                return Delegate.CreateDelegate(baseType, proxy, proxy.GetType().GetMethod("Invoke"));
            }
            else
            {
                return CreateProxy(baseType, implementedInterfaces, constructorArguments!, options, () => new DynamicAvatarInterceptor(notImplemented));
            }
        }

        /// <summary>
        /// Creates the proxy with the <see cref="Generator"/>, using the given default interceptor to implement its behavior.
        /// </summary>
        protected virtual object CreateProxy(Type baseType, Type[] implementedInterfaces, object[] constructorArguments, ProxyGenerationOptions options, Func<IInterceptor> getDefaultInteceptor)
            // TODO: bring the approach from https://github.com/moq/moq4/commit/806e9919eab9c1f3879b9e9bda895fa76ecf9d92 for performance.
            => generator.CreateClassProxy(baseType, implementedInterfaces, options, constructorArguments, getDefaultInteceptor());

        /// <summary>
        /// The <see cref="ProxyGenerator"/> used to create proxy types.
        /// </summary>
        protected ProxyGenerator Generator => generator;
    }
}
