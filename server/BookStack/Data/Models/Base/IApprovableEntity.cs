namespace BookStack.Data.Models.Base;

public interface IApprovableEntity
{
    bool IsApproved { get; }
}
