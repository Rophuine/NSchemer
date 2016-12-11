using System.ComponentModel;

namespace NSchemer.Sql
{
    public enum DataType
    {
        // Markup each datatype with the correct SQL identifier to create it
        // If the datatype requires a size, add the text "(size)" in the correct spot - the "size" part will be replaced with the length specified
        [Description("nvarchar(size)")]
        STRING,
        [Description("uniqueidentifier")]
        GUID,
        [Description("integer")]
        INT,
        [Description("datetime")]
        DATETIME,
        [Description("bit")]
        BIT,
        [Description("uniqueidentifier")]
        UNIQUEID,
        [Description("tinyint")]
        TINYINT,
        [Description("float")]
        FLOAT,
        [Description("varbinary(max)")] // this should be varbinary!
        BINARY,
        [Description("smallint")]
        SMALLINT,
        [Description("bigint")]
        BIGINT,
        [Description("binary(size)")] // this is probably what binary should be!
        SHORTBINARY
    }
}