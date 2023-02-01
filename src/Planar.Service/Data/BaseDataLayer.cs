﻿using CommonJob;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public abstract class BaseDataLayer
    {
        protected readonly PlanarContext _context;

        protected BaseDataLayer(PlanarContext context)
        {
            _context = context ?? throw new PlanarJobException(nameof(context));
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess)
        {
            return await _context.SaveChangesAsync(acceptAllChangesOnSuccess);
        }

        public async Task<int> SaveChangesWithoutConcurrency()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // *** DO NOTHING *** //
            }

            return 0;
        }

        public IExecutionStrategy CreateExecutionStrategy()
        {
            return _context.Database.CreateExecutionStrategy();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
        }
    }
}