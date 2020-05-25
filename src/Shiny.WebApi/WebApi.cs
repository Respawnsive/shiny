using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using Shiny.Caching;
using Shiny.WebApi.Caching;
using Shiny.WebApi.Policing;

namespace Shiny.WebApi
{
    public class WebApi<TWebApi> : IWebApi<TWebApi>
    {
        readonly Dictionary<MethodCacheDetails, MethodCacheAttributes> cacheableMethodsSet = new Dictionary<MethodCacheDetails, MethodCacheAttributes>();
        private readonly ConcurrentDictionary<string, object> _inflightFetchRequests = new ConcurrentDictionary<string, object>();

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

        public IObservable<TResult> Execute<TResult>(Expression<Func<TWebApi, Task<TResult>>> executeApiMethod)
        {
            string? cacheKey = null;
            MethodCacheAttributes? cacheAttributes = null;
            var policy = this.GetMethodPolicy(executeApiMethod.Body as MethodCallExpression);
            var executeApiMethodAsObservableTask = (policy != null
                    ? policy.ExecuteAsync(async () => await executeApiMethod.Compile()(this.webApi))
                    : executeApiMethod.Compile()(this.webApi)).ToObservable();
            CacheMode? cacheMode = null;

            if (this.IsMethodCacheable(executeApiMethod))
            {
                cacheKey = this.GetCacheKey(executeApiMethod);
                cacheAttributes = this.GetCacheAttribute(executeApiMethod);
                cacheMode = cacheAttributes.CacheAttribute.Mode;
            }

            if (cacheMode == null)
            {
                return executeApiMethodAsObservableTask;
            }
            else if (cacheMode == CacheMode.GetOrFetch)
            {
                return this.cache.Get<TResult>(cacheKey).ToObservable().Catch<TResult, Exception>(ex =>
                {
                    var prefixedKey = this.cache.GetHashCode().ToString(CultureInfo.InvariantCulture) + cacheKey;

                    var result = Observable.Defer(() => executeApiMethodAsObservableTask)
                        .Do(x => this.cache.Set(cacheKey, x, cacheAttributes.CacheAttribute.LifeSpan))
                        .Finally(() => this._inflightFetchRequests.TryRemove(prefixedKey, out var _))
                        .Multicast(new AsyncSubject<TResult>())
                        .RefCount();

                    return (IObservable<TResult>)this._inflightFetchRequests.GetOrAdd(prefixedKey, result);
                });
            }
            else
            {
                var fetch = Observable.DeferAsync(async token =>
                        {
                            if (!string.IsNullOrWhiteSpace(cacheKey))
                            {
                                var result = await this.cache.Get<TResult>(cacheKey);
                                return Observable.Return(result);
                            }

                            return Observable.Empty<TResult>();
                        }).Select(x => x == null)
                        .Where(x => x)
                        .SelectMany(_ =>
                        {
                            var fetchObs = executeApiMethodAsObservableTask.Catch<TResult, Exception>(
                                ex =>
                                {
                                    var shouldInvalidate =
                                        cacheAttributes != null && cacheAttributes.CacheAttribute.ShouldInvalidateOnError && !string.IsNullOrWhiteSpace(cacheKey)
                                            ? this.cache.Remove(cacheKey).ToObservable()
                                            : Observable.Return(false);
                                    return shouldInvalidate.SelectMany(_ => Observable.Throw<TResult>(ex));
                                });

                            return fetchObs.SelectMany(x => this.cache.Remove(cacheKey).ToObservable().Select(_ => x))
                                .SelectMany(x => this.cache.Set(cacheKey, x, cacheAttributes.CacheAttribute.LifeSpan).ToObservable().Select(_ => x));
                        });

                var result = this.cache.Get<TResult>(cacheKey).ToObservable().Select(x => new Tuple<TResult, bool>(x, true))
                    .Catch(Observable.Return(new Tuple<TResult, bool>(default, false)));

                return result.SelectMany(x => x.Item2 ? Observable.Return(x.Item1) : Observable.Empty<TResult>())
                    .Concat(fetch).Multicast(new ReplaySubject<TResult>()).RefCount(); 
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression<Func<TWebApi, Task<TResult>>> executeApiMethod)
        {
            string? cacheKey = null;
            TResult result = default;
            MethodCacheAttributes? cacheAttributes = null;

            if (this.IsMethodCacheable(executeApiMethod))
            {
                cacheKey = this.GetCacheKey(executeApiMethod);
                result = await this.cache.Get<TResult>(cacheKey); 
                cacheAttributes = this.GetCacheAttribute(executeApiMethod);
            }

            if (result == null || cacheAttributes?.CacheAttribute.Mode == CacheMode.GetAndFetch)
            {
                var policy = this.GetMethodPolicy(executeApiMethod.Body as MethodCallExpression);
                var executeApiMethodTask = executeApiMethod.Compile()(this.webApi);

                try
                {
                    result = policy != null
                        ? await policy.ExecuteAsync(async () => await executeApiMethodTask)
                        : await executeApiMethodTask;
                }
                catch (Exception e)
                {
                    throw new WebApiException<TResult>(e, result);
                }

                if (result != null && !string.IsNullOrWhiteSpace(cacheKey) && cacheAttributes != null)
                    await this.cache.Set(cacheKey, result, cacheAttributes.CacheAttribute.LifeSpan);
            }

            return result;
        }

        public Task ExecuteAsync(Expression<Func<TWebApi, Task>> executeApiMethod)
        {
            var policy = this.GetMethodPolicy(executeApiMethod.Body as MethodCallExpression);
            var executeApiMethodTask = executeApiMethod.Compile()(this.webApi);

            try
            {
                return policy != null
                        ? policy.ExecuteAsync(async () => await executeApiMethodTask)
                        : executeApiMethodTask;
            }
            catch (Exception e)
            {
                throw new WebApiException(e, Unit.Default);
            }
        }

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

        #region Policing

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

        #endregion
    }
}