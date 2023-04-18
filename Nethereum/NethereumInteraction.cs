﻿using Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System.Numerics;
using Utils;

namespace NethereumWorkflow
{
    public class NethereumInteraction
    {
        private readonly TestLog log;
        private readonly Web3 web3;
        private readonly string rootAccount;

        internal NethereumInteraction(TestLog log, Web3 web3, string rootAccount)
        {
            this.log = log;
            this.web3 = web3;
            this.rootAccount = rootAccount;
        }

        public void TransferTo(string account, decimal amount)
        {
            if (amount < 1 || string.IsNullOrEmpty(account)) throw new ArgumentException("Invalid arguments for AddToBalance");

            var value = ToHexBig(amount);
            var transactionId = Time.Wait(web3.Eth.TransactionManager.SendTransactionAsync(rootAccount, account, value));
            Time.Wait(web3.Eth.TransactionManager.TransactionReceiptService.PollForReceiptAsync(transactionId));

            Log($"Transferred {amount} to {account}");
        }

        public string GetTokenAddress(string marketplaceAddress)
        {
            var function = new GetTokenFunction();

            var handler = web3.Eth.GetContractQueryHandler<GetTokenFunction>();
            var result = Time.Wait(handler.QueryAsync<string>(marketplaceAddress, function));

            return result;
        }

        public void MintTestTokens(string account, decimal amount, string tokenAddress)
        {
            if (amount < 1 || string.IsNullOrEmpty(account)) throw new ArgumentException("Invalid arguments for MintTestTokens");

            var function = new MintTokensFunction
            {
                Holder = account,
                Amount = ToBig(amount)
            };

            var handler = web3.Eth.GetContractTransactionHandler<MintTokensFunction>();
            Time.Wait(handler.SendRequestAndWaitForReceiptAsync(tokenAddress, function));
        }

        public decimal GetBalance(string account)
        {
            var bigInt = Time.Wait(web3.Eth.GetBalance.SendRequestAsync(account));
            var result = ToDecimal(bigInt);
            Log($"Balance of {account} is {result}");
            return result;
        }

        private HexBigInteger ToHexBig(decimal amount)
        {
            var bigint = ToBig(amount);
            var str = bigint.ToString("X");
            return new HexBigInteger(str);
        }

        private BigInteger ToBig(decimal amount)
        {
            return new BigInteger(amount);
        }

        private decimal ToDecimal(HexBigInteger hexBigInteger)
        {
            return (decimal)hexBigInteger.Value;
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }

    [Function("token", "address")]
    public class GetTokenFunction : FunctionMessage
    {
    }

    [Function("mint")]
    public class MintTokensFunction : FunctionMessage
    {
        [Parameter("address", "holder", 1)]
        public string Holder { get; set; }

        [Parameter("uint256", "amount", 2)]
        public BigInteger Amount { get; set; }
    }
}
