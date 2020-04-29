﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.Extensions.Logging;
using MUDhub.Core.Abstracts;
using MUDhub.Core.Abstracts.Models;
using MUDhub.Core.Models;
using MUDhub.Core.Models.Muds;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MUDhub.Core.Services
{
    internal class MudManager : IMudManager
    {
        private readonly MudDbContext _context;
        private readonly ILogger? _logger;

        public MudManager(MudDbContext context, ILogger<MudManager>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool, string)> CreateMudAsync(string name, MudCreationArgs args)
        {
            var mud = new MudGame
            {
                Name = name
            };
            _context.MudGames.Add(mud);
            await _context.SaveChangesAsync()
                .ConfigureAwait(false);
            var result = await UpdateMudAsync(mud.Id, new MudUpdateArgs(args))
                .ConfigureAwait(false);
            if (result)
            {
                _logger?.LogInformation($"Mud with id: '{mud.Id}', was successfully created.");
            }
            else
            {
                _logger?.LogWarning($"Mud with id: '{mud.Id}', was successfully created, but not correctly modified. This should never happen!");
            }
            return (result, mud.Id);
        }

        public async Task<bool> UpdateMudAsync(string mudId, MudUpdateArgs args)
        {
            var mud = await GetMudGameByIdAsync(mudId)
                .ConfigureAwait(false);
            if (mud is null)
            {
                _logger?.LogWarning($"Mudid: '{mudId}' didn't exists. No Update possible.");
                return false;
            }
            if (args.Name != null)
                mud.Name = args.Name;
            if (args.Description != null)
                mud.Description = args.Description;
            if (args.ImageKey != null)
                mud.ImageKey = args.ImageKey;
            if (args.IsPublic.HasValue)
            {
                mud.IsPublic = args.IsPublic.Value;
                //Todo: handle the scenario, a MudMaster change the from public to private, how we handle the joined Characters?
                // 1. Are the related Users automatically approved? (My Favorite)
                // 2. Or should we, implementing a usecase where the characters are Block until they are approved?
                // And if it change from private to public, should the approvals be stored further? I think Yes.

            }
            if (args.AutoRestart.HasValue)
                mud.AutoRestart = args.AutoRestart.Value;
            await _context.SaveChangesAsync()
                .ConfigureAwait(false);
            _logger?.LogInformation($"Mud with id: '{mudId}', was successfully updated, with the given arguments: {Environment.NewLine}" +
                $"- Name: {args.Name ?? "<no modification>"},{Environment.NewLine}" +
                $"- Description: {args.Description ?? "<no modification>"},{Environment.NewLine}" +
                $"- ImageKey: {args.ImageKey ?? "<no modification>"},{Environment.NewLine}" +
                $"- IsPublic: {(args.IsPublic.HasValue ? args.IsPublic.Value.ToString(CultureInfo.InvariantCulture) : "<no modification>")},{Environment.NewLine}" +
                $"- AutoRestart: {(args.AutoRestart.HasValue ? args.AutoRestart.Value.ToString(CultureInfo.InvariantCulture) : "<no modification>")},{Environment.NewLine}");
            return true;
        }

        public async Task<bool> RemoveMudAsync(string mudId)
        {
            var mud = await GetMudGameByIdAsync(mudId)
                .ConfigureAwait(false);
            if (mud is null)
            {
                _logger?.LogWarning($"Mudid: '{mudId}' didn't exists. Can't remove a Mud that not exist.");
                return false;
            }
            _context.MudGames.Remove(mud);
            //Todo: Later handle references like characters or items, etc....
            await _context.SaveChangesAsync()
                .ConfigureAwait(false);
            _logger?.LogInformation($"Mud '{mud.Name}' with the Id '{mud.Id}' was successfully removed.");
            return true;
        }

        public async Task<bool> RequestUserForJoinAsync(string userId, string mudId)
        {
            //Todo: Later handle User references, check if exists

            var joinRequest = await _context.MudJoinRequests.FindAsync(mudId, userId);
            if (joinRequest != null)
            {
                _logger?.LogInformation($"The User with the UserId '{userId}' is already {joinRequest.State} " +
                                        $"in the MudGame '{joinRequest.MudGame.Name}' with the Id: '{joinRequest.MudId}', no Request.");
                //Todo: Maybe later return some useful information why the request was rejected.
                return false;
            }

            var mud = await GetMudGameByIdAsync(mudId)
                .ConfigureAwait(false);
            if (mud is null)
            {
                _logger?.LogWarning($"Mudid: '{mudId}' didn't exists. Can't request User with Id:{userId} to join.");
                return false;
            }

            if (mud.IsPublic)
            {
                _logger?.LogInformation($"The Mud '{mud.Name}' with the Id: '{mud.Id}' is public no need, to ask for join.");
                return false;
            }

            joinRequest = new MudJoinRequest(mudId, userId)
            {
                State = MudJoinState.Requested
            };
            mud.JoinRequests.Add(joinRequest);
            await _context.SaveChangesAsync()
                .ConfigureAwait(false);

            return true;
        }

        public async Task<bool> ApproveUserToJoinAsync(string userId, string mudId)
        {
            var mud = await GetMudGameByIdAsync(mudId)
               .ConfigureAwait(false);
            if (mud is null)
            {
                _logger?.LogWarning($"Mudid: '{mudId}' didn't exists. Can't handle the approval User with Id: '{userId}' to join.");
                return false;
            }
            //Todo: What if the Mud becomes public and the Request get approved afterwards?
            var joinRequest = mud.JoinRequests.FirstOrDefault(mjr => mjr.UserId == userId);
            if (joinRequest is null)
            {
                _logger?.LogWarning($"UserId: '{userId}' didn't exists. Can't handle the approval with MudId: '{mudId}' to join.");
                return false;
            }
            if (joinRequest.State == MudJoinState.Accepted)
            {
                _logger?.LogWarning($"User with the UserId '{userId}' is already approved " +
                                    $"in the MudGame '{joinRequest.MudGame.Name}' with the Id: '{joinRequest.MudId}'");
                return false;
            }
            joinRequest.State = MudJoinState.Accepted;
            await _context.SaveChangesAsync()
                .ConfigureAwait(false);
            _logger?.LogInformation($"User with the UserId '{userId}' is successfully approved " +
                                    $"and can join the MudGame '{mud.Name}' with the Id: '{mud.Id}'");
            return true;
        }

        public async Task<bool> RejectUserToJoinAsync(string userId, string mudId)
        {
            var joinRequest = await _context.MudJoinRequests.FindAsync(mudId, userId);
            //Todo: Handle if the JoinState does not exists, 
            //      check if the Mud AND User exists and only then go further.
            if (joinRequest is null)
            {
                joinRequest = new MudJoinRequest(mudId, userId);
                await _context.MudJoinRequests.AddAsync(joinRequest);
            }
            if (joinRequest.State == MudJoinState.Rejected)
            {
                _logger?.LogWarning($"User with the UserId '{userId}' is already rejected " +
                                    $"in the MudGame '{joinRequest.MudGame.Name}' with the Id: '{joinRequest.MudId}'");
                return false;
            }
            joinRequest.State = MudJoinState.Rejected;
            await _context.SaveChangesAsync()
                .ConfigureAwait(false);
            return true;
        }


        private async Task<MudGame> GetMudGameByIdAsync(string id)
            => await _context.MudGames.FindAsync(id);
    }
}