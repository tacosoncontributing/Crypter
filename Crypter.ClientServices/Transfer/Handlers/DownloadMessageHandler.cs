﻿/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Transfer.Handlers.Base;
using Crypter.ClientServices.Transfer.Models;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Contracts.Features.Transfer;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.StreamEncryption;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Transfer.Handlers
{
   public class DownloadMessageHandler : DownloadHandler
   {
      public DownloadMessageHandler(ICrypterApiService crypterApiService, ICryptoProvider cryptoProvider, IUserSessionService userSessionService, TransferSettings transferSettings)
         : base(crypterApiService, cryptoProvider, userSessionService, transferSettings)
      { }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadPreviewAsync()
      {
#pragma warning disable CS8524
         var response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiService.DownloadAnonymousMessagePreviewAsync(_transferHashId),
            TransferUserType.User => await _crypterApiService.DownloadUserMessagePreviewAsync(_transferHashId, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524

         response.DoRight(x => SetSenderPublicKey(x.PublicKey, x.KeyExchangeNonce));
         return response;
      }

      public async Task<Either<DownloadTransferCiphertextError, string>> DownloadCiphertextAsync(Maybe<Func<Task>> invokeBeforeDecryption)
      {
         byte[] symmetricKey = _symmetricKey.Match(
            () => throw new Exception("Missing symmetric key"),
            x => x);

         DownloadTransferCiphertextRequest request = _serverProof.Match(
            () => throw new Exception("Missing server key"),
            x => new DownloadTransferCiphertextRequest(x));

#pragma warning disable CS8524
         Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse> response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiService.DownloadAnonymousMessageCiphertextAsync(_transferHashId, request),
            TransferUserType.User => await _crypterApiService.DownloadUserMessageCiphertextAsync(_transferHashId, request, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524

         return await response.MatchAsync<Either<DownloadTransferCiphertextError, string>>(
            left => left,
            async right =>
            {
               await invokeBeforeDecryption.IfSomeAsync(async x => await x.Invoke());
               return DecryptMessage(symmetricKey, right.Header, right.Ciphertext);
            },
            DownloadTransferCiphertextError.UnknownError);
      }

      private string DecryptMessage(byte[] key, byte[] header, List<byte[]> ciphertext)
      {
         IStreamDecrypt streamDecryptor = _cryptoProvider.StreamEncryptionFactory.NewDecryptionStream(key, header, _transferSettings.PaddingBlockSize);
         byte[] plaintext = streamDecryptor.Pull(ciphertext[0], out bool final);

         if (!final)
         {
            throw new CryptographicException("Missing 'final' chunk.");
         }

         return Encoding.UTF8.GetString(plaintext);
      }
   }
}
