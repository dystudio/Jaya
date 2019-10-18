﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jaya.Shared.Services
{
    public sealed class ServiceLocator : IDisposable
    {
        static ServiceLocator _instance;
        static readonly object _syncRoot;
        IServiceScope _scope;
        readonly List<Type> _plugins;

        static ServiceLocator()
        {
            _syncRoot = new object();
        }

        private ServiceLocator()
        {
            _plugins = new List<Type>();
        }

        ~ServiceLocator()
        {
            Dispose();
        }

        #region properties

        public static ServiceLocator Instance
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_instance == null)
                        _instance = new ServiceLocator();
                }

                return _instance;
            }
        }

        public IServiceProvider Provider
        {
            get
            {
                if (_scope == null)
                    _scope = RegisterServices();

                return _scope.ServiceProvider;
            }
        }

        public IEnumerable<Type> PluginTypes => _plugins;

        #endregion

        IServiceScope RegisterServices()
        {
            var collection = new ServiceCollection();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (!assembly.FullName.StartsWith("Jaya", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var types = assembly.DefinedTypes;
                foreach (TypeInfo typeInfo in types)
                    if (typeInfo.IsClass && typeInfo.Name.EndsWith("Service", StringComparison.InvariantCulture))
                        collection.AddScoped(typeInfo);
            }

            var container = collection.BuildServiceProvider();
            var scopeFactory = container.GetRequiredService<IServiceScopeFactory>();
            return scopeFactory.CreateScope();
        }

        void UnregisterServices(IServiceScope scope)
        {
            if (scope == null)
                return;

            scope.Dispose();
        }

        public void Dispose()
        {
            UnregisterServices(_scope);
        }

        public T GetService<T>()
        {
            if (_scope == null)
                _scope = RegisterServices();

            return _scope.ServiceProvider.GetService<T>();
        }

        public object CreateInstance(Type type)
        {
            return ActivatorUtilities.CreateInstance(Provider, type);
        }

        public T CreateInstance<T>()
        {
            return ActivatorUtilities.CreateInstance<T>(Provider, typeof(T));
        }
    }
}
