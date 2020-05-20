using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Caching;
using Shiny.WebApi.Caching;

namespace Shiny.WebApi
{
    public class WebApi<TWebApi> : IWebApi<TWebApi>
    {
        private readonly Dictionary<MethodCacheDetails, MethodCacheAttributes> cacheableMethodsSet = new Dictionary<MethodCacheDetails, MethodCacheAttributes>();
        readonly TWebApi webApi;
        readonly ICache cache;

        public WebApi(TWebApi webApi, ICache cache)
        {
            this.webApi = webApi;
            this.cache = cache;
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression<Func<TWebApi, Task<TResult>>> executeApiMethod)
        {
            if (!this.IsMethodCacheable(executeApiMethod))
                return await executeApiMethod.Compile()(this.webApi).ConfigureAwait(false);

            var cacheKey = this.GetCacheKey(executeApiMethod);
            var cachedValue = await this.cache.Get<TResult>(cacheKey);

            if (cachedValue != null)
                return cachedValue;

            var restResponse = await executeApiMethod.Compile()(this.webApi);

            if (restResponse != null)
            {
                var refitCacheAttributes = this.GetRefitCacheAttribute(executeApiMethod);

                await this.cache.Set(cacheKey, restResponse, TimeSpan.FromHours(1)); 
            }

            var test = await this.cache.GetCachedItems();

            return restResponse;
        }

        public Task ExecuteAsync(Expression<Func<TWebApi, Task>> executeApiMethod) => executeApiMethod.Compile()(this.webApi);

        public bool IsMethodCacheable<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> restExpression)
        {
            var methodToCacheDetails = this.GetMethodToCacheData(restExpression);

            if (methodToCacheDetails == null)
                return false;

            lock (this)
            {
                var methodToCacheData = methodToCacheDetails;
                if (this.cacheableMethodsSet.ContainsKey(methodToCacheData))
                    return true;

                var refitCacheAttribute =
                    methodToCacheData
                        .MethodInfo
                        .GetCustomAttribute<CacheAttribute>();

                if (refitCacheAttribute == null)
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
                    new MethodCacheAttributes(refitCacheAttribute, cachePrimaryKey?.CacheAttribute, cachePrimaryKey?.ParameterName, cachePrimaryKey?.ParameterType,
                        cachePrimaryKey?.ParameterOrder ?? 0)
                );
            }

            return true;
        }

        private MethodCacheDetails? GetMethodToCacheData<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> restExpression)
        {
            var apiInterfaceType = typeof(TApi);

            var methodBody = restExpression.Body as MethodCallExpression;

            if (methodBody == null)
                return null;

            var methodInfo = methodBody.Method;
            return new MethodCacheDetails(apiInterfaceType, methodInfo);
        }

        private static IEnumerable<ExtractedConstant> ExtractConstants(Expression expression)
        {
            if (expression == null)
                yield break;

            if (expression is ConstantExpression)
            {
                var constantsExpression = expression as ConstantExpression;
                yield return
                    new ExtractedConstant() { Name = constantsExpression.Type.Name, Value = constantsExpression.Value };
            }


            else if (expression is LambdaExpression)
                foreach (var constant in ExtractConstants(
                    ((LambdaExpression)expression).Body))
                    yield return constant;

            else if (expression is UnaryExpression)
                foreach (var constant in ExtractConstants(
                    ((UnaryExpression)expression).Operand))
                    yield return constant;

            else if (expression is MethodCallExpression)
            {
                foreach (var arg in ((MethodCallExpression)expression).Arguments)
                foreach (var constant in ExtractConstants(arg))
                    yield return constant;
                foreach (var constant in ExtractConstants(
                    ((MethodCallExpression)expression).Object))
                    yield return constant;
            }
            else if (expression is MemberExpression)
            {
                var memberExpression = expression as MemberExpression;

                foreach (var constants in ExtractConstants(memberExpression.Expression))
                    yield return constants;
            }
            else if (expression is InvocationExpression)
            {
                var invocationExpression = expression as InvocationExpression;

                foreach (var constants in ExtractConstants(invocationExpression.Expression))
                    yield return constants;
            }

            else
                throw new NotImplementedException();
        }

        private string GetCacheKey<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> fromExpression)
        {
            var methodCallExpression = fromExpression.Body as MethodCallExpression;

            var cacheKeyPrefix = typeof(TApi).ToString() + "/" + methodCallExpression.Method.Name.ToString();
            if (!methodCallExpression.Arguments.Any())
                return cacheKeyPrefix;

            var cacheAttributes = this.GetRefitCacheAttribute(fromExpression);

            var extractedArguments = methodCallExpression.Arguments
                .SelectMany(x => ExtractConstants(x))
                .Where(x => x != null)
                .Where(x => x.Value is CancellationToken == false)
                .ToList();

            if (!extractedArguments.Any())
                return cacheKeyPrefix;

            object primaryKeyValue = null;
            var extractedArgument = extractedArguments[cacheAttributes.ParameterOrder];
            var extractedArgumentValue = extractedArgument.Value;


            var isArgumentValuePrimitve = extractedArgumentValue.GetType().GetTypeInfo().IsPrimitive ||
                                          extractedArgumentValue is decimal ||
                                          extractedArgumentValue is string;

            if (isArgumentValuePrimitve)
                primaryKeyValue = extractedArgument.Value;
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

            return $"{cacheKeyPrefix}/{primaryKeyValue.ToString()}";
        }

        private MethodCacheAttributes GetRefitCacheAttribute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> expression)
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
    }
}