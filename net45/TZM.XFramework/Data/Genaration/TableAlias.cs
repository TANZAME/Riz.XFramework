using System.Linq.Expressions;
using TZM.XFramework.Caching;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 表别名缓存项
    /// </summary>
    public class TableAlias
    {
        // 1.表别名缓存项（包含显示指定的和导航属性产生的）
        private readonly ICache<string, string> _alias = new SimpleCache<string, string>();

        // 2.导航属性产生的别名列表，这些别名没有在查询表达式显式地声明 存储为：访问链<a.Company.Address>：别名(tn)
        // 此缓存作用相当于占位符，给没有显式声明的<***>导航属性分配表别名
        // 如：
        //  where a.Company.Address.AddressName == "番禺"
        // 像这种表达式，a.Company.Address 访问链会被存储在此变量内
        private readonly ICache<string, string> _alias_Navigations = new SimpleCache<string, string>();

        // 3.由表达式显式指定的 LEFT JOIN 或者 Inner Join 表达式所对应的表的别名，存储为 数据表名称<Bas_Client>：别名(tn)
        // 如果同一个物理表显式指定多次，则只存储最后声明的那个
        // 如果实体显式指定了数据表别名，那么在后续的导航属性访问中就使用这个别名，而不会产生新的别名，从而不会产生新的关联
        // 否则后续的导航属性访问中就会产生 LEFT JOIN 语句和新的表别名
        // 如下例：
        // var query =
        //    from a in context.GetTable<Inte_CRM.Client>()
        //    join b in context.GetTable<Inte_CRM.CloudServer>() on a.ClientId equals b.CloudServerId
        //    where a.CloudServer.CloudServerId == 1
        //    select a;
        // CloudServer：t{x} 将被存储起来，在解析 a.CloudServer.CloudServerId 时表别名就会使用 t{x}
        private readonly ICache<string, string> _alias_Joins = new SimpleCache<string, string>();

        // 4.GetTable<,>(path) 指定导航属性对应的表别名
        private readonly ICache<string, string> _alias_GetTables = new SimpleCache<string, string>();

        // FROM 和 JOIN 表达式总数
        // 在这个计数的基础上再分配导航属性关联的表别名
        private int _holdQty = 0;
        // 别名前缀
        private string _aliasPrefix = null;

        /// <summary>
        /// 空别名名称
        /// </summary>
        public const string ALIASNULL = "AliasNull";

        /// <summary>
        /// FROM和JOIN子句显式指定的别名数量
        /// </summary>
        public int HoldQty
        {
            get { return _holdQty; }
        }

        /// <summary>
        /// 实例化 <see cref="TableAlias"/> 类的新实例
        /// </summary>
        public TableAlias()
            : this(0)
        {

        }

        /// <summary>
        /// 实例化 <see cref="TableAlias"/> 类的新实例
        /// </summary>
        /// <param name="holdQty">FROM 和 JOIN 表达式所占有的总数</param>
        public TableAlias(int holdQty)
            : this(holdQty, null)
        {
        }

        /// <summary>
        /// 实例化 <see cref="TableAlias"/> 类的新实例
        /// </summary>
        /// <param name="holdQty">FROM 和 JOIN 子句显式指定的别名数量</param>
        /// <param name="aliasPrefix">显式指定别名，用于内嵌的 exists 解析</param>
        public TableAlias(int holdQty, string aliasPrefix)
        {
            _holdQty = holdQty;
            _aliasPrefix = aliasPrefix;
        }

        /// <summary>
        /// 根据指定表达式取表别名
        /// </summary>
        /// <param name="expression">表达式</param> 
        /// <remarks>
        /// t=>t.Id
        /// t.Id
        /// </remarks>
        public string GetTableAlias(Expression expression)
        {
            // p=>p.p
            // p=>p.Id
            // p=>p.t.Id
            // p.Id
            // p.t.Id
            // p.t
            // <>h__TransparentIdentifier0.p.Id
            XFrameworkException.Check.NotNull(expression, "expression");
            string key = TableAlias.GetTableAliasKey(expression);
            return this.GetTableAlias(key);
        }

        /// <summary>
        ///  根据指定键值取表别名
        /// </summary>
        /// <param name="key">键值</param>
        public string GetTableAlias(string key)
        {
            return !string.IsNullOrEmpty(key) 
                ? this._alias.GetOrAdd(key, k => (!string.IsNullOrEmpty(_aliasPrefix) ? _aliasPrefix : "t") + this._alias.Count.ToString()) 
                : ALIASNULL;
        }

        /// <summary>
        ///  根据指定键值取导航属性对应的表别名
        /// </summary>
        /// <param name="key">键值</param>
        public string GetNavTableAlias(string key)
        {
            XFrameworkException.Check.NotNull(key, "key");
            return this._alias_Navigations.GetOrAdd(key, 
                k => (!string.IsNullOrEmpty(_aliasPrefix) ? _aliasPrefix : "t") + (this._alias_Navigations.Count + _holdQty).ToString());
        }

        /// <summary>
        ///  根据物理表名取其对应的别名
        ///  由查询表达式中显示指定的 左/内关联提供
        /// </summary>
        /// <param name="name">表名</param>
        public string GetJoinTableAlias(string name)
        {
            string alias = string.Empty;
            this._alias_Joins.TryGet(name, out alias);
            return alias;
        }

        /// <summary>
        /// 建立 表名/表别名 键值对
        /// 由查询表达式中显示指定的 左/内关联提供
        /// </summary>
        /// <param name="name">表名</param>
        /// <param name="alias">别名（t0,t1）</param>
        /// <returns></returns>
        public string AddJoinTableAlias(string name, string alias)
        {
            XFrameworkException.Check.NotNull(name, "name");
            return alias == ALIASNULL ? alias : this._alias_Joins.AddOrUpdate(name, k => alias, k => alias);
        }

        /// <summary>
        /// 根据 GetTable&lt;,&gt;(path) 指定导航属性取对应表别名
        /// </summary>
        /// <param name="key">键值</param>
        public string GetGetTableAlias(string key)
        {
            string alias = string.Empty;
            this._alias_GetTables.TryGet(key, out alias);
            return alias;
        }

        /// <summary>
        /// 添加由 GetTable&lt;,&gt;(path) 指定的导航属性所对应的表名称
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="alias">别名（t0,t1）</param>
        /// <returns></returns>
        public string AddGetTableAlias(string key, string alias)
        {
            XFrameworkException.Check.NotNull(key, "key");
            return alias == ALIASNULL ? alias : this._alias_GetTables.AddOrUpdate(key, k => alias, k => alias);
        }

        private static string GetTableAliasKey(Expression exp)
        {
            if (exp == null) return null;

            Expression expression = exp.ReduceUnary();

            //c
            if (exp.CanEvaluate()) return null;

            // p
            var parameter = expression as ParameterExpression;
            if (parameter != null) return parameter.Name;

            // a=>a.Id
            var lambda = expression as LambdaExpression;
            if (lambda != null) expression = lambda.Body.ReduceUnary();

            // a.Id
            // t.a
            // t.t.a
            // t.a.Id
            var member = expression as MemberExpression;
            if (member == null) return TableAlias.GetTableAliasKey(expression);

            if (member.Visitable()) return TableAlias.GetTableAliasKey(member.Expression);

            return member.Member.Name;
        }
    }
}
