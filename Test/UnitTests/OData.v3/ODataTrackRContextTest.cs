﻿using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Data.Services.Client;
using TestData;
using TestData.Entities;
using TrackR.OData.v3;

namespace UnitTests.OData.v3
{
    [TestFixture]
    public class ODataTrackRContextTest
    {
        [Test]
        public void Ctor_Called_PropertiesSetUp()
        {
            // * Arrange
            var fixture = new Fixture();
            
            // * Act
            var sut = fixture.Build();

            // * Assert
            Assert.IsNotNull(sut.QueryContext);
        }


        private class Fixture : FixtureBase<ODataTrackRContext<FakeDataServiceContext, Entity>>
        {
            public Fixture()
            {
                Sut = new FakeODataServiceContext(
                    new Uri("http://localhost:3663/odata"), 
                    new Uri("http://localhost:3663/api/TrackR"));
            }
        }

        private class FakeODataServiceContext : ODataTrackRContext<FakeDataServiceContext, Entity>
        {
            public FakeODataServiceContext(Uri baseUri, Uri trackRUri) : base(baseUri, trackRUri)
            {
            }

            protected override int GetId(object entity)
            {
                return (int) entity.GetType().GetProperty("Id").GetValue(entity);
            }
            protected override void SetId(object entity, int value)
            {
                entity.GetType().GetProperty("Id").SetValue(entity, value);
            }
        }
    }

    class FakeDataServiceContext : DataServiceContext
    {
        private readonly Uri _uri;

        public FakeDataServiceContext(Uri uri)
        {
            _uri = uri;
        }
    }
}
