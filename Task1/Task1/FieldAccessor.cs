using System.Linq.Expressions;
using System.Reflection;

namespace FieldAccessor
{
    public interface IFieldAccessorFactory
    {
        public Func<T, U?> GetFieldAccessor<T, U>(string fieldPath);
    }

    public class ExpressionBasedFieldAccessorFactory : IFieldAccessorFactory
    {
        public Func<T, U?> GetFieldAccessor<T, U>(string fieldPath)
        {
            ParameterExpression param = Expression.Parameter(typeof(T), "value");
            Expression body = fieldPath
                .Split(".")
                .Aggregate((Expression)param, Expression.Field);
            return Expression.Lambda<Func<T, U>>(body, [param]).Compile();
        }
    }

    public class ReflectionBasedFieldAccessorFactory : IFieldAccessorFactory
    {

        public Func<T, U?> GetFieldAccessor<T, U>(string fieldPath) =>
            (T value) => (U?)fieldPath.Split(".").Aggregate((object?)value, GetFieldValue);

        private static object? GetFieldValue(object? value, string fieldName) => (
                (value ?? throw new NullReferenceException($"null.{fieldName}"))
                    .GetType()
                    .GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new ArgumentException($"Field '{fieldName}' not found in type '{value.GetType()}'")
            ).GetValue(value);
    }
}
