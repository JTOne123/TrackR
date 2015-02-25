﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using TrackR.Common;

namespace TrackR.Controller.EF6
{
    public abstract class TrackRControllerBase : ApiController
    {
        private readonly DbContext _context;
        private readonly List<Assembly> _assemblies;

        protected TrackRControllerBase()
        {
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            _context = CreateContext();
            _assemblies = new List<Assembly>();

            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            AddAssemblies(_assemblies);
        }

        protected abstract DbContext CreateContext();

        protected virtual void AddAssemblies(List<Assembly> assemblies)
        {
            _assemblies.Add(typeof(string).Assembly);
        }


        public IHttpActionResult Post(ChangeSet changeSet)
        {
            if (changeSet == null)
            {
                return BadRequest();
            }

            // Delete flagged entities
            var toRemove = changeSet.Entities
                .Where(e => e.ChangeState == ChangeState.Deleted)
                .ToList();
            foreach (var remove in toRemove)
            {
                _context.Entry(remove.Entity).State = EntityState.Deleted;
            }
            
            // Add flagged entities
            var toAdd = changeSet.Entities
                .Where(e => e.ChangeState == ChangeState.Added)
                .ToList();
            foreach (var add in toAdd)
            {
                _context.Entry(add.Entity).State = EntityState.Added;
            }

            // Modify flagged entities
            var toEdit = changeSet.Entities
                .Where(e => e.ChangeState == ChangeState.Changed)
                .ToList();
            foreach (var edit in toEdit)
            {
                _context.Entry(edit.Entity).State = EntityState.Modified;
            }

            // Reconstruct object graphs
            foreach (var wrapper in changeSet.Entities)
            {
                Reconstruct(wrapper, changeSet.Entities);
            }

            // Save changes
            _context.SaveChanges();

            return Ok();
        }

        private void Reconstruct(EntityWrapper wrapper, List<EntityWrapper> entities)
        {
            foreach (var reference in wrapper.References)
            {
                var refWrapper = entities.FirstOrDefault(e => e.Guid == reference.Reference);
                
                // This reference must have been unchanged
                if (refWrapper == null) 
                    continue;

                // Attach
                var property = wrapper.Entity.GetType().GetProperty(reference.PropertyName);
                if (wrapper.ChangeState == ChangeState.Deleted)
                {
                    property.SetValue(wrapper.Entity, null);
                }
                else
                {
                    property.SetValue(wrapper.Entity, refWrapper.Entity);
                }
            }
        }
    }
}