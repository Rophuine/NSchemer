using System;
using System.ComponentModel;
using System.Reflection;

namespace NSchemer.Sql
{
    public class Column
    {
        public string name;
        public DataType dataType;
        public int length;
        public bool nullable;
        public string defaultSqlData = null;

        public virtual string GetSQL(bool forceNullable = false)
        {
            // Gives the DDL to generate this row
            string datatypeString = null;
            // This retrieves the datatype string from the enum markup above into datatypeString
            MemberInfo[] memberInfo = typeof(DataType).GetMember(dataType.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    datatypeString = ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            if (datatypeString == null)
                throw new Exception(String.Format("Cannot determine correct datatype string while creating field {0}.", name));
            string result = String.Format("[{0}] {1}", name, datatypeString);
            if (result.Contains("(size)"))
            {
                result = result.Replace("(size)", String.Format("({0})", length.ToString()));
            }
            if (nullable || forceNullable)
                result += " NULL";
            else
                result += " NOT NULL";

            if (_seed.HasValue)
            {
                result += $" IDENTITY({_seed},{_increment})";
            }

            return result;
        }
        public Column(string name, DataType dataType)
            : this(name, dataType, 0)
        { }
        public Column(string name, DataType dataType, int length)
        {
            this.name = name;
            this.dataType = dataType;
            this.length = length;
            this.nullable = true;
        }

        public Column(string name, DataType dataType, int length, bool nullable)
            : this(name, dataType, length, nullable, null)
        {}

        public Column(string name, DataType dataType, int length, bool nullable, string defaultSqlData)
            : this(name, dataType, length)
        {
            this.nullable = nullable;
            this.defaultSqlData = defaultSqlData;
        }
        public Column(string name, DataType dataType, bool nullable, string defaultSqlData) : this(name, dataType, 0, nullable, defaultSqlData) { }

        private int? _seed;
        private int? _increment;
        public Column Identity(int seed, int increment)
        {
            nullable = false;
            _seed = seed;
            _increment = increment;
            return this;
        }

        public bool PrimaryKey { get; private set; }

        public Column AsPrimaryKey()
        {
            PrimaryKey = true;
            nullable = false;
            return this;
        }

        public Column NotNull(string defaultSqlData = null)
        {
            nullable = false;
            if (defaultSqlData != null) this.defaultSqlData = defaultSqlData;
            return this;
        }

        public bool IsForeignKey { get; private set; }
        public string ForeignKeyName { get; private set; }
        public string ForeignKeyTable { get; private set; }
        public string ForeignKeyColumn { get; private set; }
        public bool CascadeOnDelete { get; private set; }
        public bool CascadeOnUpdate { get; private set; }
        public Column AsForeignKey(string keyName, string table, string column,
            bool cascadeOnDelete = false, bool cascadeOnUpdate = false)
        {
            IsForeignKey = true;
            ForeignKeyName = keyName;
            ForeignKeyTable = table;
            ForeignKeyColumn = column;
            CascadeOnDelete = cascadeOnDelete;
            CascadeOnUpdate = cascadeOnUpdate;
            return this;
        }
    }
}