NSchemer [![Build status](https://ci.appveyor.com/api/projects/status/f68iuxk6mosapale?svg=true)](https://ci.appveyor.com/project/Rophuine/nschemer)
========

.Net-based database schema management

# Why?

If you're not managing your schema automatically, you're doing it wrong. In dev environments, automatically update the schema on project launch. In all other environments (test, UAT, production), update it during your automated deployment process.

There are plenty of options out there. NSchemer gives you options, while being simple and light-weight. Microsoft SQL Server is the target platform - it's entirely possible it will work on other database platforms as well. If it doesn't and you want it to, I'm open to pull requests!

# How

## Create a schema class

Start off with `nuget install-package NSchemer`

Take a look at the system test project for a real example - but it's pretty simple. Inherit from `SqlClientDatabase` and implement the minimum to build. Your schema class might look like this:

```csharp
    public class TestSchema : SqlClientDatabase
    {
        public TestSchema(string connectionString) : base(connectionString) {}

        public override List<ITransition> Versions
        {
            get
            {
                return new List<ITransition>
                {
                    new CodeTransition(1, "Initial Schema", BuildTheWorld),
                    new CodeTransition(2, "Add Widget Table", "This script adds a very important table", AddWidgets)
                };
            }
        }

        private bool BuildTheWorld()
        {
            CreateTable("Thing",
                new Column("ThingId", DataType.BIGINT).AsIdentity(1, 1).AsPrimaryKey(),
                new Column("ThingName", DataType.STRING, 50)                
            );
            CreateTable("ThingAnnotation",
                new Column("AnnotationId", DataType.BIGINT).AsIdentity(1, 1).AsPrimaryKey(),
                new Column("Text", DataType.STRING, 50),
                new Column("ThingId", DataType.BIGINT, false).AsForeignKey("Thing", "ThingId")
            );
            return true;
        }

        private bool AddWidgets()
        {
            RunSql(@"CREATE TABLE DBO.Widget (WidgetId [int],WidgetName [nvarchar](50)) ON [PRIMARY]");
            return true;
        }
    }
```

You can use the helper methods (CreateTable, AddColumn, etc.), or you can drop SQL in-line. You can also use files:
```csharp
	new SqlScriptTransition(3, "Add another table", "Nothing to say here", "NSchemer.SystemTests.EmbeddedFile.sql")
```

## Update your schema

```csharp
new TestSchema("[connection string]").Update()
```

# Is it stable?

I do my best to keep it stable, but I don't provide a guarantee - this is a free library. However stable I keep it, I highly recommend your automated tests include schema update tests. If that's not feasible, at least ensure your various non-production environments will thoroughly exercise your schema migrations.

In short, NSchemer should be stable - but if you rely on it for important things, your testing should ensure that whichever version you're on will work with your environment and your schema.

# What was I thinking...

### ... when I included both a name and description in transitions?

Dunno. The original interface started many years ago, and I've questioned this decision many times. 1.x removes this requirement.

### ... when I decided to require version numbers?

I wanted ordering to be extremely predictable. I believe that every non-developer-PC schema you ever produce should follow exactly the same code-path - and using explicit version numbers helps you do this. However, I understand that this can be difficult in multi-developer environments. In the past, I've solved this by requiring developers to 'reserve' version numbers on master before they rely on them in a pull request.

I am also considering providing alternative ordering strategies (including omitting version 'numbers' entirely) in later versions.

### ... when I decided to require code transitions to return true/false?

Some of the code here was written way back when, and I was trying to preserve backward-compatibility. Later versions will assume transitions succeed unless they throw an exception.

Also, my early career was very C/C++ focused, and returning bools to indicate success/failure is much more of a thing in that world. I probably wouldn't make that choice today.

# Why ...

### ... are there strongly-typed helpers for things like CreateTable? Why not just SQL all the things?

I tend to SQL all the things, but when I was trying to introduce this to my team, some team members wanted to leave schema changes up to the DBA. As we didn't have a DBA, that was a problem. Some basic helpers made it easier to convince the team to start writing their own schema transitions.

### ... aren't there more strongly-typed helpers for things like CreateForeignKey?

You can create foreign keys using the fluent syntax when you add a new column. If there's demand, it wouldn't be hard to support creating foreign keys using existing columns.

# I really think you should [zzz] in version y.x.

Feel free to ping me on twitter: @rophuine. I might think you're onto something.

# Why not just use DbUp/RoundHouse/whatever?

Go for it. None of those were available when I started writing this code - it just took me years to get around to open-sourcing it.

# Roadmap

- More fluent configuration options
  - Fluent building of transition list from things like other classes, namespaces, and folders containing .sql files
  - Combining multiple sources of transitions
- More helper methods for things like foreign keys
- More ordering options, including dropping version numbers
- Better documentation
