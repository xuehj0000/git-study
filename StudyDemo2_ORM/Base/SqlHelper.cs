﻿using System;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;

namespace StudyDemo2_ORM
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlHelper
    {
        private static string ConnectionStringCustomer = ConfigurationManager.SqlConnectionStringCustom;

        /// <summary>
        /// 泛型查询
        /// </summary>
        public T Find<T>(int id) where T : BaseEntity,new()
        {
            Type type = typeof(T);
            var sql = SqlCacheBuilder<T>.GetSql(SqlCacheType.FindOne);
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@Id", id) };

            return this.ExexuteSql<T>(sql, parameters, command =>
            {
                var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    T t = new T();
                    foreach (var prop in type.GetProperties())
                    {
                        var propName = prop.GetMappingName();
                        prop.SetValue(t, reader[propName] is DBNull ? null : reader[propName]);    // 数据库为null时，查询出来的是DbNull类型，不能直接赋值给Nullable，如下处理
                    }
                    return t;
                }
                else
                {
                    return null;
                }
            });
        }

        public T Find<T>(Expression<Func<T, bool>> queryExpression) where T : BaseEntity,new()
        {
            // 表实体类型
            Type type = typeof(T);

            // 表达式目录树访问者对象（解析表达式）
            var where = new SqlVisitor<T>(queryExpression).GetSqlWhere();
            
            // 表列名字符串
            var columnString = string.Join(",", type.GetProperties().Select(a => $"[{a.GetMappingName()}]"));
            // 组装查询SQL语句
            string sql = $"select {columnString} from [{type.GetMappingName()}] where {where};";

            return this.ExexuteSql<T>(sql, null, command =>
            {
                var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    T t = new T();
                    foreach (var prop in type.GetProperties())
                    {
                        var propName = prop.GetMappingName();
                        prop.SetValue(t, reader[propName] is DBNull ? null : reader[propName]);    // 数据库为null时，查询出来的是DbNull类型，不能直接赋值给Nullable，如下处理
                    }
                    return t;
                }
                else
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// 泛型插入
        /// </summary>
        public bool Insert<T>(T t) where T : BaseEntity,new()
        {
            Type type = typeof(T);
            var sql = SqlCacheBuilder<T>.GetSql(SqlCacheType.Insert);
            var parameters = type.GetProperties().Select(r => new SqlParameter($"@{r.GetMappingName()}", r.GetValue(t)??DBNull.Value)).ToArray();
            return this.ExexuteSql<bool>(sql, parameters, command => command.ExecuteNonQuery() == 1);
        }

        /// <summary>
        /// 泛型更新
        /// </summary>
        public bool Update<T>(T t) where T : BaseEntity, new()
        {
            if (!t.Validate())
            {
                throw new Exception("数据校验失败！");
            }
            Type type = typeof(T);
            var sql = SqlCacheBuilder<T>.GetSql(SqlCacheType.Update);
            var parameters = type.GetPropsWithOutIgnore().Select(r => new SqlParameter($"@{r.GetMappingName()}", r.GetValue(t)??DBNull.Value)).Append(new SqlParameter("@Id", t.Id)).ToArray();
            return this.ExexuteSql<bool>(sql, parameters, command => command.ExecuteNonQuery() == 1);
        }

        /// <summary>
        /// 泛型删除
        /// </summary>
        public bool Delete<T>(T t) where T:BaseEntity, new()
        {
            Type type = typeof(T);
            var sql = SqlCacheBuilder<T>.GetSql(SqlCacheType.Delete);
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@Id", t.Id) };
            return this.ExexuteSql<bool>(sql, parameters, command => command.ExecuteNonQuery() == 1);
        }

        public bool Delete<T>(Expression<Func<T,bool>> expression)
        {
            var sql = $"";


            return true;
        }


        /// <summary>
        /// 封装Ado.Net,重复代码集中在一起，方便维护升级。集中管理Ado.Net操作集中起来，避免写错
        /// </summary>
        private S ExexuteSql<S>(string sql, SqlParameter[] parameters, Func<SqlCommand, S> func)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionStringCustomer))
            {
                SqlCommand command = new SqlCommand(sql, conn);
                if (parameters != null) command.Parameters.AddRange(parameters);
                conn.Open();
                return func.Invoke(command);
            }
        }
    }

    #region 笔记

    // 问题--特殊字符--sql注入， ‘，号导致异常’， sql 注入：');select * from user where 1=1;--
    // 解决sql注入：1.数据清洗，转码；2.参数化；3.使用 orm 框架(就是参数化)

    // 数据库有规则，不应该留到数据库校验。还有的时候有业务规则（state只能是012），数据入库前一定要检验，不能相信客户端检测
    // 一站式通用数据校验，集中校验，规则各不相同，只能一一提供，额外提供一点格式说明


    // 希望删除状态等于3的， ORM 是不支持的, state==3  id>10   and   or   条件千变万化，个数也不确定
    // expression:表达式目录树，是一个很好的语言。是一个二叉树结构。想获取叶节点，只能一个个访问，因为不知道节点深度，可能需要递归。

    #endregion


}
