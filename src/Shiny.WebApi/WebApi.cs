using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using Shiny.Caching;
using Shiny.WebApi.Caching;
using Shiny.WebApi.Policing;

namespace Shiny.WebApi
{
    public class WebApi<TWebApi> : IWebApi<TWebApi>
    {
        readonly Dictionary<MethodCacheDetails, MethodCacheAttributes> cacheableMethodsSet = new Dictionary<MethodCacheDetails, MethodCacheAttributes>();
        readonly TWebApi webApi;
        readonly ICache cache;
        readonly IPolicyRegistry<string>? policyRegistry;

        public WebApi(TWebApi webApi, ICache cache, IServiceProvider serviceProvider)
        {
            this.webApi = webApi;
            this.cache = cache;
            this.cache.Enabled = true;
            if(serviceProvider.IsRegistered<IPolicyRegistry<string>>())
                this.policyRegistry = serviceProvider.GetRequiredService<IPolicyRegistry<string>>();
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression<Func<TWebApi, Task<TResult>>> executeApiMethod)
        {
            if (!this.IsMethodCacheable(executeApiMethod))
                return await executeApiMethod.Compile()(this.webApi).ConfigureAwait(false);

            var cacheKey = this.GetCacheKey(executeApiMethod);
            var cachedValue = await this.cache.Get<TResult>(cacheKey);

            if (cachedValue != null)
                return cachedValue;

            var policy = this.GetMethodPolicy(executeApiMethod.Body as MethodCallExpression);
            var executeApiMethodTask = executeApiMethod.Compile()(this.webApi);

            var restResponse = policy != null
                ? await policy.ExecuteAsync(async () => await executeApiMethodTask)
                : await executeApiMethodTask;

            if (restResponse != null)
            {
                var cacheAttributes = this.GetCacheAttribute(executeApiMethod);

                await this.cache.Set(cacheKey, restResponse, cacheAttributes.CacheAttribute.LifeSpan); 
            }

            return restResponse;
        }

        public Task ExecuteAsync(Expression<Func<TWebApi, Task>> executeApiMethod) => executeApiMethod.Compile()(this.webApi);

        #region Caching

        bool IsMethodCacheable<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> restExpression)
        {
            var methodToCacheDetails = this.GetMethodToCacheData(restExpression);

            lock (this)
            {
                var methodToCacheData = methodToCacheDetails;
                if (this.cacheableMethodsSet.ContainsKey(methodToCacheData))
                    return true;

                var cacheAttribute =
                    methodToCacheDetails.ApiInterfaceType.GetTypeInfo().GetCustomAttribute<CacheAttribute>() ??
                    methodToCacheData.MethodInfo.GetCustomAttribute<CacheAttribute>();

                if (cacheAttribute == null)
                    return false;

                var methodParameters = methodToCacheData.MethodInfo.GetParameters()
                    .Where(x => !typeof(CancellationToken).GetTypeInfo().IsAssignableFrom(IntrospectionExtensions.GetTypeInfo(x.ParameterType)))
                    .ToList();
                var cachePrimaryKey =
                    methodParameters
                        .Select((x, index) => new
                        {
                            Index = index,
                            ParameterInfo = x
                        })
                        .Where(x => x.ParameterInfo.CustomAttributes.Any(y => y.AttributeType == typeof(CacheKeyAttribute)))
                        .Select(x => new
                        {
                            ParameterName = x.ParameterInfo.Name,
                            ParameterType = x.ParameterInfo.ParameterType,
                            CacheAttribute = x.ParameterInfo.GetCustomAttribute<CacheKeyAttribute>(),
                            ParameterOrder = x.Index
                        }).FirstOrDefault();

                if (cachePrimaryKey == null && methodParameters.Any())
                    throw new InvalidOperationException($"{methodToCacheData.MethodInfo.Name} method has {nameof(CacheAttribute)}, " +
                                                        $"it has method parameters but none of that contain {nameof(CacheKeyAttribute)}");


                this.cacheableMethodsSet.Add(
                    methodToCacheData,
                    new MethodCacheAttributes(cacheAttribute, cachePrimaryKey?.CacheAttribute, cachePrimaryKey?.ParameterName, cachePrimaryKey?.ParameterType,
                        cachePrimaryKey?.ParameterOrder ?? 0)
                );
            }

            return true;
        }

