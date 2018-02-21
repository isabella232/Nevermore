using System.Collections.Generic;
using Assent;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Joins;
using Nevermore.QueryGraph;
using NSubstitute;
using Xunit;
using JoinClause = Nevermore.QueryGraph.JoinClause;

namespace Nevermore.Tests.QueryBuilderFixture
{
    public class QueryBuilderFixture
    {
        readonly ITableAliasGenerator tableAliasGenerator = Substitute.For<ITableAliasGenerator>();
        readonly IRelationalTransaction transaction;

        public QueryBuilderFixture()
        {
            transaction = Substitute.For<IRelationalTransaction>();

            var tableNumber = 0;
            tableAliasGenerator.GenerateTableAlias(Arg.Any<string>()).Returns(delegate
            {
                tableNumber++;
                return "t" + tableNumber;
            });
        }

        ITableSourceQueryBuilder<TDocument> CreateQueryBuilder<TDocument>(string tableName)
        {
            return new TableSourceQueryBuilder<TDocument>(tableName, transaction, tableAliasGenerator, new CommandParameterValues(), new Parameters());
        }

        [Fact]
        public void ShouldGenerateSelect()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Where("[Price] > 5")
                .OrderBy("Name")
                .DebugViewRawQuery();

            const string expected = "SELECT * FROM dbo.[Orders] WHERE ([Price] > 5) ORDER BY [Name]";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateSelectNoOrder()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Where("[Price] > 5")
                .DebugViewRawQuery();

            const string expected = "SELECT * FROM dbo.[Orders] WHERE ([Price] > 5) ORDER BY [Id]";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateSelectForQueryBuilder()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
             .Where("[Price] > 5")
             .DebugViewRawQuery();

