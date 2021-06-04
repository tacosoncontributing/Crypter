﻿using Crypter.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Crypter.Contracts.Requests.Registered;
using Crypter.Contracts.Responses.Registered;
using System;
using System.Text;

namespace Crypter.Web.Services
{

    public interface IAuthenticationService
    {
        User User { get; }
        Task Initialize();
        Task Login(string username, string plaintextPassword, string digestedPassword, string authenticationUrl);
        Task Logout();
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpService _httpService;
        private readonly NavigationManager _navigationManager;
        private readonly ILocalStorageService _localStorageService;

        public User User { get; private set; }

        public AuthenticationService(
            IHttpService httpService,
            NavigationManager navigationManager,
            ILocalStorageService localStorageService
        ) {
            _httpService = httpService;
            _navigationManager = navigationManager;
            _localStorageService = localStorageService;
        }

        public async Task Initialize()
        {
            User = await _localStorageService.GetItem<User>("user");
        }

        public async Task Login(string username, string plaintextPassword, string digestedPassword, string authenticationUrl)
        {
            var authResult = await _httpService.Post<UserAuthenticateResponse>(authenticationUrl, new AuthenticateUserRequest(username, digestedPassword));
            User = new User(authResult.Id, authResult.Token);

            if (string.IsNullOrEmpty(authResult.EncryptedPrivateKey))
            {
                User.PrivateKey = null;
            }
            else
            {
                var decryptionKey = CryptoLib.Common.CreateSymmetricKeyFromUserDetails(username, plaintextPassword, authResult.Id);
                byte[] decodedPrivateKey = Convert.FromBase64String(authResult.EncryptedPrivateKey);
                byte[] decryptedPrivateKey = CryptoLib.Common.UndoSymmetricEncryption(decodedPrivateKey, decryptionKey);
                User.PrivateKey = Encoding.UTF8.GetString(decryptedPrivateKey);
            }

            await _localStorageService.SetItem("user", User);
        }

        public async Task Logout()
        {
            User = null;
            await _localStorageService.RemoveItem("user");
            _navigationManager.NavigateTo("/", true);
        }
    }
}
