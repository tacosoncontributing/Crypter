﻿/*
 * Copyright (C) 2023 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commercial license options.
 */

using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Test.Integration_Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Crypter.Test.Integration_Tests.UserContact_Tests
{
   [TestFixture]
   internal class AddUserContact_Tests
   {
      private Setup _setup;
      private WebApplicationFactory<Program> _factory;
      private ICrypterApiClient _client;
      private ITokenRepository _clientTokenRepository;

      [OneTimeSetUp]
      public async Task OneTimeSetUp()
      {
         _setup = new Setup();
         await _setup.InitializeRespawnerAsync();

         _factory = await Setup.SetupWebApplicationFactoryAsync();
         (_client, _clientTokenRepository) = Setup.SetupCrypterApiClient(_factory.CreateClient());
      }

      [TearDown]
      public async Task TearDown()
      {
         await _setup.ResetServerDataAsync();
      }

      [OneTimeTearDown]
      public async Task OneTimeTearDown()
      {
         await _factory.DisposeAsync();
      }

      [TestCase]
      public async Task Add_User_Contact_Works_Async()
      {
         const string contactUsername = "Samwise";
         const string contactPassword = "dropping_no_eaves";

         RegistrationRequest contactRegistrationRequest = TestData.GetRegistrationRequest(contactUsername, contactPassword);
         var contactRegistrationResult = await _client.UserAuthentication.RegisterAsync(contactRegistrationRequest);

         RegistrationRequest userRegistrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
         var userRegistrationResult = await _client.UserAuthentication.RegisterAsync(userRegistrationRequest);

         LoginRequest userLoginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
         var userLoginResult = await _client.UserAuthentication.LoginAsync(userLoginRequest);

         await userLoginResult.DoRightAsync(async loginResponse =>
         {
            await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
         });

         var result = await _client.UserContact.AddUserContactAsync(contactUsername);

         Assert.True(contactRegistrationResult.IsRight);
         Assert.True(userRegistrationResult.IsRight);
         Assert.True(userLoginResult.IsRight);
         Assert.True(result.IsRight);
      }

      [TestCase]
      public async Task Add_User_Contact_Fails_For_Same_User_Async()
      {
         RegistrationRequest userRegistrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
         var userRegistrationResult = await _client.UserAuthentication.RegisterAsync(userRegistrationRequest);

         LoginRequest userLoginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
         var userLoginResult = await _client.UserAuthentication.LoginAsync(userLoginRequest);

         await userLoginResult.DoRightAsync(async loginResponse =>
         {
            await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
         });

         var result = await _client.UserContact.AddUserContactAsync(TestData.DefaultUsername);

         Assert.True(userRegistrationResult.IsRight);
         Assert.True(userLoginResult.IsRight);
         Assert.True(result.IsLeft);
      }

      [TestCase]
      public async Task Add_User_Contact_Fails_For_User_Not_Found_Async()
      {
         RegistrationRequest userRegistrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
         var userRegistrationResult = await _client.UserAuthentication.RegisterAsync(userRegistrationRequest);

         LoginRequest userLoginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
         var userLoginResult = await _client.UserAuthentication.LoginAsync(userLoginRequest);

         await userLoginResult.DoRightAsync(async loginResponse =>
         {
            await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
         });

         var result = await _client.UserContact.AddUserContactAsync("Tom_Bombadil");

         Assert.True(userRegistrationResult.IsRight);
         Assert.True(userLoginResult.IsRight);
         Assert.True(result.IsLeft);
      }
   }
}