            const string expected = "SELECT * FROM dbo.[Orders] WHERE ([Price] > 5) ORDER BY [Id]";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateSelectForJoin()
        {
            var leftQueryBuilder = CreateQueryBuilder<IDocument>("Orders")
                .Where("[Price] > 5");
            var rightQueryBuilder = CreateQueryBuilder<IDocument>("Customers");

            var actual = leftQueryBuilder
                .Join(rightQueryBuilder.AsAliasedSource(), JoinType.InnerJoin)
                .On("CustomerId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateSelectForMultipleJoins()
        {

            var leftQueryBuilder = CreateQueryBuilder<IDocument>("Orders");
            var join1QueryBuilder = CreateQueryBuilder<IDocument>("Customers");
            var join2QueryBuilder = CreateQueryBuilder<IDocument>("Accounts");

            var actual = leftQueryBuilder
                .Join(join1QueryBuilder.AsAliasedSource(), JoinType.InnerJoin).On("CustomerId", JoinOperand.Equal, "Id")
                .Join(join2QueryBuilder.AsAliasedSource(), JoinType.InnerJoin).On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateSelectForComplicatedSubqueryJoin()
        {
            var orders = CreateQueryBuilder<IDocument>("Orders");
            var customers = CreateQueryBuilder<IDocument>("Customers")
                .Where("IsActive = 1")
                .OrderBy("Id");

            var accounts = CreateQueryBuilder<IDocument>("Accounts").Hint("WITH (UPDLOCK)");

            var actual = orders.Join(customers.Subquery().AsSource(), JoinType.InnerJoin)
                .On("CustomerId", JoinOperand.Equal, "Id")
                .On("Owner", JoinOperand.Equal, "Owner")
                .Join(accounts.Subquery().AsSource(), JoinType.InnerJoin).On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateCount()
        {
            string actual = null;
            transaction.ExecuteScalar<int>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            CreateQueryBuilder<IDocument>("Orders")
                .NoLock()
                .Where("[Price] > 5")
                .Count();

            var expected = "SELECT COUNT(*) FROM dbo.[Orders] NOLOCK WHERE ([Price] > 5)";

            Assert.Equal(expected, actual);
        }


        [Fact]
        public void ShouldGenerateDelete()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .NoLock()
                .Where("[Price] > 5")
                .GetSelectBuilder()
                .DeleteQuery();

            var expected = "DELETE FROM dbo.[Orders] NOLOCK WHERE ([Price] > 5)";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateCountForQueryBuilder()
        {
            string actual = null;
            transaction.ExecuteScalar<int>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            CreateQueryBuilder<IDocument>("Orders")
                .NoLock()
                .Where("[Price] > 5")
                .Count();

            const string expected = "SELECT COUNT(*) FROM dbo.[Orders] NOLOCK WHERE ([Price] > 5)";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateCountForJoin()
        {
            string actual = null;
            transaction.ExecuteScalar<int>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            var leftQueryBuilder = CreateQueryBuilder<IDocument>("Orders")
                .Where("[Price] > 5");
            var rightQueryBuilder = CreateQueryBuilder<IDocument>("Customers");
            leftQueryBuilder.Join(rightQueryBuilder.AsAliasedSource(), JoinType.InnerJoin).On("CustomerId", JoinOperand.Equal, "Id")
                .Count();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGeneratePaginate()
        {
            string actual = null;
            transaction.ExecuteReader<IDocument>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());
            CreateQueryBuilder<IDocument>("Orders")
                .Where("[Price] > 5")
                .OrderBy("Foo")
                .ToList(10, 20);
            
            this.Assent(actual);
        }


        [Fact]
        public void ShouldGeneratePaginateForJoin()
        {
            string actual = null;
            transaction.ExecuteReader<IDocument>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            var leftQueryBuilder = CreateQueryBuilder<IDocument>("Orders")
                .Where("[Price] > 5");
            var rightQueryBuilder = CreateQueryBuilder<IDocument>("Customers");
            leftQueryBuilder
                .Join(rightQueryBuilder.AsAliasedSource(), JoinType.InnerJoin).On("CustomerId", JoinOperand.Equal, "Id")
                .ToList(10, 20);

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateTop()
        {
            string actual = null;
            transaction.ExecuteReader<IDocument>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());
            CreateQueryBuilder<IDocument>("Orders")
                .NoLock()
                .Where("[Price] > 5")
                .OrderBy("Id")
                .Take(100);

            var expected = "SELECT TOP 100 * FROM dbo.[Orders] NOLOCK WHERE ([Price] > 5) ORDER BY [Id]";

            Assert.Equal(expected, actual);
        }


        [Fact]
        public void ShouldGenerateTopForJoin()
        {
            string actual = null;
            transaction.ExecuteReader<IDocument>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            var leftQueryBuilder = CreateQueryBuilder<IDocument>("Orders")
                .Where("[Price] > 5");
            var rightQueryBuilder = CreateQueryBuilder<IDocument>("Customers");

            leftQueryBuilder.Join(rightQueryBuilder.AsAliasedSource(), JoinType.InnerJoin).On("CustomerId", JoinOperand.Equal, "Id")
                .Take(100);

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateExpectedLikeParametersForQueryBuilder()
        {
            CommandParameterValues parameterValues = null;
            transaction.ExecuteReader<IDocument>(Arg.Any<string>(), Arg.Do<CommandParameterValues>(pv => parameterValues = pv));

            // We need to make sure parameters like opening square brackets are correctly escaped for LIKE pattern matching in SQL.
            var environment = new
            {
                Id = "Environments-1"
            };
            CreateQueryBuilder<IDocument>("Project")
                .Where("[JSON] LIKE @jsonPatternSquareBracket")
                .LikeParameter("jsonPatternSquareBracket", $"\"AutoDeployReleaseOverrides\":[{{\"EnvironmentId\":\"{environment.Id}\"")
                .Where("[JSON] NOT LIKE @jsonPatternPercentage")
                .LikeParameter("jsonPatternPercentage", $"SomeNonExistantField > 5%")
                .ToList();

            var actualParameter1 = parameterValues["jsonPatternSquareBracket"];
            const string expectedParameter1 = "%\"AutoDeployReleaseOverrides\":[[]{\"EnvironmentId\":\"Environments-1\"%";
            Assert.Equal(actualParameter1, expectedParameter1);

            var actualParameter2 = parameterValues["jsonPatternPercentage"];
            const string expectedParameter2 = "%SomeNonExistantField > 5[%]%";
            Assert.Equal(actualParameter2, expectedParameter2);
        }

        [Fact]
        public void ShouldGenerateExpectedPipedLikeParametersForQueryBuilder()
        {
            CommandParameterValues parameterValues = null;
            transaction.ExecuteReader<IDocument>(Arg.Any<string>(), Arg.Do<CommandParameterValues>(pv => parameterValues = pv));

            CreateQueryBuilder<IDocument>("Project")
                .LikePipedParameter("Name", "Foo|Bar|Baz")
                .ToList();

            Assert.Equal("%|Foo|Bar|Baz|%", parameterValues["Name"]);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereLessThan()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] < @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(2);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] < @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(2, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereLessThanExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] < @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(2);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", SqlOperand.LessThan, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(2, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereLessThanOrEqual()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] <= @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(10);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] <= @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(10, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereLessThanOrEqualExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] <= @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(10);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", SqlOperand.LessThanOrEqual, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(10, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereEquals()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] = @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("[Title] = @title")
                .Parameter("title", "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "nevermore"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereEqualsExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] = @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("Title", SqlOperand.Equal, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "nevermore"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereNotEquals()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] <> @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("[Title] <> @title")
                .Parameter("title", "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "nevermore"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereNotEqualsExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] <> @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("Title", SqlOperand.NotEqual, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "nevermore"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThan()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] > @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(11);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] > @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(11, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] > @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(3);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", SqlOperand.GreaterThan, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(3, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanOrEqual()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] >= @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(21);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] >= @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(21, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanOrEqualExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] >= @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(21);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", SqlOperand.GreaterThanOrEqual, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(21, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereContains()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] LIKE @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("[Title] LIKE @title")
                .Parameter("title", "%nevermore%")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "%nevermore%"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereContainsExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] LIKE @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("Title", SqlOperand.Contains, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "%nevermore%"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInUsingWhereString()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] IN (@nevermore, @octofront))";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("[Title] IN (@nevermore, @octofront)")
                .Parameter("nevermore", "nevermore")
                .Parameter("octofront", "octofront")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    cp["nevermore"].ToString() == "nevermore"
                    && cp["octofront"].ToString() == "octofront"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInUsingWhereArray()
        {
            const string expectedSql = "SELECT * FROM dbo.[Project] WHERE ([State] IN (@state0, @state1)) ORDER BY [Id]";
            var queryBuilder = CreateQueryBuilder<IDocument>("Project")
                .Where("State", SqlOperand.In, new[] { State.Queued, State.Running });

            queryBuilder.DebugViewRawQuery().Should().Be(expectedSql);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInUsingWhereList()
        {
            var matches = new List<State>
            {
                State.Queued,
                State.Running
            };
            const string expectedSql = "SELECT * FROM dbo.[Project] WHERE ([State] IN (@state0, @state1)) ORDER BY [Id]";
            var queryBuilder = CreateQueryBuilder<IDocument>("Project")
                .Where("State", SqlOperand.In, matches);

            queryBuilder.DebugViewRawQuery().Should().Be(expectedSql);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInUsingEmptyList()
        {
            const string expextedSql = "SELECT * FROM dbo.[Project] WHERE (0 = 1) ORDER BY [Id]";
            var queryBuilder =
                CreateQueryBuilder<IDocument>("Project").Where("State", SqlOperand.In, new List<State>());

            queryBuilder.DebugViewRawQuery().Should().Be(expextedSql);
        }


        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] IN (@title0, @title1))";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("Title", SqlOperand.In, new[] { "nevermore", "octofront" })
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    cp["title0"].ToString() == "nevermore"
                    && cp["title1"].ToString() == "octofront"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereBetween()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] BETWEEN @startvalue AND @endvalue)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] BETWEEN @startvalue AND @endvalue")
                .Parameter("StartValue", 5)
                .Parameter("EndValue", 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereBetweenExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] BETWEEN @startvalue AND @endvalue)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", SqlOperand.Between, 5, 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereBetweenOrEqual()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] >= @startvalue AND [Completed] <= @endvalue)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] >= @startvalue AND [Completed] <= @endvalue")
                .Parameter("StartValue", 5)
                .Parameter("EndValue", 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereBetweenOrEqualExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] >= @startvalue) AND ([Completed] <= @endvalue)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", SqlOperand.BetweenOrEqual, 5, 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForOrderBy()
        {
            const string expectedSql = "SELECT TOP 1 * FROM dbo.[TodoItem] ORDER BY [Title]";
            var todoItem = new TodoItem { Id = 1, Title = "Complete Nevermore", Completed = false };

            transaction.ExecuteReader<TodoItem>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(new[] { todoItem });

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .OrderBy("Title")
                .First();

            transaction.Received(1).ExecuteReader<TodoItem>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp.Count == 0));

            Assert.NotNull(result);
            Assert.Equal(todoItem, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForOrderByDescending()
        {
            const string expectedSql = "SELECT TOP 1 * FROM dbo.[TodoItem] ORDER BY [Title] DESC";
            var todoItem = new TodoItem { Id = 1, Title = "Complete Nevermore", Completed = false };

            transaction.ExecuteReader<TodoItem>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(new[] { todoItem });

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .OrderByDescending("Title")
                .First();

            transaction.Received(1).ExecuteReader<TodoItem>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp.Count == 0));

            Assert.NotNull(result);
            Assert.Equal(todoItem, result);
        }

        [Fact]
        public void ShouldGenerateAliasForTable()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Alias("ORD")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateAliasForSubquery()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Subquery()
                .Alias("ORD")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateAliasesForSourcesInJoin()
        {
            var accounts = CreateQueryBuilder<IDocument>("Accounts")
                .Subquery()
                .Alias("ACC");

            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Alias("ORD")
                .Join(accounts.AsSource(), JoinType.InnerJoin)
                .On("AccountId", JoinOperand.Equal, "Id")
                .Where("Id", SqlOperand.Equal, 1)
                .OrderBy("Name")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateColumnSelection()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Column("Foo")
                .Column("Bar")
                .Column("Baz")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateColumnSelectionWithAliases()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Column("Foo", "F")
                .Column("Bar", "B")
                .Column("Baz", "B2")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateColumnSelectionWithTableAlias()
        {
            const string ordersTableAlias = "ORD";
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Alias(ordersTableAlias)
                .Column("Foo", "F", ordersTableAlias)
                .Column("Bar", "B", ordersTableAlias)
                .Column("Baz", "B2", ordersTableAlias)
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateColumnSelectionForJoin()
        {
            var accounts = CreateQueryBuilder<IDocument>("Accounts")
                .Subquery()
                .Alias("ACC");

            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Alias("ORD")
                .Join(accounts.AsSource(), JoinType.InnerJoin)
                .On("AccountId", JoinOperand.Equal, "Id")
                .Column("Id", "OrderId", "ORD")
                .Column("Id", "AccountId", "Acc")
                .Column("Number") // should come from "ORD"
                .Column("Id", "OrderId2") // should come from "ORD"
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateRowNumber()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .AddRowNumberColumn("ROWNUM")
                .OrderBy("ROWNUM")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateRowNumberWithOrderBy()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .OrderBy("Foo")
                .AddRowNumberColumn("ROWNUM")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateRowNumberWithPartitionBy()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .OrderBy("Foo")
                .AddRowNumberColumn("ROWNUM", "Region", "Area")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateRowNumberWithPartitionByInJoin()
        {
            var account = CreateQueryBuilder<IDocument>("Account");
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Join(account.AsAliasedSource(), JoinType.InnerJoin)
                .On("AccountId", JoinOperand.Equal, "Id")
                .OrderBy("Foo")
                .AddRowNumberColumn("ROWNUM", "Region", "Area")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateRowNumberWithPartitionByInJoinWithCustomAliases()
        {
            var account = CreateQueryBuilder<IDocument>("Account")
                .Alias("ACC");
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Alias("ORD")
                .Join(account.AsAliasedSource(), JoinType.InnerJoin)
                .On("AccountId", JoinOperand.Equal, "Id")
                .OrderBy("Foo")
                .AddRowNumberColumn("ROWNUM", new ColumnFromTable("Region", "ACC"), new ColumnFromTable("Area", "ACC"))
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateUnion()
        {
            var account = CreateQueryBuilder<IDocument>("Account")
                .Column("Id", "Id");
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Column("Id", "Id")
                .Union(account)
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateCalculatedColumn()
        {
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .CalculatedColumn("'CONSTANT'", "MyConstant")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateLeftHashJoin()
        {
            var account = CreateQueryBuilder<IDocument>("Account");
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Join(account.AsAliasedSource(), JoinType.LeftHashJoin)
                .On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateMultipleJoinTypes()
        {
            var customers = CreateQueryBuilder<IDocument>("Customers")
                .Where("Name", SqlOperand.StartsWith, "Bob");
            var account = CreateQueryBuilder<IDocument>("Account");
            var actual = CreateQueryBuilder<IDocument>("Orders")
                .Join(customers.Subquery().AsSource(), JoinType.InnerJoin)
                .On("CustomerId", JoinOperand.Equal, "Id")
                .Join(account.AsAliasedSource(), JoinType.LeftHashJoin)
                .On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateComplexDashboardQuery()
        {
            const string taskTableAlias = "t";
            const string releaseTableAlias = "r";
            var dashboard = CurrentDeployments()
                .Union(PreviousDeployments())
                .Alias("d")
                .Join(CreateQueryBuilder<IDocument>("ServerTask").Alias(taskTableAlias).AsAliasedSource(), JoinType.InnerJoin)
                .On("TaskId", JoinOperand.Equal, "Id")
                .Join(CreateQueryBuilder<IDocument>("Release").Alias(releaseTableAlias).AsAliasedSource(), JoinType.InnerJoin)
                .On("ReleaseId", JoinOperand.Equal, "Id")
                .Column("Id", "Id")
                .Column("Created", "Created")
                .Column("ProjectId", "ProjectId")
                .Column("EnvironmentId", "EnvironmentId")
                .Column("ReleaseId", "ReleaseId")
                .Column("TaskId", "TaskId")
                .Column("ChannelId", "ChannelId")
                .Column("CurrentOrPrevious", "CurrentOrPrevious")
                .Column("State", "State", taskTableAlias)
                .Column("HasPendingInterruptions", "HasPendingInterruptions", taskTableAlias)
                .Column("HasWarningsOrErrors", "HasWarningsOrErrors", taskTableAlias)
                .Column("ErrorMessage", "ErrorMessage", taskTableAlias)
                .Column("QueueTime", "QueueTime", taskTableAlias)
                .Column("CompletedTime", "CompletedTime", taskTableAlias)
                .Column("Version", "Version", releaseTableAlias)
                .Where("([Rank]=1 AND CurrentOrPrevious='P') OR ([Rank]=1 AND CurrentOrPrevious='C')");

            var actual = dashboard.DebugViewRawQuery();

            this.Assent(actual);
        }

        IQueryBuilder<IDocument> CurrentDeployments()
        {
            const string taskTableAlias = "t";
            const string deploymentTableAlias = "d";

            return CreateQueryBuilder<IDocument>("Deployment")
                .Alias(deploymentTableAlias)
                .Join(CreateQueryBuilder<IDocument>("ServerTask").Alias(taskTableAlias).AsAliasedSource(), JoinType.InnerJoin)
                .On("TaskId", JoinOperand.Equal, "Id")
                .CalculatedColumn("'C'", "CurrentOrPrevious")
                .Column("Id", "Id")
                .Column("Created", "Created")
                .Column("ProjectId", "ProjectId")
                .Column("EnvironmentId", "EnvironmentId")
                .Column("ReleaseId", "ReleaseId")
                .Column("TaskId", "TaskId")
                .Column("ChannelId", "ChannelId")
                .OrderByDescending("Created")
                .AddRowNumberColumn("Rank", new ColumnFromTable("EnvironmentId", deploymentTableAlias), new ColumnFromTable("ProjectId", deploymentTableAlias))
                .Where($"NOT (({taskTableAlias}.State = \'Canceled\' OR {taskTableAlias}.State = \'Cancelling\') AND {taskTableAlias}.StartTime IS NULL)");
        }
        
        IQueryBuilder<IDocument> PreviousDeployments()
        {
            const string deploymentTableAlias = "d";
            const string taskTableAlias = "t";
            const string l = "l";
            return CreateQueryBuilder<IDocument>("Deployment")
                .Alias(deploymentTableAlias)
                .Join(CreateQueryBuilder<IDocument>("ServerTask").Alias(taskTableAlias).AsAliasedSource(), JoinType.InnerJoin)
                .On("TaskId", JoinOperand.Equal, "Id")
                .Join(LQuery().Subquery().Alias(l).AsSource(), JoinType.LeftHashJoin)
                .On("Id", JoinOperand.Equal, "Id")
                .CalculatedColumn("'P'", "CurrentOrPrevious")
                .Column("Id", "Id")
                .Column("Created", "Created")
                .Column("ProjectId", "ProjectId")
                .Column("EnvironemntId", "EnvironmentId")
                .Column("ReleaseId", "ReleaseId")
                .Column("TaskId", "TaskId")
                .Column("ChannelId", "ChannelId")
                .OrderByDescending("Created")
                .AddRowNumberColumn("Rank", new ColumnFromTable("EnvironmentId", deploymentTableAlias),
                    new ColumnFromTable("ProjectId", deploymentTableAlias))
                .Where($"{taskTableAlias}.State = 'Success'")
                .Where($"{l}.Id is null");
        }

        IQueryBuilder<IDocument> LQuery()
        {
            return LatestDeployment()
                .Subquery()
                .Alias("LatestDeployment")
                .Column("Id")
                .Where("Rank", SqlOperand.Equal, 1);
        } 
        
        IQueryBuilder<IDocument> LatestDeployment()
        {
            var deploymentTableAlias = "d";
            var serverTaskTableAlias = "t";
            return CreateQueryBuilder<IDocument>("Deployment")
                .Alias(deploymentTableAlias)
                .Join(CreateQueryBuilder<IDocument>("ServerTask").Alias(serverTaskTableAlias).AsAliasedSource(), JoinType.InnerJoin)
                .On("TaskId", JoinOperand.Equal, "Id")
                .Column("Id", "Id")
                .OrderByDescending("Created")
                .AddRowNumberColumn("Rank", new ColumnFromTable("EnvironmentId", deploymentTableAlias), new ColumnFromTable("ProjectId", deploymentTableAlias))
                .Where($"NOT (({serverTaskTableAlias}.State = \'Canceled\' OR {serverTaskTableAlias}.State = \'Cancelling\') AND {serverTaskTableAlias}.StartTime IS NULL)");
        }

        [Fact]
        public void ShouldGenerateComplexQueryWithParameters()
        {
            const string eventTableAlias = "Event";

            var withJoins = CreateQueryBuilder<IDocument>("Deployment")
                .Join(CreateQueryBuilder<IDocument>("DeploymentRelatedMachine").AsAliasedSource(), JoinType.InnerJoin)
                .On("Id", JoinOperand.Equal, "DeploymentId")
                .Join(CreateQueryBuilder<IDocument>("EventRelatedDocument").AsAliasedSource(), JoinType.InnerJoin)
                .On("Id", JoinOperand.Equal, "RelatedDocumentId")
                .Join(CreateQueryBuilder<IDocument>("Event").Alias(eventTableAlias).AsAliasedSource(), JoinType.InnerJoin)
                .On("Id", JoinOperand.Equal, "EventId")
                .AllColumns()
                .OrderByDescending($"[{eventTableAlias}].[Occurred]")
                .AddRowNumberColumn("Rank", "EnvironmentId", "ProjectId", "TenantId");

            var actual = withJoins
                .Where("[DeploymentRelatedMachine].MachineId = @machineId")
                .Parameter(new Parameter("machineId", new NVarChar()))
                .Where("[Event].Category = \'DeploymentSucceeded\'")
                .Subquery()
                .Where("Rank = 1");

            this.Assent(actual.DebugViewRawQuery());
        }

        [Fact]
        public void ShouldGenerateComplexQueryUsingWith() // TODO: Work out how to add the api to support the `With` keyword
        {
            var nugetPackagesWithHasVersion = CreateQueryBuilder<IDocument>("GetNuGetPackages")
                .AllColumns()
                .CalculatedColumn("CASE WHEN VersionSpecial = \'\' THEN 0 ELSE 1 END", "HasVersionSpecial")
                .Subquery();
            var nugetPackageswithRecency = VersionOrder(nugetPackagesWithHasVersion)
                .AddRowNumberColumn("Recency", "PackageId");
            var packages = VersionOrder(nugetPackageswithRecency.OrderBy("PackageId"))
                .AddRowNumberColumn("RowNumber")
                .AllColumns()
                .Where("(@allowPreRelease = 1) or (@allowPreRelease = 0 and VersionSpecial = \'\')")
                .Parameter(new Parameter("allowPreRelease"))
                .Where("(@packageId is null or @packageId = \'\') or (@partialMatch = 0 and PackageId = @packageId) or (@partialMatch = 1 and PackageId LIKE \'%\' + @packageId + \'%\')")
                .Parameter(new Parameter("packageId"))
                .Parameter(new Parameter("partialMatch"))
                .Subquery()
                .Alias("Packages");

            packages
                .AllColumns()
                .OrderBy("RowNumber")
                .AddRowNumberColumn("FilteredRowNumber")
                .Where("@latestOnly = 0 OR (@latestOnly = 1 and Recency = 1)")
                .Parameter(new Parameter("latestOnly"));

            var actual = packages
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        IQueryBuilder<IDocument> VersionOrder(IQueryBuilder<IDocument> builder)
        {
            return builder
                .OrderByDescending("VersionMajor")
                .OrderByDescending("VersionMinor")
                .OrderByDescending("VersionBuild")
                .OrderByDescending("VersionRevision")
                .OrderBy("HasVersionSpecial")
                .OrderByDescending("VersionSpecial");
        }
    }

    public class Todos
    {
        public int Id { get; set; }
        public int Completed { get; set; }
        public List<TodoItem> Items { get; set; }
    }

    public class TodoItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool Completed { get; set; }
    }

    public enum State
    {
        Queued,
        Running
    }
}
