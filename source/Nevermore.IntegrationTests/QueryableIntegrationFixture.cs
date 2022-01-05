﻿using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class QueryableIntegrationFixture : FixtureWithRelationalStore
    {
        [Test]
        public void WhereEqual()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.FirstName == "Alice")
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple");
        }
        [Test]
        public void WhereEqualJson()
        {
            using var t = Store.BeginTransaction();

            var testMachines = new[]
            {
                new Machine { Name = "Machine A", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle A" } },
                new Machine { Name = "Machine B", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle B" } },
                new Machine { Name = "Machine C", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle C" } },
            };

            foreach (var c in testMachines)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Machine>()
                .Where(m => m.Endpoint.Name == "Tentacle A")
                .ToList();

            customers.Select(m => m.Name).Should().BeEquivalentTo("Machine A");
        }

        [Test]
        public void WhereNotEqual()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.FirstName != "Alice")
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana", "Cherry");
        }

        [Test]
        public void WhereGreaterThan()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance > 100)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Cherry");
        }

        [Test]
        public void WhereLessThan()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance < 100)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana");
        }

        [Test]
        public void WhereGreaterThanOrEqual()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance >= 301.4m)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Cherry");
        }

        [Test]
        public void WhereLessThanOrEqual()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance <= 56.3m)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana");
        }

        [Test]
        public void WhereComposite()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance >= 50m && c.Balance <= 100)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana");
        }

        [Test]
        public void WhereContains()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var names = new[] { "Apple", "Orange", "Peach" };
            var customers = t.Queryable<Customer>()
                .Where(c => names.Contains(c.LastName))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Alice");
        }

        [Test]
        public void WhereContainsOnDocument()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Roles = { "RoleA", "RoleB" }},
                new Customer { FirstName = "Bob", LastName = "Banana", Roles = { "RoleA", "RoleC" }},
                new Customer { FirstName = "Charlie", LastName = "Cherry", Roles = { "RoleB", "RoleC" }}
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Roles.Contains("RoleC"))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Bob", "Charlie");
        }

        [Test]
        public void WhereContainsOnDocumentJson()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] { 78, 321 }},
                new Customer { FirstName = "Bob", LastName = "Banana", LuckyNumbers = new[] { 662, 91 }},
                new Customer { FirstName = "Charlie", LastName = "Cherry", LuckyNumbers = new[] { 4, 18 }}
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.LuckyNumbers.Contains(4))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Charlie");
        }

        [Test]
        public void WhereNotContains()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var names = new[] { "Apple", "Orange", "Peach" };
            var customers = t.Queryable<Customer>()
                .Where(c => !names.Contains(c.LastName))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Bob", "Charlie");
        }

        [Test]
        public void WhereNotStringContains()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Bear" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Beets" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Chicken" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => !c.Nickname.StartsWith("C"))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Alice", "Bob");
        }

        [Test]
        public void First()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customer = t.Queryable<Customer>()
                .First();

            customer.LastName.Should().BeEquivalentTo("Apple");
        }

        [Test]
        public void FirstWithPredicate()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customer = t.Queryable<Customer>()
                .First(c => c.FirstName == "Alice");

            customer.LastName.Should().BeEquivalentTo("Apple");
        }

        [Test]
        public void FirstOrDefault()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customer = t.Queryable<Customer>()
                .FirstOrDefault();

            customer.LastName.Should().BeEquivalentTo("Apple");
        }

        [Test]
        public void FirstOrDefaultWithPredicate()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customer = t.Queryable<Customer>()
                .FirstOrDefault(c => c.FirstName.EndsWith("y"));

            customer.Should().BeNull();
        }

        [Test]
        public void Skip()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Skip(2)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Cherry");
        }

        [Test]
        public void Take()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Take(2)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Banana");
        }

        [Test]
        public void SkipAndTake()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" },
                new Customer { FirstName = "Dan", LastName = "Durian" },
                new Customer { FirstName = "Erin", LastName = "Eggplant" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Skip(2)
                .Take(2)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Cherry", "Durian");
        }

        [Test]
        public void Count()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var count = t.Queryable<Customer>().Count();

            count.Should().Be(3);
        }

        [Test]
        public void CountWithPredicate()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Bandit" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Chief" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Cherry Bomb" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var count = t.Queryable<Customer>().Count(c => c.Nickname.StartsWith("C"));

            count.Should().Be(2);
        }

        [Test]
        public void OrderBy()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Zeta" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Omega" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>().OrderBy(c => c.Nickname).ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana", "Cherry", "Apple");
        }

        [Test]
        public void OrderByDescending()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Omega" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Zeta" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>().OrderByDescending(c => c.Nickname).ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Cherry", "Apple", "Banana");
        }

        [Test]
        public void Any()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Omega" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Zeta" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var anyCustomers = t.Queryable<Customer>().Any();

            anyCustomers.Should().BeTrue();
        }

        [Test]
        public void AnyWithPredicate()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Omega" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Zeta" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var anyCustomers = t.Queryable<Customer>().Any(c => c.Nickname == "Warlock");

            anyCustomers.Should().BeFalse();
        }
    }
}