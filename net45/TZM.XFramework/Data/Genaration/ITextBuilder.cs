using System;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SQL 语句建造器
    /// </summary>
    public interface ITextBuilder
    {
        /// <summary>
        /// 获取或设置当前 <see cref="ITextBuilder"/> 对象的长度。
        /// </summary>
        int Length { get; set; }

        /// <summary>
        /// 获取或设置此实例中指定字符位置处的字符。
        /// </summary>
        char this[int index] { get; }

        /// <summary>
        /// 缩进
        /// </summary>
        int Indent { get; set; }

        /// <summary>
        /// 参数化
        /// </summary>
        bool Parameterized { get; }

        /// <summary>
        /// 解析上下文参数
        /// </summary>
        ParserToken Token { get; set; }

        /// <summary>
        /// 追加列名
        /// </summary>
        /// <param name="aliases">表别名</param>
        /// <param name="expression">列名表达式</param>
        /// <returns>返回解析到的表别名</returns>
        string AppendMember(TableAliasCache aliases, Expression expression);

        /// <summary>
        /// 追加列名
        /// </summary>
        /// <param name="expression">列名表达式</param>
        /// <param name="aliases">表别名</param>
        /// <returns>返回解析到的表别名</returns>
        Expression AppendMember(Expression expression, TableAliasCache aliases);

        /// <summary>
        /// 追加列名
        /// </summary>
        ITextBuilder AppendMember(string alias, string name);

        /// <summary>
        /// 追加成员名称
        /// </summary>
        /// <param name="name">成员名称</param>
        /// <param name="quote">使用安全符号括起来，临时表不需要括</param>
        /// <returns></returns>
        ITextBuilder AppendMember(string name, bool quote = true);

        /// <summary>
        /// 在此实例的结尾追加 AS
        /// </summary>
        ITextBuilder AppendAs(string name);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ITextBuilder Append(string value);

        /// <summary>
        /// 将字符串插入到此实例中的指定字符位置。
        /// </summary>
        ITextBuilder Insert(int index, string value);

        /// <summary>
        /// 将字符串插入到此实例中的指定字符位置。
        /// </summary>
        ITextBuilder Insert(int index, object value);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ITextBuilder Append(int value);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ITextBuilder Append(char value);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ITextBuilder Append(object value);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ITextBuilder Append(object value, MemberExpression m);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ITextBuilder Append(object value, MemberInfo m, Type declareType);

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        ITextBuilder AppendNewLine();

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        ITextBuilder AppendNewLine(string value);

        /// <summary>
        /// 将通过处理复合格式字符串（包含零个或零个以上格式项）返回的字符串追加到此实例。每个格式项都替换为形参数组中相应实参的字符串表示形式。
        /// </summary>
        ITextBuilder AppendFormat(string value, params object[] args);

        /// <summary>
        /// 在此实例的结尾追加制表符
        /// </summary>
        ITextBuilder AppendNewTab();

        /// <summary>
        /// 将此实例中所有指定字符串的匹配项替换为其他指定字符串。
        /// </summary>
        ITextBuilder Replace(string oldValue, string newValue);

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="node">成员访问表达式</param>
        /// <returns></returns>
        string GetSqlValue(object value, MemberExpression node = null);

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="member">成员</param>
        /// <param name="runtimeType">成员所属类型</param>
        /// <returns></returns>
        string GetSqlValue(object value, MemberInfo member, Type runtimeType);

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        string GetSqlValue(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null);

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// <para>因为会使用到默认值，故此重载仅限 INSERT / UPDATE 时使用</para>
        /// </summary>
        string GetSqlValueWidthDefault(object value, ColumnAttribute attribute);

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="attribute">字段属性</param>
        string GetSqlValue(object value, ColumnAttribute attribute);

        /// <summary>
        /// 单引号转义
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <param name="unicode">是否需要加N</param>
        /// <param name="replace">单引号替换成双引号</param>
        /// <param name="quote">前后两端是否加引号</param>
        /// <returns></returns>
        string EscapeQuote(string s, bool unicode = false, bool replace = false, bool quote = true);

        /// <summary>
        /// 获取指定成员的 <see cref="ColumnAttribute"/>
        /// </summary>
        ColumnAttribute GetColumnAttribute(System.Reflection.MemberInfo member, Type runtimeType);
    }
}