        MethodCacheDetails GetMethodToCacheData<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> restExpression)
        {
            var apiInterfaceType = typeof(TApi);
            var methodBody = (MethodCallExpression)restExpression.Body;
            var methodInfo = methodBody.Method;
            return new MethodCacheDetails(apiInterfaceType, methodInfo);
        }

        static IEnumerable<ExtractedConstant> ExtractConstants(Expression expression)
        {
            if (expression == null)
                yield break;

            if (expression is ConstantExpression constantsExpression)
                yield return new ExtractedConstant { Name = constantsExpression.Type.Name, Value = constantsExpression.Value };


            else if (expression is LambdaExpression lambdaExpression)
                foreach (var constant in ExtractConstants(lambdaExpression.Body))
                    yield return constant;

            else if (expression is UnaryExpression unaryExpression)
                foreach (var constant in ExtractConstants(unaryExpression.Operand))
                    yield return constant;

            else if (expression is MethodCallExpression methodCallExpression)
            {
                foreach (var arg in methodCallExpression.Arguments)
                    foreach (var constant in ExtractConstants(arg))
                        yield return constant;
                foreach (var constant in ExtractConstants(methodCallExpression.Object))
                    yield return constant;
            }
            else if (expression is MemberExpression memberExpression)
            {
                foreach (var constants in ExtractConstants(memberExpression.Expression))
                    yield return constants;
            }
            else if (expression is InvocationExpression invocationExpression)
            {
                foreach (var constants in ExtractConstants(invocationExpression.Expression))
                    yield return constants;
            }

            else
                throw new NotImplementedException();
        }

        string GetCacheKey<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> fromExpression)
        {
            var methodCallExpression = (MethodCallExpression)fromExpression.Body;

            var cacheKeyPrefix = $"{typeof(TApi)}.{methodCallExpression.Method.Name}";
            if (!methodCallExpression.Arguments.Any())
                return $"{cacheKeyPrefix}()";

            var cacheAttributes = this.GetCacheAttribute(fromExpression);

            var extractedArguments = methodCallExpression.Arguments
                .SelectMany(x => ExtractConstants(x))
                .Where(x => x != null)
                .Where(x => x.Value is CancellationToken == false)
                .ToList();

            if (!extractedArguments.Any())
                return $"{cacheKeyPrefix}()";

            var primaryKeyName = cacheAttributes.ParameterName;
            object primaryKeyValue;
            var extractedArgument = extractedArguments[cacheAttributes.ParameterOrder];
            var extractedArgumentValue = extractedArgument.Value;


            var isArgumentValuePrimitve = extractedArgumentValue.GetType().GetTypeInfo().IsPrimitive ||
                                          extractedArgumentValue is decimal ||
                                          extractedArgumentValue is string;

            if (isArgumentValuePrimitve)
            {
                primaryKeyValue = extractedArgument.Value;
            }
            else
            {
                var primaryKeyValueField = extractedArgumentValue.GetType().GetRuntimeFields().Select((x, i) => new
                {
                    Index = i,
                    Field = x
                }).First(x => x.Index == cacheAttributes.ParameterOrder);

                primaryKeyValue = primaryKeyValueField.Field.GetValue(extractedArgumentValue);
            }

            foreach (var argument in extractedArguments)
            {
                var primaryKeyCacheField = argument
                    .Value
                    .GetType()
                    .GetRuntimeFields()
                    .FirstOrDefault(x => x.Name.Equals(cacheAttributes.ParameterName));

                if (primaryKeyCacheField != null)
                {
                    primaryKeyValue = primaryKeyCacheField.GetValue(argument.Value);
                    break;
                }
            }

            if (primaryKeyValue == null)
                throw new InvalidOperationException($"{nameof(CacheKeyAttribute)} primary key found for: " + cacheKeyPrefix);

            return $"{cacheKeyPrefix}({primaryKeyName}:{primaryKeyValue})";
        }

        MethodCacheAttributes? GetCacheAttribute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> expression)
        {
            lock (this)
            {
                var methodToCacheData = this.GetMethodToCacheData(expression);
                return this.cacheableMethodsSet[methodToCacheData];
            }
        }

        class ExtractedConstant
        {
            public object Value { get; set; }

            public string Name { get; set; }
        }

        #endregion

        IAsyncPolicy? GetMethodPolicy(MethodCallExpression? methodCallExpression)
        {
            if (methodCallExpression == null)
                return null;

            if (this.policyRegistry == null)
                return null;

            var policyAttribute = methodCallExpression.Method.GetCustomAttribute<PolicyAttribute>();
            if (policyAttribute == null)
                return null;

            IAsyncPolicy? policy = null;
            foreach (var registryKey in policyAttribute.RegistryKeys)
            {
                if (this.policyRegistry.TryGet<IAsyncPolicy>(registryKey, out var registeredPolicy))
                {
                    if (policy == null)
                        policy = registeredPolicy;
                    else
                        policy.WrapAsync(registeredPolicy);
                }
            }

            return policy;
        }
    }
}