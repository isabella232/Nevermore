﻿using System.Linq;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore;
using Nevermore.IntegrationTests.Model;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.RelationalTransaction
{
    public class LoadFixture : FixtureWithRelationalStore
    {

        [Test]
        public void LoadWithSingleId()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.Load<Product>("A");
            }
        }

        [Test]
        public void LoadWithMultipleIds()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.Load<Product>(new[] {"A", "B"});
            }
        }

        [Test]
        public void LoadWithMoreThan2100Ids()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.Load<Product>(Enumerable.Range(1, 3000).Select(n => "ID-" + n));
            }
        }

        [Test]
        public void LoadStreamWithMoreThan2100Ids()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.LoadStream<Product>(Enumerable.Range(1, 3000).Select(n => "ID-" + n));
            }
        }

        [Test]
        public void StoreAndLoadInheritedTypes()
        {
            using (var trn = Store.BeginTransaction())
            {
                var originalSpecial = new SpecialProduct()
                {
                    Name = "Unicorn Dust",
                    BonusMaterial = "Directors Commentary",
                    Id = "UD-01",
                    Price = 11.1m,
                };

                var originalDud = new DodgyProduct()
                {
                    Id = "DO-01",
                    Name = "Something",
                    Price = 12.3m,
                    Tax = 15m
                };

                trn.Insert<SpecialProduct>(originalSpecial);
                trn.Insert<DodgyProduct>(originalDud);

                var allProducts = trn.TableQuery<Product>().ToList();
                Assert.True(allProducts.Exists(p =>
                    p is SpecialProduct sp && sp.BonusMaterial == originalSpecial.BonusMaterial));
                Assert.True(allProducts.Exists(p => p is DodgyProduct dp && dp.Tax == originalDud.Tax));

                var onlySpecial = trn.TableQuery<SpecialProduct>().ToList();
                onlySpecial.Count.Should().Be(1);
                onlySpecial[0].BonusMaterial.Should().Be(originalSpecial.BonusMaterial);
            }
        }
    }
}