﻿using Dapper;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public class UserData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<User> AddUser(User user)
    {
        var result = _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<User?> GetUser(int id, bool withTracking = false)
    {
        IQueryable<User> query = _context.Users;
        if (!withTracking)
        {
            query = query.AsNoTracking();
        }

        var result = await query.SingleOrDefaultAsync(u => u.Id == id);
        return result;
    }

    public async Task<User?> GetUser(string username, bool withTracking = false)
    {
        IQueryable<User> query = _context.Users;
        if (!withTracking)
        {
            query = query.AsNoTracking();
        }

        var result = await query.SingleOrDefaultAsync(u => u.Username == username);
        return result;
    }

    public async Task<UserIdentity?> GetUserIdentity(string username)
    {
        var result = await _context.Users
            .Where(u => u.Username == username)
            .Select(u => new UserIdentity
            {
                Id = u.Id,
                Surename = u.FirstName,
                GivenName = u.LastName,
                Username = u.Username,
                Password = u.Password,
                Salt = u.Salt,
            })
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<string?> GetUserRole(int id)
    {
        var result = await _context.Groups
            .Where(g => g.Users.Any(u => u.Id == id))
            .Select(g => g.Role.ToLower())
            .OrderByDescending(g => g)
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<string?> GetUserRole(string username)
    {
        var result = await _context.Groups
            .Where(g => g.Users.Any(u => u.Username == username))
            .Select(g => g.Role)
            .OrderByDescending(g => g)
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<List<EntityTitle>> GetGroupsForUser(int id)
    {
        var result = await _context.Groups
                .Where(g => g.Users.Any(u => u.Id == id))
                .Select(g => new EntityTitle(g.Name))
                .ToListAsync();

        return result;
    }

    public async Task UpdateUser(User user)
    {
        _context.Users.Update(user);
        _context.Entry(user).Property(u => u.Password).IsModified = false;
        _context.Entry(user).Property(u => u.Salt).IsModified = false;
        await _context.SaveChangesAsync();
    }

    public IQueryable<User> GetUsers()
    {
        var result = _context.Users
            .AsNoTracking()
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName);

        return result;
    }

    public async Task<int> RemoveUser(string username)
    {
        var parameters = new { Username = username };
        var cmd = new CommandDefinition(
            commandText: "dbo.DeleteUser",
            commandType: CommandType.StoredProcedure,
            parameters: parameters);

        return await DbConnection.ExecuteAsync(cmd);
    }

    public async Task<bool> IsUserExists(int userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }

    public async Task<int> GetUserId(string username)
    {
        return await _context.Users
            .Where(u => u.Username == username)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsUsernameExists(string? username)
    {
        if (username == null) { return false; }
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> IsUsernameExists(string? username, string ignoreUsername)
    {
        if (username == null) { return false; }
        return await _context.Users.AnyAsync(u => u.Username == username && u.Username != ignoreUsername);
    }
}