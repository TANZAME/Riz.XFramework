using System;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SQL 语句生成器
    /// </summary>
    public interface ISqlBuilder
    {
        /// <summary>
        /// 获取或设置当前 <see cref="ISqlBuilder"/> 对象的长度。
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
        ResolveToken Token { get; }

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
        ISqlBuilder AppendMember(string alias, string name);

        /// <summary>
        /// 追加成员名称
        /// </summary>
        /// <param name="name">成员名称</param>
        /// <param name="quote">使用安全符号括起来，临时表不需要括</param>
        /// <returns></returns>
        ISqlBuilder AppendMember(string name, bool quote = true);

        /// <summary>
        /// 在此实例的结尾追加 AS
        /// </summary>
        ISqlBuilder AppendAs(string name);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ISqlBuilder Append(string value);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ISqlBuilder Append(char value);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ISqlBuilder Append(object value);

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        ISqlBuilder Append(object value, MemberVisitedMark.VisitedMember m);

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        ISqlBuilder AppendNewLine();

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        ISqlBuilder AppendNewLine(string value);

        /// <summary>
        /// 将通过处理复合格式字符串（包含零个或零个以上格式项）返回的字符串追加到此实例。每个格式项都替换为形参数组中相应实参的字符串表示形式。
        /// </summary>
        ISqlBuilder AppendFormat(string value, params object[] args);

        /// <summary>
        /// 在此实例的结尾追加制表符
        /// </summary>
        ISqlBuilder AppendTab();

        /// <summary>
        /// 将此实例中所有指定字符串的匹配项替换为其他指定字符串。
        /// </summary>
        ISqlBuilder Replace(string oldValue, string newValue);

        /// <summary>
        /// 将此实例的指定段中的字符复制到目标 System.Char 数组的指定段中
        /// </summary>
        /// <param name="sourceIndex">此实例中开始复制字符的位置。 索引是从零开始的</param>
        /// <param name="destination">将从中复制字符的数组</param>
        /// <param name="destinationIndex">destination 中将从其开始复制字符的起始位置。 索引是从零开始的</param>
        /// <param name="count">要复制的字符数。</param>
        void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// 去掉尾部的空白字符，包含换行符
        /// </summary>
        ISqlBuilder TrimEnd(params char[] chars);
    }
}
