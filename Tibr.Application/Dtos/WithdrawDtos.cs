using Tibr.Domain.Enums;

namespace Tibr.Application.Dtos;
public class CreateWithdrawDto
{
    public decimal Amount { get; set; }
    public WithdrawType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;

}

public class WithdrawDto
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;

}


public class UpdateWithdrawStatusDto
{
    public long Id { get; set; }
    public WithdrawStatus Status { get; set; }
}