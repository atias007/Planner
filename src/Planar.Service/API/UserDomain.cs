﻿using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General.Hash;
using Planar.Service.General.Password;
using Planar.Service.Model;
using System;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class UserDomain(IServiceProvider serviceProvider) : BaseLazyBL<UserDomain, IUserData>(serviceProvider)
{
    public async Task<AddUserResponse> Add(AddUserRequest request)
    {
        if (await DataLayer.IsUsernameExists(request.Username))
        {
            throw new RestConflictException($"user with username '{request.Username}' already exists");
        }

        var hash = GeneratePassword();
        var user = Mapper.Map<User>(request);
        user.Password = hash.Hash;
        user.Salt = hash.Salt;

        _ = await DataLayer.AddUser(user);

        AuditSecuritySafe($"user '{user.Username}' was created");

        var response = new AddUserResponse
        {
            Password = hash.Value
        };

        return response;
    }

    public async Task Delete(string username)
    {
        var count = await DataLayer.RemoveUser(username);
        if (count == 0)
        {
            throw new RestNotFoundException($"user with username '{username}' could not be found");
        }

        AuditSecuritySafe($"user '{username}' was deleted");
    }

    public async Task<UserDetails> Get(string username)
    {
        var user = await DataLayer.GetUser(username);
        ValidateExistingEntity(user, "user");
        var groups = await DataLayer.GetGroupsForUser(user!.Id);
        var result = Mapper.Map<UserDetails>(user);
        groups.ForEach(g => result.Groups.Add(g.ToString()));
        var role = await DataLayer.GetUserRole(username);
        result.Role = role ?? RoleHelper.DefaultRole;
        return result;
    }

    public async Task<PagingResponse<UserRowModel>> GetAll(IPagingRequest request)
    {
        var query = DataLayer.GetUsers();
        var result = await query.ProjectToWithPagingAsyc<User, UserRowModel>(Mapper, request);
        return result;
    }

    public async Task<string> GetRole(string username)
    {
        if (!await DataLayer.IsUsernameExists(username))
        {
            throw new RestNotFoundException($"user with username '{username}' could not be found");
        }

        var role = await DataLayer.GetUserRole(username) ?? RoleHelper.DefaultRole;
        return role;
    }

    public async Task PartialUpdate(UpdateEntityRequestByName request)
    {
        TrimPropertyName(request);
        ForbbidenPartialUpdateProperties(request, $"to join user to group use: planar-cli user join {request.Name} {request.PropertyValue}", nameof(UserDetails.Groups));
        ForbbidenPartialUpdateProperties(request, $"to update user password use: planar-cli user set-password  {request.Name} {request.PropertyValue}", nameof(User.Password));
        ForbbidenPartialUpdateProperties(request, "to update user role join the user to group which has an appropriate role", nameof(UserDetails.Role));

        var user = await DataLayer.GetUser(request.Name);
        ValidateExistingEntity(user, "user");
        var updateUser = Mapper.Map<UpdateUserRequest>(user);
        updateUser.CurrentUsername = request.Name;
        var validator = Resolve<IValidator<UpdateUserRequest>>();
        await SetEntityProperties(updateUser, request, validator);
        await Update(updateUser);
    }

    public async Task<string> ResetPassword(string username)
    {
        var existsUser = await DataLayer.GetUser(username, withTracking: true);
        ValidateExistingEntity(existsUser, "user");
        if (existsUser == null) { return string.Empty; }
        var hash = GeneratePassword();
        existsUser.Password = hash.Hash;
        existsUser.Salt = hash.Salt;
        await DataLayer.SaveChangesAsync();

        AuditSecuritySafe($"password for user '{username}' was reset");

        return hash.Value;
    }

    public async Task SetPassword(string username, SetPasswordRequest request)
    {
        var existsUser = await DataLayer.GetUser(username, withTracking: true);
        ValidateExistingEntity(existsUser, "user");
        if (existsUser == null) { return; }
        var hash = HashUtil.CreateHash(request.Password);
        existsUser.Password = hash.Hash;
        existsUser.Salt = hash.Salt;
        await DataLayer.SaveChangesAsync();

        AuditSecuritySafe($"password for user '{username}' was changed");
    }

    public async Task Update(UpdateUserRequest request)
    {
        var current = await DataLayer.GetUser(request.CurrentUsername, withTracking: true)
            ?? throw new RestNotFoundException($"user with username '{request.CurrentUsername}' is not exists");

        if (await DataLayer.IsUsernameExists(request.Username, request.CurrentUsername))
        {
            throw new RestConflictException($"user with username '{request.Username}' already exists");
        }

        Mapper.Map(request, current);
        await DataLayer.SaveChangesAsync();
    }

    private static HashEntity GeneratePassword()
    {
        var password = PasswordGenerator.GeneratePassword(
           new PasswordGeneratorBuilder()
           .IncludeLowercase()
           .IncludeNumeric()
           .IncludeSpecial()
           .IncludeUppercase()
           .WithLength(12)
           .Build());

        var hash = HashUtil.CreateHash(password);
        return hash;
    }
}