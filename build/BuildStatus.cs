// ReSharper disable MemberCanBePrivate.Global
namespace Build;

public readonly struct BuildStatus
{
    public static readonly BuildStatus Succeed = new(Status.Succeed);
    
    public static readonly BuildStatus Warnings = new(Status.Warning);
    
    public static readonly BuildStatus Fail = new(Status.Fail);
    
    private readonly Status _status;
    
    private BuildStatus(Status status) => _status = status;
    
    public int ExitCode => 
        _status switch
        {
            Status.Succeed => 0,
            Status.Fail => 1,
            _ => 199
        };

    public static implicit operator BuildStatus(bool success) => success ? Succeed : Fail;
    
    public static implicit operator BuildStatus(int? exitCode) => exitCode == 0;
    
    public static implicit operator int(BuildStatus buildStatus) => buildStatus.ExitCode;
    
    public static BuildStatus operator +(BuildStatus buildStatus1, BuildStatus buildStatus2) => 
        new(buildStatus1._status > buildStatus2._status ? buildStatus1._status : buildStatus2._status);
    
    public override string ToString() => _status.ToString();
    
    private enum Status
    {
        Succeed,
        Warning,
        Fail
    } 
}