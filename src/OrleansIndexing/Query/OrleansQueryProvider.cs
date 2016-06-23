using Orleans.Runtime;
using Orleans.Runtime.MembershipService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;

namespace Orleans.Indexing
{
    /// <summary>
    /// Implements <see cref="IOrleansQueryProvider"/>
    /// </summary>
    public class OrleansQueryProvider<T> : IOrleansQueryProvider where T : IIndexableGrain
    {
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression.Type.IsGenericType && expression.Type.GetGenericArguments().Count() == 1)
            {
                return CreateQuery(expression, expression.Type.GetGenericArguments()[0]);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return (IQueryable<TElement>)((IQueryProvider)this).CreateQuery(expression);
        }

        private IQueryable CreateQuery(Expression expression, Type iGrainType)
        {
            if(expression.NodeType == ExpressionType.Call)
            {
                var methodCall = ((MethodCallExpression)expression);
                IGrainFactory gf;
                if(IsWhereClause(methodCall) 
                    && CheckIsOrleansIndex(methodCall.Arguments[0], iGrainType, out gf)
                    && methodCall.Arguments[1].NodeType == ExpressionType.Quote
                    && ((UnaryExpression)methodCall.Arguments[1]).Operand.NodeType == ExpressionType.Lambda)
                {
                    var whereClause = (LambdaExpression)((UnaryExpression)methodCall.Arguments[1]).Operand;
                    String indexName;
                    object lookupValue;
                    if(TryGetIndexNameAndLookupValue(whereClause, iGrainType, out indexName, out lookupValue))
                    {
                        return (IQueryable)Activator.CreateInstance(typeof(QueryIndexedGrains<>).MakeGenericType(iGrainType), gf, indexName, lookupValue);
                    }
                }
            }
            throw new NotSupportedException();
        }

        private bool CheckIsOrleansIndex(Expression e, Type iGrainType, out IGrainFactory gf)
        {
            if(e.NodeType == ExpressionType.Constant &&
                typeof(QueryActiveGrains<>).MakeGenericType(iGrainType).IsAssignableFrom(((ConstantExpression)e).Value.GetType().GetGenericTypeDefinition().MakeGenericType(iGrainType)))
            {
                gf = ((QueryGrains)((ConstantExpression)e).Value).GetGrainFactory();
                return true;
            }
            gf = null;
            return false;
        }

        private bool IsWhereClause(MethodCallExpression call)
        {
            return call.Arguments.Count() == 2 && call.Method.ReflectedType.Equals(typeof(Queryable)) && call.Method.Name == "Where";
        }

        /// <summary>
        /// This method tries to pull out the index name and
        /// lookup value from the given expression tree.
        /// </summary>
        /// <param name="exprTree">the given expression tree</param>
        /// <param name="indexName">the index name that is intended to
        /// be pulled out of the expression tree.</param>
        /// <param name="lookupValue">the lookup value that is intended to
        /// be pulled out of the expression tree.</param>
        /// <returns>determines whether the operation was successful or not</returns>
        private static bool TryGetIndexNameAndLookupValue(LambdaExpression exprTree, Type iGrainType, out string indexName, out object lookupValue)
        {
            if (exprTree.Body is BinaryExpression)
            {
                BinaryExpression operation = (BinaryExpression)exprTree.Body;
                if (operation.NodeType == ExpressionType.Equal)
                {
                    ConstantExpression constantExpr = null;
                    Expression fieldExpr = null;
                    if (operation.Right is ConstantExpression)
                    {
                        constantExpr = (ConstantExpression)operation.Right;
                        fieldExpr = operation.Left;
                    }
                    else if (operation.Left is ConstantExpression)
                    {
                        constantExpr = (ConstantExpression)operation.Left;
                        fieldExpr = operation.Right;
                    }

                    if (constantExpr != null && fieldExpr != null)
                    {
                        lookupValue = constantExpr.Value;
                        indexName = GetIndexName(exprTree, iGrainType, fieldExpr);
                        return true;
                    }
                }
            }
            throw new NotSupportedException(string.Format("The provided expression is not supported yet: {0}", exprTree));
        }

        /// <summary>
        /// This method tries to pull out the index name from
        /// a given field expression.
        /// </summary>
        /// <param name="exprTree">the original expression tree</param>
        /// <param name="fieldExpr">the field expression that should
        /// contain the indexed field</param>
        /// <returns></returns>
        private static string GetIndexName(LambdaExpression exprTree, Type iGrainType, Expression fieldExpr)
        {
            ParameterExpression iGrainParam = exprTree.Parameters[0];
            if (fieldExpr is MemberExpression)
            {
                Expression innerFieldExpr = ((MemberExpression)fieldExpr).Expression;
                if (innerFieldExpr is MethodCallExpression)
                {
                    MethodCallExpression methodCall = (MethodCallExpression)innerFieldExpr;
                    if (methodCall.Object.Equals(iGrainParam))
                    {
                        MethodInfo grainInterfaceMethod = methodCall.Method;
                        return IndexUtils.GetIndexNameOnInterfaceGetter(iGrainType, grainInterfaceMethod);
                    }
                }
            }
            throw new NotSupportedException(string.Format("The provided expression is not supported yet: {0}", exprTree));
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>
        /// The value that results from executing the specified query.
        /// </returns>
        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression);
        }
    }
}
