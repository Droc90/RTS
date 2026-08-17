namespace RTS.Domain.Rules;

public enum ComparisonOperator
{
    Equal = 1,
    NotEqual = 2,
    GreaterThan = 3,
    GreaterThanOrEqual = 4,
    LessThan = 5,
    LessThanOrEqual = 6,
    Between = 7,
    OutsideRange = 8,
    Contains = 9,
    In = 10,
    CrossesAbove = 11,
    CrossesBelow = 12
}