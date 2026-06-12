namespace ZansiHustle.Shared.Enums.Wallets;

/// <summary>Bank account type captured on a withdrawal request.</summary>
public enum WithdrawalAccountType
{
    /// <summary>Cheque / current account.</summary>
    Cheque = 1,
    Savings = 2,
    Business = 3
}
